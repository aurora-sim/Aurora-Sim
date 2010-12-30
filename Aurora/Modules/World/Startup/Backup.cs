using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Timers;
using System.Xml;
using Nini.Config;
using log4net;
using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace Aurora.Modules
{
    public class Backup : ISharedRegionStartupModule
    {
        #region Declares

        protected SceneManager m_manager;
        protected Dictionary<Scene, InternalSceneBackup> m_backup = new Dictionary<Scene, InternalSceneBackup>();
        // the minimum time that must elapse before a changed object will be considered for persisted
        public static long m_dontPersistBefore = 60;
        // the maximum time that must elapse before a changed object will be considered for persisted
        public static long m_persistAfter = 600;

        #endregion

        #region ISharedRegionStartupModule Members

        public void Initialise(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
            MainConsole.Instance.Commands.AddCommand("region", false, "backup", "backup [all]", "Persist objects to the database now, if [all], will force the persistence of all prims", RunCommand);
            //Set up the backup for the scene
            m_backup[scene] = new InternalSceneBackup(scene);

            IConfig persistanceConfig = source.Configs["Persistance"];
            if (persistanceConfig != null)
            {
                m_dontPersistBefore =
                    persistanceConfig.GetLong("MinimumTimeBeforePersistenceConsidered", m_dontPersistBefore);

                m_persistAfter =
                    persistanceConfig.GetLong("MaximumTimeBeforePersistenceConsidered", m_persistAfter);
            }
        }

        public void PostInitialise(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
        }

        public void FinishStartup(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
            m_manager = scene.RequestModuleInterface<SceneManager>();
            m_backup[scene].FinishStartup();
        }

        public void Close(Scene scene)
        {
        }

        #endregion

        #region Console commands

        /// <summary>
        /// Runs commands issued by the server console from the operator
        /// </summary>
        /// <param name="command">The first argument of the parameter (the command)</param>
        /// <param name="cmdparams">Additional arguments passed to the command</param>
        public void RunCommand(string module, string[] cmdparams)
        {
            List<string> args = new List<string>(cmdparams);
            if (args.Count < 1)
                return;

            string command = args[0];
            args.RemoveAt(0);

            cmdparams = args.ToArray();

            switch (command)
            {
                case "backup":
                    m_manager.ForEachCurrentScene(delegate(Scene scene)
                    {
                        m_backup[scene].ProcessPrimBackupTaints(true,args.Count == 1);
                    });
                    break;
            }
        }

        #endregion

        #region Per region backup class

        protected class InternalSceneBackup : IBackupModule
        {
            #region Declares

            protected static readonly ILog m_log
                = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            protected Scene m_scene;
            protected List<UUID> m_needsDeleted = new List<UUID>();
            protected Dictionary<UUID, SceneObjectGroup> m_backupTaintedPrims = new Dictionary<UUID, SceneObjectGroup>();
            protected Dictionary<UUID, SceneObjectGroup> m_secondaryBackupTaintedPrims = new Dictionary<UUID, SceneObjectGroup>();
            protected DateTime runSecondaryBackup = DateTime.Now;
            protected volatile bool m_backingup = false;
            protected int m_update_backup = 50; //Trigger backup
            protected DateTime m_lastRanBackupInHeartbeat = DateTime.MinValue;
            protected bool m_LoadingPrims = false;

            #endregion

            #region Constructor

            public InternalSceneBackup(Scene scene)
            {
                m_scene = scene;
                m_scene.RegisterModuleInterface<IBackupModule>(this);
                m_scene.EventManager.OnFrame += UpdateStorageBackup;
            }

            #endregion

            #region Scene events

            /// <summary>
            /// Back up queued up changes if it is time
            /// </summary>
            protected void UpdateStorageBackup()
            {
                if (m_scene.Frame % m_update_backup == 0) //Don't do this every time
                {
                    //Check every min persistant times as well except when it is set to 0
                    if (!m_backingup || (m_lastRanBackupInHeartbeat.Ticks > DateTime.Now.Ticks
                        && m_dontPersistBefore != 0))
                    {
                        //Add the time now plus minimum persistance time so that we can force a run if it goes wrong
                        m_lastRanBackupInHeartbeat = DateTime.Now.AddMinutes((m_dontPersistBefore / 10000000L));
                        Util.FireAndForget(Backup);
                    }
                }
            }

            /// <summary>
            /// Backup the scene.  This acts as the main method of the backup thread.
            /// This is used for the scene event trigger only
            /// </summary>
            /// <param name="forced"></param>
            protected void Backup(object forced)
            {
                ProcessPrimBackupTaints(false, false);
            }

            /// <summary>
            /// Loads the World's objects
            /// </summary>
            protected void LoadPrimsFromStorage()
            {
                LoadingPrims = true;
                m_log.Info("[BackupModule]: Loading objects from datastore");

                List<SceneObjectGroup> PrimsFromDB = m_scene.SimulationDataService.LoadObjects(m_scene.RegionInfo.RegionID, m_scene);
                foreach (SceneObjectGroup group in PrimsFromDB)
                {
                    m_scene.SceneGraph.CheckAllocationOfLocalIds(group);
                    if (group.IsAttachment || (group.RootPart.Shape != null && (group.RootPart.Shape.State != 0 &&
                        (group.RootPart.Shape.PCode == (byte)PCode.None ||
                        group.RootPart.Shape.PCode == (byte)PCode.Prim ||
                        group.RootPart.Shape.PCode == (byte)PCode.Avatar))))
                    {
                        m_log.Warn("[BackupModule]: Broken state for object " + group.Name + " while loading objects, removing it from the database.");
                        //WTF went wrong here? Remove it and then pass it by on loading
                        m_scene.SimulationDataService.RemoveObject(group.UUID, m_scene.RegionInfo.RegionID);
                        continue;
                    }
                    group.Scene = m_scene;
                    m_scene.EventManager.TriggerOnSceneObjectLoaded(group);

                    if (group.RootPart == null)
                    {
                        m_log.ErrorFormat("[BackupModule] Found a SceneObjectGroup with m_rootPart == null and {0} children",
                                          group.ChildrenList.Count);
                        continue;
                    }
                    m_scene.SceneGraph.RestorePrimToScene(group);
                    SceneObjectPart rootPart = group.GetChildPart(group.UUID);
                    rootPart.Flags &= ~PrimFlags.Scripted;
                    rootPart.TrimPermissions();
                    group.CheckSculptAndLoad();
                }
                LoadingPrims = false;
                m_log.Info("[BackupModule]: Loaded " + PrimsFromDB.Count.ToString() + " SceneObject(s)");
            }

            /// <summary>
            /// Loads all Parcel data from the datastore for region identified by regionID
            /// </summary>
            protected void LoadAllLandObjectsFromStorage()
            {
                m_log.Info("[BackupModule]: Loading Land Objects from database... ");
                IParcelServiceConnector conn = DataManager.DataManager.RequestPlugin<IParcelServiceConnector>();
                List<LandData> LandObjects = m_scene.SimulationDataService.LoadLandObjects(m_scene.RegionInfo.RegionID);
                if (conn != null)
                {
                    if (LandObjects.Count != 0)
                    {
                        foreach (LandData land in LandObjects)
                        {
                            //Store it in the new database
                            conn.StoreLandObject(land);
                            //Remove it from the old
                            m_scene.SimulationDataService.RemoveLandObject(m_scene.RegionInfo.RegionID, land.GlobalID);
                        }
                    }
                    m_scene.EventManager.TriggerIncomingLandDataFromStorage(conn.LoadLandObjects(m_scene.RegionInfo.RegionID));
                }
                else
                    m_scene.EventManager.TriggerIncomingLandDataFromStorage(LandObjects);

                m_scene.EventManager.TriggerParcelPrimCountUpdate();
            }

            internal void FinishStartup()
            {
                //Load the prims from the database now that we are done loading
                LoadPrimsFromStorage();
                //Then load the land objects
                LoadAllLandObjectsFromStorage();
                //Load the prims from the database now that we are done loading
                CreateScriptInstances();
            }

            /// <summary>
            /// Start all the scripts in the scene which should be started.
            /// </summary>
            protected void CreateScriptInstances()
            {
                m_log.Info("[BackupModule]: Starting scripts in " + m_scene.RegionInfo.RegionName);
                //Set loading prims here to block backup
                LoadingPrims = true;
                EntityBase[] entities;
                lock (m_scene.Entities)
                {
                    entities = m_scene.Entities.GetEntities();
                }
                foreach (EntityBase group in entities)
                {
                    if (group is SceneObjectGroup)
                    {
                        ((SceneObjectGroup)group).CreateScriptInstances(0, false, m_scene.DefaultScriptEngine, 0, UUID.Zero);
                        ((SceneObjectGroup)group).ResumeScripts();
                    }
                }
                //Now reset it
                LoadingPrims = false;
            }

            #endregion

            #region Public members

            /// <summary>
            /// Are we currently loading prims?
            /// </summary>
            public bool LoadingPrims
            {
                get { return m_LoadingPrims; }
                set { m_LoadingPrims = value; }
            }

            /// <summary>
            /// Queue the prim to be deleted from the simulation service
            /// </summary>
            /// <param name="uuid"></param>
            public void DeleteFromStorage(UUID uuid)
            {
                lock (m_needsDeleted)
                {
                    if (!m_needsDeleted.Contains(uuid))
                        m_needsDeleted.Add(uuid);
                }
            }

            /// <summary>
            /// Delete every object from the scene.  This does not include attachments worn by avatars.
            /// </summary>
            public void DeleteAllSceneObjects()
            {
                EntityBase[] entities;
                lock (m_scene.Entities)
                {
                    entities = m_scene.Entities.GetEntities();
                }
                List<ISceneEntity> ObjectsToDelete = new List<ISceneEntity>();
                foreach (EntityBase e in entities)
                {
                    if (e is SceneObjectGroup)
                    {
                        SceneObjectGroup group = (SceneObjectGroup)e;
                        if (group.IsAttachment)
                            continue;

                        m_scene.DeleteSceneObject(group, true);
                        ObjectsToDelete.Add(group.RootPart);
                    }
                }
                m_scene.ForEachScenePresence(delegate(ScenePresence avatar)
                {
                    avatar.ControllingClient.SendKillObject(m_scene.RegionInfo.RegionHandle, ObjectsToDelete.ToArray());
                });

                m_scene.SimulationDataService.RemoveRegion(m_scene.RegionInfo.RegionID);
            }

            /// <summary>
            /// Add a backup taint to the prim
            /// </summary>
            /// <param name="sceneObjectGroup"></param>
            public void AddPrimBackupTaint(EntityBase sceneObjectGroup)
            {
                lock (m_backupTaintedPrims)
                {
                    if (sceneObjectGroup is SceneObjectGroup)
                    {
                        if (!m_backupTaintedPrims.ContainsKey(sceneObjectGroup.UUID))
                            m_backupTaintedPrims.Add(sceneObjectGroup.UUID, (SceneObjectGroup)sceneObjectGroup);
                    }
                }
            }

            /// <summary>
            /// This is the new backup processor, it only deals with prims that 
            /// have been 'tainted' so that it does not waste time
            /// running through as large of a backup loop
            /// </summary>
            public void ProcessPrimBackupTaints(bool forced, bool backupAll)
            {
                if (m_backingup)
                    return;

                m_backingup = true;
                HashSet<SceneObjectGroup> backupPrims = new HashSet<SceneObjectGroup>();
                //Add all
                if (backupAll)
                {
                    EntityBase[] entities = m_scene.Entities.GetEntities();
                    foreach (EntityBase entity in entities)
                    {
                        if (entity is SceneObjectGroup)
                            backupPrims.Add(entity as SceneObjectGroup);
                    }
                }
                else if (forced)
                {
                    lock (m_backupTaintedPrims)
                    {
                        //Add all these to the backup
                        backupPrims = new HashSet<SceneObjectGroup>(m_backupTaintedPrims.Values);
                        m_backupTaintedPrims.Clear();
                        //Reset the timer
                        runSecondaryBackup = DateTime.Now.AddMinutes((m_dontPersistBefore / 10000000L));

                        if (m_secondaryBackupTaintedPrims.Count != 0)
                        {
                            //Check this set
                            foreach (SceneObjectGroup grp in m_secondaryBackupTaintedPrims.Values)
                            {
                                backupPrims.Add(grp);
                            }
                        }
                        m_secondaryBackupTaintedPrims.Clear();
                    }
                }
                else
                {
                    lock (m_backupTaintedPrims)
                    {
                        if (m_backupTaintedPrims.Count != 0)
                        {
                            backupPrims = new HashSet<SceneObjectGroup>(m_backupTaintedPrims.Values);
                            m_backupTaintedPrims.Clear();
                        }
                    }
                    //The seconary backup storage is so that we do not check every time and kill checking for updates that are not ready to persist yet
                    // So it runs every X minutes depending on how long the minimum persistance time is
                    if (runSecondaryBackup.Ticks < DateTime.Now.Ticks)
                    {
                        //Add the min persistance time to now to get the new time
                        runSecondaryBackup = DateTime.Now.AddMinutes((m_dontPersistBefore / 10000000L));
                        lock (m_secondaryBackupTaintedPrims)
                        {
                            if (m_secondaryBackupTaintedPrims.Count != 0)
                            {
                                //Check this set
                                foreach (SceneObjectGroup grp in m_secondaryBackupTaintedPrims.Values)
                                {
                                    backupPrims.Add(grp);
                                }
                            }
                            m_secondaryBackupTaintedPrims.Clear();
                        }
                        //Add the min persistance time to now to get the new time
                        runSecondaryBackup = DateTime.Now.AddMinutes((m_dontPersistBefore / 10000000L));
                    }
                }
                int PrimsBackedUp = 0;
                foreach (SceneObjectGroup grp in backupPrims)
                {
                    //Check this prim
                    bool shouldReaddToLoop;
                    bool shouldReaddToLoopNow;
                    //If its forced, we do it. If its time, we do it, else, check whether it should be requeued
                    if (!forced && !isTimeForGroupToPersist(grp, out shouldReaddToLoop, out shouldReaddToLoopNow))
                    {
                        if (shouldReaddToLoop)
                        {
                            //Readd it into the seconary backup loop then as its not time for it to backup yet
                            lock (m_secondaryBackupTaintedPrims)
                                lock (m_backupTaintedPrims)
                                    //Make sure its not in either so that we don't duplicate checking
                                    if (!m_secondaryBackupTaintedPrims.ContainsKey(grp.UUID) &&
                                        !m_backupTaintedPrims.ContainsKey(grp.UUID))
                                        m_secondaryBackupTaintedPrims.Add(grp.UUID, grp);
                        }
                        if (shouldReaddToLoopNow)
                        {
                            //Readd it into the seconary backup loop then as its not time for it to backup yet
                            lock (m_backupTaintedPrims)
                                //Make sure its not in either so that we don't duplicate checking
                                if (!m_backupTaintedPrims.ContainsKey(grp.UUID))
                                    m_backupTaintedPrims.Add(grp.UUID, grp);
                        }
                    }
                    else
                    {
                        //Given the ok to backup
                        BackupGroup(grp);
                        PrimsBackedUp++;
                    }
                }
                if (PrimsBackedUp != 0)
                    m_log.Info("[BackupModule]: Processed backup of " + PrimsBackedUp + " prims");
                //Now make sure that we delete any prims sitting around
                // Bit ironic that backup deals with deleting of objects too eh? 
                lock (m_needsDeleted)
                {
                    if (m_needsDeleted.Count != 0)
                    {
                        //Removes all objects in one call
                        m_scene.SimulationDataService.RemoveObjects(m_needsDeleted);
                        m_needsDeleted.Clear();
                    }
                }
                m_backingup = false;
            }

            #endregion

            #region Per Object Backup

            /// <summary>
            /// Returns whether it is time to backup or not
            /// </summary>
            /// <param name="shouldReaddToLoop">Should this prim even be checked again for backup in the secondary loop?</param>
            /// <param name="shouldReaddToLoopNow">Should this prim be added to the immediate loop for next backup?</param>
            /// <returns></returns>
            protected bool isTimeForGroupToPersist(SceneObjectGroup grp, out bool shouldReaddToLoop, out bool shouldReaddToLoopNow)
            {
                shouldReaddToLoop = true;
                shouldReaddToLoopNow = false;

                if (grp.IsDeleted || grp.IsAttachment ||
                    grp.RootPart.Shape.State != 0 || grp.UUID == UUID.Zero)
                {
                    //Do not readd under these circumstances as we don't deal with backing up either of those into sim storage
                    shouldReaddToLoop = false;
                    return false;
                }

                //Forced to backup NOW
                if (grp.m_forceBackupNow)
                {
                    //Revert it
                    grp.m_forceBackupNow = false;
                    return true;
                }
                //If we are shutting down, no more additions should occur
                // NOTE: When we call backup on shutdown, we do a force backup, which ignores this switch, which is why we can safely block this
                if (m_scene.ShuttingDown)
                {
                    //Do not readd now
                    shouldReaddToLoop = false;
                    return false;
                }

                DateTime currentTime = DateTime.Now;
                //If it selected, we want to back it up... but not immediately
                if (grp.IsSelected)
                {
                    //Check the max time for backup as well as it should override IsSelected
                    if ((currentTime - grp.timeFirstChanged).TotalMinutes > m_persistAfter)
                        return true;
                    //Selected prims are probably being changed, add them back for tte next backup
                    shouldReaddToLoopNow = true;
                    return false;
                }
                //Check whether it is between the Min Time and Max Time to backup
                if ((currentTime - grp.timeLastChanged).TotalMinutes > m_dontPersistBefore || (currentTime - grp.timeFirstChanged).TotalMinutes > m_persistAfter)
                    return true;
                return false;
            }

            /// <summary>
            /// Deal with backing up this prim
            /// </summary>
            /// <param name="datastore">Place to save the prim into</param>
            /// <param name="forcedBackup">Is this backup forced?</param>
            /// <param name="shouldReaddToLoop">Should we even check this prim again until it is changed again?</param>
            /// <param name="shouldReaddToLoopNow">Should this prim be readded to the backup loop for immediate checking next loop?</param>
            /// <returns></returns>
            protected bool BackupGroup(SceneObjectGroup grp)
            {
                // Since this is the top of the section of call stack for backing up a particular scene object, don't let
                // any exception propogate upwards.

                // don't backup while it's selected or you're asking for changes mid stream.
                DateTime startTime = DateTime.Now;

                SceneObjectGroup backup_group = (SceneObjectGroup)grp.Copy();
                //Do this we don't try to re-persist to the DB
                backup_group.m_isLoaded = false;
                m_scene.SimulationDataService.StoreObject(backup_group, m_scene.RegionInfo.RegionID);

                //Backup inventory, no lock as this isn't added ANYWHERE but here
                foreach (SceneObjectPart part in backup_group.ChildrenList)
                {
                    part.Inventory.ProcessInventoryBackup(m_scene.SimulationDataService);
                }

                m_log.DebugFormat(
                        "[BackupModule]: Stored {0}, {1} in {2} at {3} in {4} seconds",
                        backup_group.Name, backup_group.UUID, m_scene.RegionInfo.RegionName, backup_group.AbsolutePosition.ToString(), (DateTime.Now - startTime).TotalSeconds);


                grp.HasGroupChanged = false;
                backup_group = null;
                return true;
            }

            #endregion
        }

        #endregion
    }
}
