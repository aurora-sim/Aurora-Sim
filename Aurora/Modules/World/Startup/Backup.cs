/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

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
using OpenSim.Services.Interfaces;

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
            if (MainConsole.Instance != null)
                MainConsole.Instance.Commands.AddCommand ("backup", "backup [all]", "Persist objects to the database now, if [all], will force the persistence of all prims", RunCommand);
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
        }

        #endregion

        #region Console commands

        /// <summary>
        /// Runs commands issued by the server console from the operator
        /// </summary>
        /// <param name="cmdparams">Additional arguments passed to the command</param>
        public void RunCommand(string[] cmdparams)
        {
            m_manager.ForEachCurrentScene(delegate(Scene scene)
                    {
                        scene.AuroraEventManager.FireGenericEventHandler ("Backup", null);
                    });
        }

        #endregion

        #region Per region backup class

        protected class InternalSceneBackup : IBackupModule, IAuroraBackupModule
        {
            #region Declares

            protected static readonly ILog m_log
                = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            protected Scene m_scene;
            protected bool m_LoadingPrims = false;
            protected bool m_haveLoadedPrims = false;
            protected bool m_haveLoadedParcels = false;

            #endregion

            #region Constructor

            public InternalSceneBackup (Scene scene)
            {
                m_scene = scene;
                m_scene.StackModuleInterface<IAuroraBackupModule> (this);
                m_scene.RegisterModuleInterface<IBackupModule> (this);

                if (MainConsole.Instance != null)
                {
                    MainConsole.Instance.Commands.AddCommand ("delete object owner",
                    "delete object owner <UUID>",
                    "Delete object by owner", HandleDeleteObject);
                    MainConsole.Instance.Commands.AddCommand ("delete object creator",
                        "delete object creator <UUID>",
                        "Delete object by creator", HandleDeleteObject);
                    MainConsole.Instance.Commands.AddCommand ("delete object uuid",
                        "delete object uuid <UUID>",
                        "Delete object by uuid", HandleDeleteObject);
                    MainConsole.Instance.Commands.AddCommand ("delete object name",
                        "delete object name <name>",
                        "Delete object by name", HandleDeleteObject);
                }
            }

            #endregion

            #region Console Commands

            private void HandleDeleteObject (string[] cmd)
            {
                if (cmd.Length < 4)
                    return;

                string mode = cmd[2];
                string o = cmd[3];

                List<SceneObjectGroup> deletes = new List<SceneObjectGroup> ();

                UUID match;

                switch (mode)
                {
                    case "owner":
                        if (!UUID.TryParse (o, out match))
                            return;
                        m_scene.ForEachSOG (delegate (SceneObjectGroup g)
                                {
                                    if (g.OwnerID == match && !g.IsAttachment)
                                        deletes.Add (g);
                                });
                        break;
                    case "creator":
                        if (!UUID.TryParse (o, out match))
                            return;
                        m_scene.ForEachSOG (delegate (SceneObjectGroup g)
                                {
                                    if (g.RootPart.CreatorID == match && !g.IsAttachment)
                                        deletes.Add (g);
                                });
                        break;
                    case "uuid":
                        if (!UUID.TryParse (o, out match))
                            return;
                        m_scene.ForEachSOG (delegate (SceneObjectGroup g)
                                {
                                    if (g.UUID == match && !g.IsAttachment)
                                        deletes.Add (g);
                                });
                        break;
                    case "name":
                        m_scene.ForEachSOG (delegate (SceneObjectGroup g)
                                {
                                    if (g.RootPart.Name == o && !g.IsAttachment)
                                        deletes.Add (g);
                                });
                        break;
                }

                foreach (SceneObjectGroup g in deletes)
                    DeleteSceneObject (g, true, true);
            }

            #endregion

            #region Scene events

            /// <summary>
            /// Loads the World's objects
            /// </summary>
            public void LoadPrimsFromStorage()
            {
                LoadingPrims = true;
                m_log.Info("[BackupModule]: Loading objects from datastore");

                List<ISceneEntity> PrimsFromDB = m_scene.SimulationDataService.LoadObjects(m_scene);
                foreach (ISceneEntity group in PrimsFromDB)
                {
                    try
                    {
                        if (group.IsAttachment || (group.RootChild.Shape != null && (group.RootChild.Shape.State != 0 &&
                            (group.RootChild.Shape.PCode == (byte)PCode.None ||
                            group.RootChild.Shape.PCode == (byte)PCode.Prim ||
                            group.RootChild.Shape.PCode == (byte)PCode.Avatar))))
                        {
                            m_log.Warn("[BackupModule]: Broken state for object " + group.Name + " while loading objects, removing it from the database.");
                            //WTF went wrong here? Remove by passing it by on loading
                            continue;
                        }
                        else if (group.RootChild.Shape == null)
                        {
                            m_log.Warn("[BackupModule]: Broken object (" + group.Name + ") found while loading objects, removing it from the database.");
                            //WTF went wrong here? Remove by passing it by on loading
                            continue;
                        }
                        else if (group.AbsolutePosition.X > m_scene.RegionInfo.RegionSizeX + 10 ||
                            group.AbsolutePosition.X < -10 ||
                            group.AbsolutePosition.Y > m_scene.RegionInfo.RegionSizeY + 10 ||
                            group.AbsolutePosition.Y < -10)
                        {
                            m_log.Warn ("[BackupModule]: Object outside the region (" + group.Name + ", " + group.AbsolutePosition + ") found while loading objects, removing it from the database.");
                            //WTF went wrong here? Remove by passing it by on loading
                            continue;
                        }
                        m_scene.SceneGraph.CheckAllocationOfLocalIds (group);
                        group.Scene = m_scene;

                        if (group.RootChild == null)
                        {
                            m_log.ErrorFormat("[BackupModule] Found a SceneObjectGroup with m_rootPart == null and {0} children",
                                              group.ChildrenEntities().Count);
                            continue;
                        }
                        m_scene.SceneGraph.RestorePrimToScene(group);
                        ISceneChildEntity rootPart = group.GetChildPart(group.UUID);
                        rootPart.Flags &= ~PrimFlags.Scripted;
                        rootPart.TrimPermissions();
                    }
                    catch(Exception ex)
                    {
                        m_log.WarnFormat("[BackupModule]: Exception attempting to load object from the database, {0}, continuing...", ex.ToString());
                    }
                }
                LoadingPrims = false;
                m_log.Info("[BackupModule]: Loaded " + PrimsFromDB.Count.ToString() + " SceneObject(s)");
                PrimsFromDB.Clear ();
            }

            /// <summary>
            /// Loads all Parcel data from the datastore for region identified by regionID
            /// </summary>
            public void LoadAllLandObjectsFromStorage()
            {
                m_log.Info ("[BackupModule]: Loading Land Objects from database... ");
                m_scene.EventManager.TriggerIncomingLandDataFromStorage(m_scene.SimulationDataService.LoadLandObjects(m_scene.RegionInfo.RegionID));
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
            /// Delete every object from the scene.  This does not include attachments worn by avatars.
            /// </summary>
            public void DeleteAllSceneObjects()
            {
                try
                {
                    LoadingPrims = true;
                    List<SceneObjectGroup> groups = new List<SceneObjectGroup>();
                    lock (m_scene.Entities)
                    {
                        ISceneEntity[] entities = m_scene.Entities.GetEntities();
                        foreach (ISceneEntity entity in entities)
                        {
                            if (entity is SceneObjectGroup && !((SceneObjectGroup)entity).IsAttachment)
                                groups.Add((SceneObjectGroup)entity);
                        }
                    }
                    //Delete all the groups now
                    DeleteSceneObjects(groups.ToArray(), true);

                    //Now remove the entire region at once
                    m_scene.SimulationDataService.RemoveRegion(m_scene.RegionInfo.RegionID);
                    LoadingPrims = false;
                }
                catch
                {
                }
            }

            public void ResetRegionToStartupDefault ()
            {
                m_haveLoadedPrims = false;
                //Add the loading prims piece just to be safe
                LoadingPrims = true;

                try
                {
                    lock (m_scene.Entities)
                    {
                        ISceneEntity[] entities = m_scene.Entities.GetEntities();
                        foreach (ISceneEntity entity in entities)
                        {
                            if (entity is SceneObjectGroup && !((SceneObjectGroup)entity).IsAttachment)
                            {
                                List<SceneObjectPart> parts = new List<SceneObjectPart>();
                                SceneObjectGroup grp = entity as SceneObjectGroup;
                                parts.AddRange(grp.ChildrenList);
                                DeleteSceneObject(grp, true, false); //Don't remove from the database
                                m_scene.ForEachScenePresence(delegate(IScenePresence avatar)
                                {
                                    avatar.ControllingClient.SendKillObject(m_scene.RegionInfo.RegionHandle, parts.ToArray());
                                });
                            }
                        }
                    }
                }
                catch
                {
                }

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
                //Tell the database that something has changed
                m_scene.SimulationDataService.Tainted ();
            }

            #endregion

            #region Per Object Methods

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
                    if (part.PhysActor != null)
                    {
                        m_scene.PhysicsScene.RemovePrim(part.PhysActor);
                        part.PhysActor = null;
                    }
                }

                m_scene.SimulationDataService.Tainted ();
                if (m_scene.SceneGraph.DeleteEntity(group))
                {
                    // We need to keep track of this state in case this group is still queued for backup.
                    group.IsDeleted = true;
                    //Clear the update schedule HERE so that IsDeleted will not have to fire as well
                    
                    foreach (SceneObjectPart part in group.ChildrenList)
                    {
                        //Make sure it isn't going to be updated again
                        part.ClearUpdateSchedule ();
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
            private List<LandData> m_parcels = new List<LandData>();
            private bool m_merge = false;

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
                        parcelMap = null;
                    }
                }

                m_log.Info("[Archive]: Finished writing parcels to archive");
                m_log.Info ("[Archive]: Writing terrain to archive");

                writer.WriteDir ("terrain");
                writer.WriteDir ("revertterrain");

                writer.WriteDir ("water");
                writer.WriteDir ("revertwater");

                ITerrainModule tModule = scene.RequestModuleInterface<ITerrainModule> ();
                if (tModule != null)
                {
                    try
                    {
                        MemoryStream s = new MemoryStream ();
                        tModule.SaveToStream (tModule.TerrainMap, scene.RegionInfo.RegionID.ToString () + ".r32", s);
                        writer.WriteFile ("terrain/" + scene.RegionInfo.RegionID.ToString () + ".r32", s.ToArray ());
                        s.Close ();
                        s = null;
                        s = new MemoryStream ();
                        tModule.SaveToStream (tModule.TerrainRevertMap, scene.RegionInfo.RegionID.ToString () + ".r32", s);
                        writer.WriteFile ("revertterrain/" + scene.RegionInfo.RegionID.ToString () + ".r32", s.ToArray ());
                        s.Close ();
                        s = null;
                        if (tModule.TerrainWaterMap != null)
                        {
                            s = new MemoryStream ();
                            tModule.SaveToStream (tModule.TerrainWaterMap, scene.RegionInfo.RegionID.ToString () + ".r32", s);
                            writer.WriteFile ("water/" + scene.RegionInfo.RegionID.ToString () + ".r32", s.ToArray ());
                            s.Close ();
                            s = null;
                            s = new MemoryStream ();
                            tModule.SaveToStream (tModule.TerrainWaterRevertMap, scene.RegionInfo.RegionID.ToString () + ".r32", s);
                            writer.WriteFile ("revertwater/" + scene.RegionInfo.RegionID.ToString () + ".r32", s.ToArray ());
                            s.Close ();
                            s = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        m_log.WarnFormat ("[Backup]: Exception caught: {0}", ex.ToString ());
                    }
                }
                
                m_log.Info("[Archive]: Finished writing terrain to archive");
                m_log.Info("[Archive]: Writing entities to archive");
                ISceneEntity[] entities = scene.Entities.GetEntities();
                //Get all entities, then start writing them to the database
                writer.WriteDir("entities");

                IDictionary<UUID, AssetType> assets = new Dictionary<UUID, AssetType>();
                UuidGatherer assetGatherer = new UuidGatherer(m_scene.AssetService);
                IAuroraBackupArchiver archiver = m_scene.RequestModuleInterface<IAuroraBackupArchiver> ();
                bool saveAssets = false;
                if(archiver.AllowPrompting)
                    saveAssets = MainConsole.Instance.CmdPrompt ("Save assets? (Will not be able to load on other grids)", "false").Equals ("true", StringComparison.CurrentCultureIgnoreCase);

                int count = 0;
                foreach (ISceneEntity entity in entities)
                {
                    try
                    {
                        if (entity.IsAttachment && !((entity.RootChild.Flags & PrimFlags.Temporary) == PrimFlags.Temporary)
                             && !((entity.RootChild.Flags & PrimFlags.TemporaryOnRez) == PrimFlags.TemporaryOnRez))
                            continue;
                        //Write all entities
                        byte[] xml = ((ISceneObject)entity).ToBinaryXml2 ();
                        writer.WriteFile ("entities/" + entity.UUID.ToString (), xml);
                        xml = null;
                        count++;
                        if (count % 5 == 0)
                            Thread.Sleep (1);
                        //Get all the assets too
                        if (saveAssets)
                            assetGatherer.GatherAssetUuids (entity, assets, scene);
                    }
                    catch (Exception ex)
                    {
                        m_log.WarnFormat ("[Backup]: Exception caught: {0}", ex.ToString ());
                    }
                }
                entities = null;

                m_log.Info("[Archive]: Finished writing entities to archive");
                m_log.Info("[Archive]: Writing assets for entities to archive");

                bool foundAllAssets = true;
                foreach (UUID assetID in new List<UUID> (assets.Keys))
                {
                    try
                    {
                        AssetBase asset = m_scene.AssetService.GetCached (assetID.ToString ());
                        if (asset != null)
                            WriteAsset (assetID.ToString (), asset, writer); //Write it syncronously since we havn't 
                        else
                        {
                            foundAllAssets = false; //Not all are cached
                            m_missingAssets.Add (assetID);
                            m_scene.AssetService.Get (assetID.ToString (), writer, RetrievedAsset);
                        }
                    }
                    catch (Exception ex)
                    {
                        m_log.WarnFormat ("[Backup]: Exception caught: {0}", ex.ToString ());
                    }
                }
                if (foundAllAssets)
                    m_isArchiving = false; //We're done if all the assets were found

                m_log.Info("[Archive]: Finished writing assets for entities to archive");
            }

            private byte[] ShortToByte(short[] ter)
            {
                byte[] heightmap = new byte[ter.Length * sizeof(short)];
                int ii = 0;
                for (int i = 0; i < ter.Length; i++)
                {
                    Utils.Int16ToBytes(ter[i], heightmap, ii);
                    ii += 2;
                }
                return heightmap;
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
                WriteAsset(id, asset, writer);
                if (m_missingAssets.Count == 0)
                    m_isArchiving = false;
            }

            private void WriteAsset(string id, AssetBase asset, TarArchiveWriter writer)
            {
                if (asset != null)
                    writer.WriteFile ("assets/" + asset.ID, asset.Data);
                else
                    m_log.WarnFormat ("Could not find asset {0}", id);
            }

            public void BeginLoadModuleFromArchive(IScene scene)
            {
                IBackupModule backup = m_scene.RequestModuleInterface<IBackupModule>();
                IScriptModule[] modules = m_scene.RequestModuleInterfaces<IScriptModule>();
                IParcelManagementModule parcelModule = scene.RequestModuleInterface<IParcelManagementModule>();
                //Disable the script engine so that it doesn't load in the background and kill OAR loading
                foreach (IScriptModule module in modules)
                {
                    if(module != null)
                        module.Disabled = true;
                }
                //Disable backup for now as well
                if (backup != null)
                {
                    backup.LoadingPrims = true;
                    m_merge = MainConsole.Instance.CmdPrompt("Should we merge prims together (keep the prims from the old region too)?", "false") == "true";
                    if (!m_merge)
                    {
                        DateTime before = DateTime.Now;
                        m_log.Info("[ARCHIVER]: Clearing all existing scene objects");
                        backup.DeleteAllSceneObjects();
                        m_log.Info("[ARCHIVER]: Cleared all existing scene objects in " + (DateTime.Now - before).Minutes + ":" + (DateTime.Now - before).Seconds);
                        if (parcelModule != null)
                            parcelModule.ClearAllParcels ();
                    }
                }
            }

            public void EndLoadModuleFromArchive(IScene scene)
            {
                IBackupModule backup = m_scene.RequestModuleInterface<IBackupModule>();
                IScriptModule[] modules = m_scene.RequestModuleInterfaces<IScriptModule>();
                //Reeanble now that we are done
                foreach (IScriptModule module in modules)
                {
                    module.Disabled = false;
                }
                //Reset backup too
                if (backup != null)
                    backup.LoadingPrims = false;

                //Update the database as well!
                IParcelManagementModule parcelManagementModule = m_scene.RequestModuleInterface<IParcelManagementModule>();
                if (parcelManagementModule != null && !m_merge) //Only if we are not merging
                {
                    if (m_parcels.Count > 0)
                    {
                        m_scene.EventManager.TriggerIncomingLandDataFromStorage (m_parcels);
                        //Update the database as well!
                        if (parcelManagementModule != null)
                        {
                            foreach (LandData parcel in m_parcels)
                            {
                                parcelManagementModule.UpdateLandObject (parcel.LocalID, parcel);
                            }
                        }
                    }
                    else if (parcelManagementModule != null)
                        parcelManagementModule.ResetSimLandObjects ();
                    m_parcels.Clear();
                }
            }

            public void LoadModuleFromArchive(byte[] data, string filePath, TarArchiveReader.TarEntryType type, IScene scene)
            {
                if (filePath.StartsWith("parcels/"))
                {
                    if (!m_merge)
                    {
                        //Only use if we are not merging
                        LandData parcel = new LandData();
                        OSD parcelData = OSDParser.DeserializeLLSDBinary(data);
                        parcel.FromOSD((OSDMap)parcelData);
                        m_parcels.Add(parcel);
                    }
                }
                else if (filePath.StartsWith ("terrain/"))
                {
                    ITerrainModule terrainModule = m_scene.RequestModuleInterface<ITerrainModule> ();

                    MemoryStream ms = new MemoryStream (data);
                    terrainModule.LoadFromStream (filePath, ms, 0, 0);
                    ms.Close ();
                }
                else if (filePath.StartsWith ("revertterrain/"))
                {
                    ITerrainModule terrainModule = m_scene.RequestModuleInterface<ITerrainModule> ();

                    MemoryStream ms = new MemoryStream (data);
                    terrainModule.LoadRevertMapFromStream (filePath, ms, 0, 0);
                    ms.Close ();
                }
                else if (filePath.StartsWith ("water/"))
                {
                    ITerrainModule terrainModule = m_scene.RequestModuleInterface<ITerrainModule> ();

                    MemoryStream ms = new MemoryStream (data);
                    terrainModule.LoadWaterFromStream (filePath, ms, 0, 0);
                    ms.Close ();
                }
                else if (filePath.StartsWith ("revertwater/"))
                {
                    ITerrainModule terrainModule = m_scene.RequestModuleInterface<ITerrainModule> ();

                    MemoryStream ms = new MemoryStream (data);
                    terrainModule.LoadWaterRevertMapFromStream (filePath, ms, 0, 0);
                    ms.Close ();
                }
                else if (filePath.StartsWith("entities/"))
                {
                    MemoryStream ms = new MemoryStream(data);
                    SceneObjectGroup sceneObject = OpenSim.Region.Framework.Scenes.Serialization.SceneObjectSerializer.FromXml2Format(ms, (Scene)scene);
                    foreach (SceneObjectPart part in sceneObject.ChildrenList)
                    {
                        if (!ResolveUserUuid(part.CreatorID))
                            part.CreatorID = m_scene.RegionInfo.EstateSettings.EstateOwner;

                        if (!ResolveUserUuid(part.OwnerID))
                            part.OwnerID = m_scene.RegionInfo.EstateSettings.EstateOwner;

                        if (!ResolveUserUuid(part.LastOwnerID))
                            part.LastOwnerID = m_scene.RegionInfo.EstateSettings.EstateOwner;

                        // Fix ownership/creator of inventory items
                        // Not doing so results in inventory items
                        // being no copy/no mod for everyone
                        lock (part.TaskInventory)
                        {
                            TaskInventoryDictionary inv = part.TaskInventory;
                            foreach (KeyValuePair<UUID, TaskInventoryItem> kvp in inv)
                            {
                                if (!ResolveUserUuid(kvp.Value.OwnerID))
                                {
                                    kvp.Value.OwnerID = m_scene.RegionInfo.EstateSettings.EstateOwner;
                                }
                                if (!ResolveUserUuid(kvp.Value.CreatorID))
                                {
                                    kvp.Value.CreatorID = m_scene.RegionInfo.EstateSettings.EstateOwner;
                                }
                            }
                        }
                    }

                    if (m_scene.SceneGraph.AddPrimToScene(sceneObject))
                    {
                        sceneObject.HasGroupChanged = true;
                        sceneObject.ScheduleGroupUpdate (PrimUpdateFlags.ForcedFullUpdate);
                        sceneObject.CreateScriptInstances(0, false, 0, UUID.Zero);
                        sceneObject.ResumeScripts();
                    }
                }
            }

            private bool ResolveUserUuid (UUID uuid)
            {
                return m_scene.UserAccountService.GetUserAccount (m_scene.RegionInfo.ScopeID, uuid) != null;
            }

            #endregion
        }

        #endregion
    }
}
