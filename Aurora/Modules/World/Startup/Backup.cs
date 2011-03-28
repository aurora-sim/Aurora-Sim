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
using System.Xml;
using Nini.Config;
using log4net;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.DataManager;
using Aurora.Framework;
using OpenSim.Framework;
using OpenSim.Framework.Serialization;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace Aurora.Modules
{
    public class Backup : ISharedRegionStartupModule
    {
        #region Declares

        protected static readonly ILog m_log
                = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
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
        }

        public void PostFinishStartup(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
            m_manager = scene.RequestModuleInterface<SceneManager>();
            m_backup[scene].FinishStartup();
        }

        public void StartupComplete()
        {
        }

        public void Close(Scene scene)
        {
            m_log.Info("[BackupModule]: Persisting changed objects in scene " + scene.RegionInfo.RegionName + "...");
            while (m_backup[scene].IsBackingUp)
            {
                //Wait until other threads are done with backup before backing up so that we get everything
                Thread.Sleep(100);
            }
            m_backup[scene].ProcessPrimBackupTaints(true, false);
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

        protected class InternalSceneBackup : IBackupModule, IAuroraBackupModule
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
            protected bool m_haveLoadedPrims = false;
            protected bool m_haveLoadedParcels = false;
            protected bool m_shouldLoadPrims = true;
            protected bool m_shouldLoadParcels = true;

            public bool LoadParcels
            {
                get { return m_shouldLoadParcels; }
                set { m_shouldLoadParcels = value; }
            }

            public bool LoadPrims
            {
                get { return m_shouldLoadPrims; }
                set { m_shouldLoadPrims = value; }
            }

            #endregion

            #region Constructor

            public InternalSceneBackup(Scene scene)
            {
                m_scene = scene;
                m_scene.StackModuleInterface<IAuroraBackupModule>(this);
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
                        m_lastRanBackupInHeartbeat = DateTime.Now.AddMinutes((m_dontPersistBefore / 60));
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
            public void LoadPrimsFromStorage()
            {
                if (m_haveLoadedPrims || !m_shouldLoadPrims)
                    return;
                m_haveLoadedPrims = true;
                LoadingPrims = true;
                m_log.Info("[BackupModule]: Loading objects from datastore");

                List<SceneObjectGroup> PrimsFromDB = m_scene.SimulationDataService.LoadObjects(m_scene.RegionInfo.RegionID, m_scene);
                foreach (SceneObjectGroup group in PrimsFromDB)
                {
                    try
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
                        else if (group.RootPart.Shape == null)
                        {
                            m_log.Warn("[BackupModule]: Broken object (" + group.Name + ") found while loading objects, removing it from the database.");
                            //WTF went wrong here? Remove it and then pass it by on loading
                            m_scene.SimulationDataService.RemoveObject(group.UUID, m_scene.RegionInfo.RegionID);
                            continue;
                        }
                        group.Scene = m_scene;

                        if (group.RootPart == null)
                        {
                            m_log.ErrorFormat("[BackupModule] Found a SceneObjectGroup with m_rootPart == null and {0} children",
                                              group.ChildrenList.Count);
                            continue;
                        }
                        m_scene.SceneGraph.RestorePrimToScene(group);
                        SceneObjectPart rootPart = (SceneObjectPart)group.GetChildPart(group.UUID);
                        rootPart.Flags &= ~PrimFlags.Scripted;
                        rootPart.TrimPermissions();
                        group.CheckSculptAndLoad();
                    }
                    catch(Exception ex)
                    {
                        m_log.WarnFormat("[BackupModule]: Exception attempting to load object from the database, {0}, removing...", ex.ToString());
                        m_scene.SimulationDataService.RemoveObject(group.UUID, m_scene.RegionInfo.RegionID);
                    }
                }
                LoadingPrims = false;
                m_log.Info("[BackupModule]: Loaded " + PrimsFromDB.Count.ToString() + " SceneObject(s)");
            }

            /// <summary>
            /// Loads all Parcel data from the datastore for region identified by regionID
            /// </summary>
            public void LoadAllLandObjectsFromStorage()
            {
                if (m_haveLoadedParcels || !m_shouldLoadParcels)
                    return;
                m_haveLoadedParcels = true;

                m_log.Info("[BackupModule]: Loading Land Objects from database... ");
                IParcelServiceConnector conn = DataManager.DataManager.RequestPlugin<IParcelServiceConnector>();
                List<LandData> LandObjects = m_scene.SimulationDataService.LoadLandObjects(m_scene.RegionInfo.RegionID);
                if (conn != null)
                {
                    //Read from the old database as well
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
            }

            public void FinishStartup()
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
            public void CreateScriptInstances()
            {
                m_log.Info("[BackupModule]: Starting scripts in " + m_scene.RegionInfo.RegionName);
                //Set loading prims here to block backup
                LoadingPrims = true;
                ISceneEntity[] entities = m_scene.Entities.GetEntities();
                foreach (ISceneEntity group in entities)
                {
                    if (group is SceneObjectGroup)
                    {
                        ((SceneObjectGroup)group).CreateScriptInstances(0, false, 0, UUID.Zero);
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
            /// Are we currently backing up?
            /// </summary>
            public bool IsBackingUp
            {
                get { return m_backingup; }
                set { m_backingup = value; }
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
            /// Remove all prims in the queue to be deleted
            /// </summary>
            /// <param name="uuid"></param>
            public void ClearDeleteFromStorage()
            {
                lock (m_needsDeleted)
                {
                    m_needsDeleted.Clear();
                }
            }

            /// <summary>
            /// Delete every object from the scene.  This does not include attachments worn by avatars.
            /// </summary>
            public void DeleteAllSceneObjects()
            {
                //We are doing a heavy operation, suspend backup
                m_backingup = true;

                List<SceneObjectGroup> groups = new List<SceneObjectGroup>();
                lock (m_scene.Entities)
                {
                    ISceneEntity[] entities = m_scene.Entities.GetEntities ();
                    foreach (ISceneEntity entity in entities)
                    {
                        if(entity is SceneObjectGroup && !((SceneObjectGroup)entity).IsAttachment)
                            groups.Add((SceneObjectGroup)entity);
                    }
                }
                //Delete all the groups now
                DeleteSceneObjects(groups.ToArray(), true);

                //Clear the queue so that we don't try to remove the prims twice
                ClearDeleteFromStorage();

                //Now remove the entire region at once
                m_scene.SimulationDataService.RemoveRegion(m_scene.RegionInfo.RegionID);

                //All clear, let backup go
                m_backingup = false;
            }

            public void ResetRegionToStartupDefault ()
            {
                while (IsBackingUp || m_backupTaintedPrims.Count != 0 || m_secondaryBackupTaintedPrims.Count != 0)
                {
                    //Wait until other threads are done with backup before backing up so that we get everything
                    Thread.Sleep (100);
                }
                m_haveLoadedPrims = false;
                //Add the loading prims piece just to be safe
                LoadingPrims = true;

                //We are doing a heavy operation, suspend backup
                m_backingup = true;

                //Clear the queue so that we don't try to remove the prims twice
                ClearDeleteFromStorage ();

                lock (m_scene.Entities)
                {
                    ISceneEntity[] entities = m_scene.Entities.GetEntities ();
                    foreach (ISceneEntity entity in entities)
                    {
                        if (entity is SceneObjectGroup && !((SceneObjectGroup)entity).IsAttachment)
                        {
                            List<SceneObjectPart> parts = new List<SceneObjectPart> ();
                            SceneObjectGroup grp = entity as SceneObjectGroup;
                            parts.AddRange (grp.ChildrenList);
                            DeleteSceneObject (grp, true, false); //Don't remove from the database
                            m_scene.ForEachScenePresence (delegate (IScenePresence avatar)
                            {
                                avatar.ControllingClient.SendKillObject (m_scene.RegionInfo.RegionHandle, parts.ToArray ());
                            });
                        } 
                    }
                }

                //Clear the queue so that we don't try to remove the prims twice
                ClearDeleteFromStorage ();

                //All clear, let backup go
                m_backingup = false;

                LoadingPrims = false;
            }

            /// <summary>
            /// Synchronously delete the objects from the scene.
            /// This does send kill object updates and resets the parcel prim counts.
            /// </summary>
            /// <param name="groups"></param>
            /// <param name="DeleteScripts"></param>
            /// <returns></returns>
            public bool DeleteSceneObjects (ISceneEntity[] groups, bool DeleteScripts)
            {
                List<SceneObjectPart> parts = new List<SceneObjectPart>();
                foreach (ISceneEntity grp in groups)
                {
                    SceneObjectGroup group = grp as SceneObjectGroup;
                    if (grp == null)
                        continue;
                    //if (group.IsAttachment)
                    //    continue;
                    parts.AddRange(group.ChildrenList);
                    DeleteSceneObject(group, true, true);
                }
                m_scene.ForEachScenePresence(delegate(IScenePresence avatar)
                {
                    avatar.ControllingClient.SendKillObject(m_scene.RegionInfo.RegionHandle, parts.ToArray());
                });

                return true;
            }

            /// <summary>
            /// Add a backup taint to the prim
            /// </summary>
            /// <param name="sceneObjectGroup"></param>
            public void AddPrimBackupTaint (ISceneEntity sceneObjectGroup)
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
                try
                {
                    HashSet<SceneObjectGroup> backupPrims = new HashSet<SceneObjectGroup>();
                    //Add all
                    if (backupAll)
                    {
                        ISceneEntity[] entities = m_scene.Entities.GetEntities();
                        foreach (ISceneEntity entity in entities)
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
                            runSecondaryBackup = DateTime.Now.AddMinutes(((double)m_dontPersistBefore / 60));

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
                            runSecondaryBackup = DateTime.Now.AddMinutes(((double)m_dontPersistBefore / 60));
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
                            runSecondaryBackup = DateTime.Now.AddMinutes(((double)m_dontPersistBefore / 60));
                        }
                    }
                    int PrimsBackedUp = 0;
                    foreach (SceneObjectGroup grp in backupPrims)
                    {
                        try
                        {
                            //Check this prim
                            bool shouldReaddToLoop;
                            bool shouldReaddToLoopNow;
                            //If its forced, we do it. If its time, we do it, else, check whether it should be requeued
                            if (!forced && !isTimeForGroupToPersist (grp, out shouldReaddToLoop, out shouldReaddToLoopNow))
                            {
                                if (shouldReaddToLoop)
                                {
                                    //Readd it into the seconary backup loop then as its not time for it to backup yet
                                    lock (m_secondaryBackupTaintedPrims)
                                        lock (m_backupTaintedPrims)
                                            //Make sure its not in either so that we don't duplicate checking
                                            if (!m_secondaryBackupTaintedPrims.ContainsKey (grp.UUID) &&
                                                !m_backupTaintedPrims.ContainsKey (grp.UUID))
                                                m_secondaryBackupTaintedPrims.Add (grp.UUID, grp);
                                }
                                if (shouldReaddToLoopNow)
                                {
                                    //Readd it into the seconary backup loop then as its not time for it to backup yet
                                    lock (m_backupTaintedPrims)
                                        //Make sure its not in either so that we don't duplicate checking
                                        if (!m_backupTaintedPrims.ContainsKey (grp.UUID))
                                            m_backupTaintedPrims.Add (grp.UUID, grp);
                                }
                            }
                            else
                            {
                                //Given the ok to backup
                                BackupGroup (grp);
                                PrimsBackedUp++;
                            }
                        }
                        catch(Exception ex)
                        {
                            m_log.Error ("[BackupModule] Error while ProcessPrimBackupTaints ", ex);
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
                }
                catch (Exception e)
                {
                    m_log.Error ("[BackupModule] Error while ProcessPrimBackupTaints ", e);
                    throw;
                }
                finally
                {
                    m_backingup = false;
                }
            }

            #endregion

            #region Per Object Methods

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
                    grp.RootPart.Shape.State != 0 || grp.UUID == UUID.Zero || (grp.RootPart.Flags & PrimFlags.Temporary) != 0)
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

                //SceneObjectGroup backup_group = (SceneObjectGroup)grp.Copy(true);
                //Do this we don't try to re-persist to the DB
                //backup_group.m_isLoaded = false;
                m_scene.SimulationDataService.StoreObject(grp, m_scene.RegionInfo.RegionID);

                //Backup inventory, no lock as this isn't added ANYWHERE but here
                foreach (SceneObjectPart part in grp.ChildrenList)
                {
                    part.Inventory.ProcessInventoryBackup();
                }

                m_log.DebugFormat(
                        "[BackupModule]: Stored {0}, {1} in {2} at {3} in {4} seconds",
                        grp.Name, grp.UUID, m_scene.RegionInfo.RegionName, grp.AbsolutePosition.ToString(), (DateTime.Now - startTime).TotalSeconds);


                grp.HasGroupChanged = false;
                //backup_group = null;
                return true;
            }

            /// <summary>
            /// Synchronously delete the given object from the scene.
            /// </summary>
            /// <param name="group">Object Id</param>
            /// <param name="DeleteScripts">Remove the scripts from the ScriptEngine as well</param>
            /// <param name="removeFromDatabase">Remove from the database?</param>
            protected bool DeleteSceneObject(SceneObjectGroup group, bool DeleteScripts, bool removeFromDatabase)
            {
                //m_log.DebugFormat("[Backup]: Deleting scene object {0} {1}", group.Name, group.UUID);

                lock (group.RootPart.SitTargetAvatar)
                {
                    if (group.RootPart.SitTargetAvatar.Count != 0)
                    {
                        UUID[] ids = new UUID[group.RootPart.SitTargetAvatar.Count];
                        group.RootPart.SitTargetAvatar.CopyTo(ids);
                        foreach (UUID avID in ids)
                        {
                            IScenePresence SP = m_scene.GetScenePresence(avID);
                            if (SP != null)
                                SP.StandUp();
                        }
                    }
                }

                // Serialise calls to RemoveScriptInstances to avoid
                // deadlocking on m_parts inside SceneObjectGroup
                if (DeleteScripts)
                {
                    group.RemoveScriptInstances(true);
                }

                foreach (SceneObjectPart part in group.ChildrenList)
                {
                    if (part.IsJoint() && ((part.Flags & PrimFlags.Physics) != 0))
                    {
                        m_scene.PhysicsScene.RequestJointDeletion(part.Name); // FIXME: what if the name changed?
                    }
                    else if (part.PhysActor != null)
                    {
                        m_scene.PhysicsScene.RemovePrim(part.PhysActor);
                        part.PhysActor = null;
                    }
                }

                if (m_scene.SceneGraph.DeleteEntity(group))
                {
                    if(removeFromDatabase)
                        DeleteFromStorage(group.UUID);

                    // We need to keep track of this state in case this group is still queued for backup.
                    group.IsDeleted = true;
                    //Clear the update schedule HERE so that IsDeleted will not have to fire as well
                    lock (group.ChildrenListLock)
                    {
                        foreach (SceneObjectPart part in group.ChildrenList)
                        {
                            //Make sure it isn't going to be updated again
                            part.ClearUpdateSchedule();
                        }
                    }
                    m_scene.EventManager.TriggerObjectBeingRemovedFromScene(group);
                    return true;
                }

                //m_log.DebugFormat("[SCENE]: Exit DeleteSceneObject() for {0} {1}", group.Name, group.UUID);
                return false;
            }

            #endregion

            #region IAuroraBackupModule Methods

            private bool m_isArchiving = false;
            private List<UUID> m_missingAssets = new List<UUID>();

            public bool IsArchiving
            {
                get { return m_isArchiving; }
            }

            public void SaveModuleToArchive(TarArchiveWriter writer, IScene scene)
            {
                m_isArchiving = true;

                m_log.Info("[Archive]: Writing parcels to archive");

                writer.WriteDir("parcels");

                IParcelManagementModule module = scene.RequestModuleInterface<IParcelManagementModule>();
                if (module != null)
                {
                    List<ILandObject> landObject = module.AllParcels();
                    foreach (ILandObject parcel in landObject)
                    {
                        OSDMap parcelMap = parcel.LandData.ToOSD();
                        writer.WriteFile("parcels/" + parcel.LandData.GlobalID.ToString(), OSDParser.SerializeLLSDBinary(parcelMap));
                    }
                }

                m_log.Info("[Archive]: Finished writing parcels to archive");
                m_log.Info("[Archive]: Writing entities to archive");
                ISceneEntity[] entities = scene.Entities.GetEntities();
                //Get all entities, then start writing them to the database
                writer.WriteDir("entities");

                IDictionary<UUID, AssetType> assets = new Dictionary<UUID, AssetType>();
                UuidGatherer assetGatherer = new UuidGatherer(m_scene.AssetService);

                foreach (ISceneEntity entity in entities)
                {
                    //Write all entities
                    writer.WriteFile("entities/" + entity.UUID.ToString(), ((ISceneObject)entity).ToXml2());
                    //Get all the assets too
                    assetGatherer.GatherAssetUuids(entity, assets, scene);
                }

                m_log.Info("[Archive]: Finished writing entities to archive");
                m_log.Info("[Archive]: Writing assets for entities to archive");

                bool foundAllAssets = true;
                foreach (UUID assetID in new List<UUID>(assets.Keys))
                {
                    AssetBase asset = m_scene.AssetService.GetCached(assetID.ToString());
                    if (asset != null)
                        WriteAsset(asset, writer); //Write it syncronously since we havn't 
                    else
                    {
                        foundAllAssets = false; //Not all are cached
                        m_missingAssets.Add(assetID);
                        m_scene.AssetService.Get(assetID.ToString(), writer, RetrievedAsset);
                    }
                }
                if (foundAllAssets)
                    m_isArchiving = false; //We're done if all the assets were found

                m_log.Info("[Archive]: Finished writing assets for entities to archive");
            }

            private void RetrievedAsset(string id, Object sender, AssetBase asset)
            {
                m_missingAssets.Remove(UUID.Parse(id));
                TarArchiveWriter writer = (TarArchiveWriter)sender;
                if (writer == null)
                {
                    if (m_missingAssets.Count == 0)
                        m_isArchiving = false;
                    return;
                }
                //Add the asset
                WriteAsset(asset, writer);
                if (m_missingAssets.Count == 0)
                    m_isArchiving = false;
            }

            private void WriteAsset(AssetBase asset, TarArchiveWriter writer)
            {
                writer.WriteFile("assets", asset.Data);
            }

            #endregion
        }

        #endregion
    }
}
