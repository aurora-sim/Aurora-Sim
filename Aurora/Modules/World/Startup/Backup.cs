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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using Aurora.Framework.Serialization;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace Aurora.Modules.Startup
{
    public class Backup : ISharedRegionStartupModule
    {
        #region Declares

        protected ISceneManager m_manager;
        protected Dictionary<IScene, InternalSceneBackup> m_backup = new Dictionary<IScene, InternalSceneBackup>();

        #endregion

        #region ISharedRegionStartupModule Members

        public void Initialise(IScene scene, IConfigSource source, ISimulationBase openSimBase)
        {
            if (MainConsole.Instance != null && m_backup.Count == 0)//Only add them once
            {
                MainConsole.Instance.Commands.AddCommand ("backup", "backup", "Persist objects to the database now, if [all], will force the persistence of all prims", RunCommand);
                MainConsole.Instance.Commands.AddCommand ("disable backup", "disable backup", "Disables persistance until reenabled", DisableBackup);
                MainConsole.Instance.Commands.AddCommand ("enable backup", "disable backup", "Enables persistance after 'disable persistance' has been run", EnableBackup);
            }
            //Set up the backup for the scene
            m_backup[scene] = new InternalSceneBackup(scene);
        }

        public void PostInitialise(IScene scene, IConfigSource source, ISimulationBase openSimBase)
        {
        }

        public void FinishStartup(IScene scene, IConfigSource source, ISimulationBase openSimBase)
        {
        }

        public void PostFinishStartup(IScene scene, IConfigSource source, ISimulationBase openSimBase)
        {
            m_manager = scene.RequestModuleInterface<ISceneManager>();
            m_backup[scene].FinishStartup();
        }

        public void StartupComplete()
        {
            EnableBackup (null);
        }

        public void Close(IScene scene)
        {
            m_backup.Remove(scene);
        }

        public void DeleteRegion(IScene scene)
        {
        }

        #endregion

        #region Console commands

        /// <summary>
        /// Runs commands issued by the server console from the operator
        /// </summary>
        /// <param name="cmdparams">Additional arguments passed to the command</param>
        public void RunCommand (string[] cmdparams)
        {
#if (!ISWIN)
            m_manager.ForEachCurrentScene(delegate(IScene scene)
            {
                scene.AuroraEventManager.FireGenericEventHandler("Backup", null);
            });
#else
            m_manager.ForEachCurrentScene (scene => scene.AuroraEventManager.FireGenericEventHandler("Backup", null));
#endif
        }

        public void DisableBackup (string[] cmdparams)
        {
            m_manager.ForEachCurrentScene (delegate (IScene scene)
            {
                scene.SimulationDataService.SaveBackups = false;
            });
            MainConsole.Instance.Warn ("Disabled backup");
        }

        public void EnableBackup (string[] cmdparams)
        {
            m_manager.ForEachCurrentScene (delegate (IScene scene)
            {
                scene.SimulationDataService.SaveBackups = true;
            });
            if(cmdparams != null)//so that it doesn't show on startup
                MainConsole.Instance.Warn ("Enabled backup");
        }

        #endregion

        #region Per region backup class

        protected class InternalSceneBackup : IBackupModule, IAuroraBackupModule
        {
            #region Declares

            protected IScene m_scene;
            protected bool m_LoadingPrims;

            #endregion

            #region Constructor

            public InternalSceneBackup (IScene scene)
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

                List<ISceneEntity> deletes = new List<ISceneEntity> ();

                UUID match;

                switch (mode)
                {
                    case "owner":
                        if (!UUID.TryParse (o, out match))
                            return;
                        m_scene.ForEachSceneEntity (delegate (ISceneEntity g)
                                {
                                    if (g.OwnerID == match && !g.IsAttachment)
                                        deletes.Add (g);
                                });
                        break;
                    case "creator":
                        if (!UUID.TryParse (o, out match))
                            return;
                        m_scene.ForEachSceneEntity (delegate (ISceneEntity g)
                                {
                                    if (g.RootChild.CreatorID == match && !g.IsAttachment)
                                        deletes.Add (g);
                                });
                        break;
                    case "uuid":
                        if (!UUID.TryParse (o, out match))
                            return;
                        m_scene.ForEachSceneEntity (delegate (ISceneEntity g)
                                {
                                    if (g.UUID == match && !g.IsAttachment)
                                        deletes.Add (g);
                                });
                        break;
                    case "name":
                        m_scene.ForEachSceneEntity (delegate (ISceneEntity g)
                                {
                                    if (g.RootChild.Name == o && !g.IsAttachment)
                                        deletes.Add (g);
                                });
                        break;
                }

                MainConsole.Instance.Warn("Deleting " + deletes.Count + " objects.");
                DeleteSceneObjects(deletes.ToArray(), true, true);
            }

            #endregion

            #region Scene events

            /// <summary>
            /// Loads the World's objects
            /// </summary>
            public void LoadPrimsFromStorage()
            {
                LoadingPrims = true;

                MainConsole.Instance.Info("[BackupModule]: Loading objects for " + m_scene.RegionInfo.RegionName + " from " + m_scene.SimulationDataService.Name);
                List<ISceneEntity> PrimsFromDB = m_scene.SimulationDataService.LoadObjects(m_scene);
                foreach (ISceneEntity group in PrimsFromDB)
                {
                    try
                    {
                        if (group == null)
                        {
                            MainConsole.Instance.Warn("[BackupModule]: Null object while loading objects, ignoring.");
                            continue;
                        }
                        if (group.RootChild.Shape == null)
                        {
                            MainConsole.Instance.Warn("[BackupModule]: Broken object (" + group.Name + ") found while loading objects, removing it from the database.");
                            //WTF went wrong here? Remove by passing it by on loading
                            continue;
                        }
                        if (group.IsAttachment || (group.RootChild.Shape.State != 0 &&
                            (group.RootChild.Shape.PCode == (byte)PCode.None ||
                            group.RootChild.Shape.PCode == (byte)PCode.Prim ||
                            group.RootChild.Shape.PCode == (byte)PCode.Avatar)))
                        {
                            MainConsole.Instance.Warn("[BackupModule]: Broken state for object " + group.Name + " while loading objects, removing it from the database.");
                            //WTF went wrong here? Remove by passing it by on loading
                            continue;
                        }
                        if (group.AbsolutePosition.X > m_scene.RegionInfo.RegionSizeX + 10 ||
                            group.AbsolutePosition.X < -10 ||
                            group.AbsolutePosition.Y > m_scene.RegionInfo.RegionSizeY + 10 ||
                            group.AbsolutePosition.Y < -10)
                        {
                            MainConsole.Instance.Warn ("[BackupModule]: Object outside the region (" + group.Name + ", " + group.AbsolutePosition + ") found while loading objects, removing it from the database.");
                            //WTF went wrong here? Remove by passing it by on loading
                            continue;
                        }
                        m_scene.SceneGraph.CheckAllocationOfLocalIds (group);
                        group.Scene = m_scene;

                        if (group.RootChild == null)
                        {
                            MainConsole.Instance.ErrorFormat("[BackupModule] Found a SceneObjectGroup with m_rootPart == null and {0} children",
                                              group.ChildrenEntities().Count);
                            continue;
                        }
                        m_scene.SceneGraph.RestorePrimToScene(group, false);
                    }
                    catch(Exception ex)
                    {
                        MainConsole.Instance.WarnFormat("[BackupModule]: Exception attempting to load object from the database, {0}, continuing...", ex.ToString());
                    }
                }
                LoadingPrims = false;
                MainConsole.Instance.Info("[BackupModule]: Loaded " + PrimsFromDB.Count.ToString() + " object(s) in " + m_scene.RegionInfo.RegionName);
                PrimsFromDB.Clear ();
            }

            /// <summary>
            /// Loads all Parcel data from the datastore for region identified by regionID
            /// </summary>
            public void LoadAllLandObjectsFromStorage()
            {
                MainConsole.Instance.Debug ("[BackupModule]: Loading Land Objects from database... ");
                m_scene.EventManager.TriggerIncomingLandDataFromStorage(m_scene.SimulationDataService.LoadLandObjects(m_scene.RegionInfo.RegionID), Vector2.Zero);
            }

            public void FinishStartup()
            {
                //Load the prims from the database now that we are done loading
                LoadPrimsFromStorage();
                //Then load the land objects
                LoadAllLandObjectsFromStorage();
                //Load the prims from the database now that we are done loading
                CreateScriptInstances();
                //Now destroy the local caches as we're all loaded
                m_scene.SimulationDataService.Dispose();
            }

            /// <summary>
            /// Start all the scripts in the scene which should be started.
            /// </summary>
            public void CreateScriptInstances()
            {
                MainConsole.Instance.Info("[BackupModule]: Starting scripts in " + m_scene.RegionInfo.RegionName);
                //Set loading prims here to block backup
                LoadingPrims = true;
                ISceneEntity[] entities = m_scene.Entities.GetEntities();
                foreach(ISceneEntity group in entities)
                {
                    group.CreateScriptInstances(0, false, StateSource.RegionStart, UUID.Zero, false);
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
                    List<ISceneEntity> groups = new List<ISceneEntity>();
                    lock (m_scene.Entities)
                    {
                        ISceneEntity[] entities = m_scene.Entities.GetEntities();
#if (!ISWIN)
                        foreach (ISceneEntity entity in entities)
                        {
                            if (!entity.IsAttachment)
                                groups.Add(entity);
                        }
#else
                        groups.AddRange(entities.Where(entity => !entity.IsAttachment));
#endif
                    }
                    //Delete all the groups now
                    DeleteSceneObjects(groups.ToArray(), true, true);

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
                //Add the loading prims piece just to be safe
                LoadingPrims = true;

                try
                {
                    lock (m_scene.Entities)
                    {
                        ISceneEntity[] entities = m_scene.Entities.GetEntities();
                        foreach (ISceneEntity entity in entities)
                        {
                            if (!entity.IsAttachment)
                            {
                                List<ISceneChildEntity> parts = new List<ISceneChildEntity>();
                                parts.AddRange(entity.ChildrenEntities());
                                DeleteSceneObject(entity, true, false); //Don't remove from the database
#if (!ISWIN)
                                m_scene.ForEachScenePresence(delegate(IScenePresence avatar)
                                {
                                    avatar.ControllingClient.SendKillObject(m_scene.RegionInfo.RegionHandle, parts.ToArray());
                                });
#else
                                m_scene.ForEachScenePresence(
                                    avatar =>
                                    avatar.ControllingClient.SendKillObject(m_scene.RegionInfo.RegionHandle,
                                                                            parts.ToArray()));
#endif
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
            /// <param name="sendKillPackets"></param>
            /// <returns></returns>
            public bool DeleteSceneObjects (ISceneEntity[] groups, bool DeleteScripts, bool sendKillPackets)
            {
                List<ISceneChildEntity> parts = new List<ISceneChildEntity>();
                foreach (ISceneEntity grp in groups)
                {
                    if (grp == null)
                        continue;
                    //if (group.IsAttachment)
                    //    continue;
                    parts.AddRange(grp.ChildrenEntities());
                    DeleteSceneObject(grp, true, true);
                }
                if(sendKillPackets)
                {
#if (!ISWIN)
                    m_scene.ForEachScenePresence(delegate(IScenePresence avatar)
                    {
                        avatar.ControllingClient.SendKillObject(
                            m_scene.RegionInfo.RegionHandle, parts.ToArray());
                    });
#else
                    m_scene.ForEachScenePresence(avatar => avatar.ControllingClient.SendKillObject(
                        m_scene.RegionInfo.RegionHandle, parts.ToArray()));
#endif
                }

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
            protected bool DeleteSceneObject(ISceneEntity group, bool DeleteScripts, bool removeFromDatabase)
            {
                //MainConsole.Instance.DebugFormat("[Backup]: Deleting scene object {0} {1}", group.Name, group.UUID);

                if (group.SitTargetAvatar.Count != 0)
                {
                    foreach (UUID avID in group.SitTargetAvatar)
                    {
                        //Don't screw up avatar's that are sitting on us!
                        IScenePresence SP = m_scene.GetScenePresence(avID);
                        if (SP != null)
                            SP.StandUp();
                    }
                }

                // Serialise calls to RemoveScriptInstances to avoid
                // deadlocking on m_parts inside SceneObjectGroup
                if (DeleteScripts)
                {
                    group.RemoveScriptInstances(true);
                }

                foreach (ISceneChildEntity part in group.ChildrenEntities())
                {
                    IScriptControllerModule m = m_scene.RequestModuleInterface<IScriptControllerModule>();
                    if(m != null)
                        m.RemoveAllScriptControllers(part);
                }
                if (group.RootChild.PhysActor != null)
                {
                    //Remove us from the physics sim
                    m_scene.PhysicsScene.DeletePrim(group.RootChild.PhysActor);
                    //We MUST leave this to the PhysicsScene or it will hate us forever!
                    //group.RootChild.PhysActor = null;
                }

                if(!group.IsAttachment)
                    m_scene.SimulationDataService.Tainted ();
                if (m_scene.SceneGraph.DeleteEntity(group))
                {
                    // We need to keep track of this state in case this group is still queued for backup.
                    group.IsDeleted = true;
                    m_scene.EventManager.TriggerObjectBeingRemovedFromScene(group);
                    return true;
                }

                //MainConsole.Instance.DebugFormat("[SCENE]: Exit DeleteSceneObject() for {0} {1}", group.Name, group.UUID);
                return false;
            }

            #endregion

            #region IAuroraBackupModule Methods

            private bool m_isArchiving = false;
            private readonly List<UUID> m_missingAssets = new List<UUID>();
            private readonly List<LandData> m_parcels = new List<LandData>();
            private bool m_merge = false;
            private bool m_loadAssets = false;
            private GenericAccountCache<OpenSim.Services.Interfaces.UserAccount> m_cache = new GenericAccountCache<OpenSim.Services.Interfaces.UserAccount>();
            private List<SceneObjectGroup> m_groups = new List<SceneObjectGroup>();

            public bool IsArchiving
            {
                get { return m_isArchiving; }
            }

            public void SaveModuleToArchive(TarArchiveWriter writer, IScene scene)
            {
                m_isArchiving = true;

                MainConsole.Instance.Info("[Archive]: Writing parcels to archive");

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

                MainConsole.Instance.Info("[Archive]: Finished writing parcels to archive");
                MainConsole.Instance.Info ("[Archive]: Writing terrain to archive");

                writer.WriteDir ("newstyleterrain");
                writer.WriteDir ("newstylerevertterrain");

                writer.WriteDir ("newstylewater");
                writer.WriteDir ("newstylerevertwater");

                ITerrainModule tModule = scene.RequestModuleInterface<ITerrainModule> ();
                if (tModule != null)
                {
                    try
                    {
                        byte[] sdata = WriteTerrainToStream (tModule.TerrainMap);
                        writer.WriteFile ("newstyleterrain/" + scene.RegionInfo.RegionID.ToString () + ".terrain", sdata);
                        sdata = null;

                        sdata = WriteTerrainToStream (tModule.TerrainRevertMap);
                        writer.WriteFile ("newstylerevertterrain/" + scene.RegionInfo.RegionID.ToString () + ".terrain", sdata);
                        sdata = null;

                        if (tModule.TerrainWaterMap != null)
                        {
                            sdata = WriteTerrainToStream (tModule.TerrainWaterMap);
                            writer.WriteFile ("newstylewater/" + scene.RegionInfo.RegionID.ToString () + ".terrain", sdata);
                            sdata = null;
                            
                            sdata = WriteTerrainToStream (tModule.TerrainWaterRevertMap);
                            writer.WriteFile ("newstylerevertwater/" + scene.RegionInfo.RegionID.ToString () + ".terrain", sdata);
                            sdata = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        MainConsole.Instance.WarnFormat ("[Backup]: Exception caught: {0}", ex.ToString ());
                    }
                }
                
                MainConsole.Instance.Info("[Archive]: Finished writing terrain to archive");
                MainConsole.Instance.Info("[Archive]: Writing entities to archive");
                ISceneEntity[] entities = scene.Entities.GetEntities();
                //Get all entities, then start writing them to the database
                writer.WriteDir("entities");

                IDictionary<UUID, AssetType> assets = new Dictionary<UUID, AssetType>();
                UuidGatherer assetGatherer = new UuidGatherer(m_scene.AssetService);
                IAuroraBackupArchiver archiver = m_scene.RequestModuleInterface<IAuroraBackupArchiver> ();
                bool saveAssets = false;
                if(archiver.AllowPrompting)
                    saveAssets = MainConsole.Instance.Prompt ("Save assets? (Will not be able to load on other grids)", "false").Equals ("true", StringComparison.CurrentCultureIgnoreCase);

                int count = 0;
                foreach (ISceneEntity entity in entities)
                {
                    try
                    {
                        if (entity.IsAttachment || ((entity.RootChild.Flags & PrimFlags.Temporary) == PrimFlags.Temporary)
                             || ((entity.RootChild.Flags & PrimFlags.TemporaryOnRez) == PrimFlags.TemporaryOnRez))
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
                        MainConsole.Instance.WarnFormat ("[Backup]: Exception caught: {0}", ex);
                    }
                }
                entities = null;

                MainConsole.Instance.Info("[Archive]: Finished writing entities to archive");
                MainConsole.Instance.Info("[Archive]: Writing assets for entities to archive");

                bool foundAllAssets = true;
                foreach (UUID assetID in new List<UUID> (assets.Keys))
                {
                    try
                    {
                        foundAllAssets = false; //Not all are cached
                        m_scene.AssetService.Get (assetID.ToString (), writer, RetrievedAsset);
                        m_missingAssets.Add(assetID);
                    }
                    catch (Exception ex)
                    {
                        MainConsole.Instance.WarnFormat ("[Backup]: Exception caught: {0}", ex);
                    }
                }
                if (foundAllAssets)
                    m_isArchiving = false; //We're done if all the assets were found

                MainConsole.Instance.Info("[Archive]: Finished writing assets for entities to archive");
            }

            private static byte[] WriteTerrainToStream (ITerrainChannel tModule)
            {
                int tMapSize = tModule.Height * tModule.Height;
                byte[] sdata = new byte[tMapSize * 2];
                Buffer.BlockCopy (tModule.GetSerialised (tModule.Scene), 0, sdata, 0, sdata.Length);
                return sdata;
            }

            private void RetrievedAsset(string id, Object sender, AssetBase asset)
            {
                TarArchiveWriter writer = (TarArchiveWriter)sender;
                //Add the asset
                WriteAsset(id, asset, writer);
                m_missingAssets.Remove(UUID.Parse(id));
                if (m_missingAssets.Count == 0)
                    m_isArchiving = false;
            }

            private void WriteAsset(string id, AssetBase asset, TarArchiveWriter writer)
            {
                if (asset != null)
                    writer.WriteFile ("assets/" + asset.ID, OSDParser.SerializeJsonString(asset.ToOSD()));
                else
                    MainConsole.Instance.WarnFormat ("Could not find asset {0}", id);
            }

            public void BeginLoadModuleFromArchive(IScene scene)
            {
                IBackupModule backup = scene.RequestModuleInterface<IBackupModule>();
                IScriptModule[] modules = scene.RequestModuleInterfaces<IScriptModule>();
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
                    m_loadAssets = MainConsole.Instance.Prompt("Should any stored assets be loaded? (If you got this .abackup from another grid, choose yes", "no").ToLower() == "yes";
                    m_merge = MainConsole.Instance.Prompt("Should we merge prims together (keep the prims from the old region too)?", "no").ToLower() == "yes";
                    if (!m_merge)
                    {
                        DateTime before = DateTime.Now;
                        MainConsole.Instance.Info("[ARCHIVER]: Clearing all existing scene objects");
                        backup.DeleteAllSceneObjects();
                        MainConsole.Instance.Info("[ARCHIVER]: Cleared all existing scene objects in " + (DateTime.Now - before).Minutes + ":" + (DateTime.Now - before).Seconds);
                        if (parcelModule != null)
                            parcelModule.ClearAllParcels ();
                    }
                }
            }

            public void EndLoadModuleFromArchive(IScene scene)
            {
                IBackupModule backup = scene.RequestModuleInterface<IBackupModule>();
                IScriptModule[] modules = scene.RequestModuleInterfaces<IScriptModule>();
                //Reeanble now that we are done
                foreach (IScriptModule module in modules)
                {
                    module.Disabled = false;
                }
                //Reset backup too
                if (backup != null)
                    backup.LoadingPrims = false;

                //Update the database as well!
                IParcelManagementModule parcelManagementModule = scene.RequestModuleInterface<IParcelManagementModule>();
                if (parcelManagementModule != null && !m_merge) //Only if we are not merging
                {
                    if (m_parcels.Count > 0)
                    {
                        scene.EventManager.TriggerIncomingLandDataFromStorage(m_parcels, Vector2.Zero);
                        //Update the database as well!
                        foreach (LandData parcel in m_parcels)
                        {
                            parcelManagementModule.UpdateLandObject(parcelManagementModule.GetLandObject(parcel.LocalID));
                        }
                    }
                    else parcelManagementModule.ResetSimLandObjects ();
                    m_parcels.Clear();
                }

                foreach (SceneObjectGroup sceneObject in m_groups)
                {
                    foreach (ISceneChildEntity part in sceneObject.ChildrenEntities())
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
                                    kvp.Value.OwnerID = scene.RegionInfo.EstateSettings.EstateOwner;
                                }
                                if (!ResolveUserUuid(kvp.Value.CreatorID))
                                {
                                    kvp.Value.CreatorID = scene.RegionInfo.EstateSettings.EstateOwner;
                                }
                            }
                        }
                    }

                    if (scene.SceneGraph.AddPrimToScene(sceneObject))
                    {
                        sceneObject.HasGroupChanged = true;
                        sceneObject.ScheduleGroupUpdate(PrimUpdateFlags.ForcedFullUpdate);
                        sceneObject.CreateScriptInstances(0, false, StateSource.RegionStart, UUID.Zero, false);
                    }
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
                #region New Style Terrain Loading
                else if (filePath.StartsWith ("newstyleterrain/"))
                {
                    ITerrainModule terrainModule = scene.RequestModuleInterface<ITerrainModule>();
                    terrainModule.TerrainMap = ReadTerrain(data, scene);
                }
                else if (filePath.StartsWith ("newstylerevertterrain/"))
                {
                    ITerrainModule terrainModule = scene.RequestModuleInterface<ITerrainModule>();
                    terrainModule.TerrainRevertMap = ReadTerrain(data, scene);
                }
                else if (filePath.StartsWith ("newstylewater/"))
                {
                    ITerrainModule terrainModule = scene.RequestModuleInterface<ITerrainModule>();
                    terrainModule.TerrainWaterMap = ReadTerrain(data, scene);
                }
                else if (filePath.StartsWith ("newstylerevertwater/"))
                {
                    ITerrainModule terrainModule = scene.RequestModuleInterface<ITerrainModule>();
                    terrainModule.TerrainWaterRevertMap = ReadTerrain(data, scene);
                }
                #endregion
                #region Old Style Terrain Loading
                else if (filePath.StartsWith ("terrain/"))
                {
                    ITerrainModule terrainModule = scene.RequestModuleInterface<ITerrainModule>();

                    MemoryStream ms = new MemoryStream (data);
                    terrainModule.LoadFromStream (filePath, ms, 0, 0);
                    ms.Close ();
                }
                else if (filePath.StartsWith ("revertterrain/"))
                {
                    ITerrainModule terrainModule = scene.RequestModuleInterface<ITerrainModule>();

                    MemoryStream ms = new MemoryStream (data);
                    terrainModule.LoadRevertMapFromStream (filePath, ms, 0, 0);
                    ms.Close ();
                }
                else if (filePath.StartsWith ("water/"))
                {
                    ITerrainModule terrainModule = scene.RequestModuleInterface<ITerrainModule>();

                    MemoryStream ms = new MemoryStream (data);
                    terrainModule.LoadWaterFromStream (filePath, ms, 0, 0);
                    ms.Close ();
                }
                else if (filePath.StartsWith ("revertwater/"))
                {
                    ITerrainModule terrainModule = scene.RequestModuleInterface<ITerrainModule>();

                    MemoryStream ms = new MemoryStream (data);
                    terrainModule.LoadWaterRevertMapFromStream (filePath, ms, 0, 0);
                    ms.Close ();
                }
                #endregion
                else if (filePath.StartsWith ("entities/"))
                {
                    MemoryStream ms = new MemoryStream (data);
                    SceneObjectGroup sceneObject = OpenSim.Region.Framework.Scenes.Serialization.SceneObjectSerializer.FromXml2Format (ref ms, scene);
                    ms.Close ();
                    ms = null;
                    data = null;
                    m_groups.Add(sceneObject);
                }
                else if(filePath.StartsWith("assets/"))
                {
                    if(m_loadAssets)
                    {
                        AssetBase asset = new AssetBase();
                        asset.Unpack(OSDParser.DeserializeJson(Encoding.UTF8.GetString(data)));
                        scene.AssetService.Store(asset);
                    }
                }
            }

            private ITerrainChannel ReadTerrain (byte[] data, IScene scene)
            {
                short[] sdata = new short[data.Length / 2];
                Buffer.BlockCopy (data, 0, sdata, 0, data.Length);
                return new TerrainChannel(sdata, scene);
            }

            private bool ResolveUserUuid (UUID uuid)
            {
                OpenSim.Services.Interfaces.UserAccount acc;
                if (m_cache.Get(uuid, out acc))
                    return acc != null;
                acc = m_scene.UserAccountService.GetUserAccount(m_scene.RegionInfo.AllScopeIDs, uuid);
                m_cache.Cache(uuid, acc);
                return acc != null;
            }

            #endregion
        }

        #endregion
    }
}
