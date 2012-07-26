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
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using Aurora.Modules.Archivers;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules.DefaultInventoryIARLoader
{
    public class DefaultInventoryIARLoader : IDefaultLibraryLoader
    {
        protected Dictionary<string, AssetType> m_assetTypes = new Dictionary<string, AssetType>();
        protected IRegistryCore m_registry;
        protected ILibraryService m_service;
        protected IInventoryData m_Database;

        #region IDefaultLibraryLoader Members

        public void LoadLibrary(ILibraryService service, IConfigSource source, IRegistryCore registry)
        {
            m_service = service;
            m_registry = registry;
            m_Database = Aurora.DataManager.DataManager.RequestPlugin<IInventoryData>();

            IConfig libConfig = source.Configs["InventoryIARLoader"];
            const string pLibrariesLocation = "DefaultInventory/";
            AddDefaultAssetTypes();
            if (libConfig != null)
            {
                if (libConfig.GetBoolean("WipeLibrariesOnNextLoad", false))
                {
                    service.ClearDefaultInventory();//Nuke it
                    libConfig.Set("WipeLibrariesOnNextLoad", false);
                    source.Save();
                }
                if (libConfig.GetBoolean("PreviouslyLoaded", false))
                    return; //If it is loaded, don't reload
                foreach (string iarFileName in Directory.GetFiles(pLibrariesLocation, "*.iar"))
                {
                    LoadLibraries(iarFileName);
                }
            }
        }

        #endregion

        private void AddDefaultAssetTypes()
        {
            m_assetTypes.Add("Animation", AssetType.Animation);
            m_assetTypes.Add("Bodypart", AssetType.Bodypart);
            m_assetTypes.Add("Body part", AssetType.Bodypart);
            m_assetTypes.Add("CallingCard", AssetType.CallingCard);
            m_assetTypes.Add("Calling Card", AssetType.CallingCard);
            m_assetTypes.Add("Clothing", AssetType.Clothing);
            m_assetTypes.Add("CurrentOutfit", AssetType.CurrentOutfitFolder);
            m_assetTypes.Add("Current Outfit", AssetType.CurrentOutfitFolder);
            m_assetTypes.Add("Gesture", AssetType.Gesture);
            m_assetTypes.Add("Landmark", AssetType.Landmark);
            m_assetTypes.Add("Script", AssetType.LSLText);
            m_assetTypes.Add("Scripts", AssetType.LSLText);
            m_assetTypes.Add("Mesh", AssetType.Mesh);
            m_assetTypes.Add("Notecard", AssetType.Notecard);
            m_assetTypes.Add("Object", AssetType.Object);
            m_assetTypes.Add("Photo", AssetType.SnapshotFolder);
            m_assetTypes.Add("Snapshot", AssetType.SnapshotFolder);
            m_assetTypes.Add("Sound", AssetType.Sound);
            m_assetTypes.Add("Texture", AssetType.Texture);
            m_assetTypes.Add("Images", AssetType.Texture);
        }

        /// <summary>
        ///   Use the asset set information at path to load assets
        /// </summary>
        /// <param name = "path"></param>
        /// <param name = "assets"></param>
        protected void LoadLibraries(string iarFileName)
        {
            RegionInfo regInfo = new RegionInfo();
            IScene m_MockScene = null;
            //Make the scene for the IAR loader
            if (m_registry is IScene)
                m_MockScene = (IScene)m_registry;
            else
            {
                m_MockScene = new Scene();
                m_MockScene.Initialize(regInfo);
                m_MockScene.AddModuleInterfaces(m_registry.GetInterfaces());
            }

            UserAccount uinfo = m_MockScene.UserAccountService.GetUserAccount(null, m_service.LibraryOwner);
            //Make the user account for the default IAR
            if (uinfo == null)
            {
                MainConsole.Instance.Warn("Creating user " + m_service.LibraryOwnerName);
                m_MockScene.UserAccountService.CreateUser(m_service.LibraryOwner, UUID.Zero, m_service.LibraryOwnerName, "", "");
                uinfo = m_MockScene.UserAccountService.GetUserAccount(null, m_service.LibraryOwner);
                m_MockScene.InventoryService.CreateUserInventory(uinfo.PrincipalID, false);
            }
            if (m_MockScene.InventoryService.GetRootFolder(m_service.LibraryOwner) == null)
                m_MockScene.InventoryService.CreateUserInventory(uinfo.PrincipalID, false);

            List<InventoryFolderBase> rootFolders = m_MockScene.InventoryService.GetFolderFolders(uinfo.PrincipalID,
                                                                                                  UUID.Zero);
#if (!ISWIN)
            bool alreadyExists = false;
            foreach (InventoryFolderBase folder in rootFolders)
            {
                if (folder.Name == iarFileName)
                {
                    alreadyExists = true;
                    break;
                }
            }
#else
            bool alreadyExists = rootFolders.Any(folder => folder.Name == iarFileName);
#endif
            if (alreadyExists)
            {
                MainConsole.Instance.InfoFormat("[LIBRARY INVENTORY]: Found previously loaded iar file {0}, ignoring.", iarFileName);
                return;
            }

            MainConsole.Instance.InfoFormat("[LIBRARY INVENTORY]: Loading iar file {0}", iarFileName);
            InventoryFolderBase rootFolder = m_MockScene.InventoryService.GetRootFolder(uinfo.PrincipalID);

            if (null == rootFolder)
            {
                //We need to create the root folder, otherwise the IAR freaks
                m_MockScene.InventoryService.CreateUserInventory(uinfo.PrincipalID, false);
            }

            InventoryArchiveReadRequest archread = new InventoryArchiveReadRequest(m_MockScene, uinfo, "/", iarFileName,
                                                                                   false, m_service.LibraryOwner);

            try
            {
                archread.ReplaceAssets = true;//Replace any old assets
                List<InventoryNodeBase> nodes = new List<InventoryNodeBase>(archread.Execute(true));
                if (nodes.Count == 0)
                    return;
                InventoryFolderBase f = (InventoryFolderBase)nodes[0];
                UUID IARRootID = f.ID;

                TraverseFolders(IARRootID, m_MockScene);
                FixParent(IARRootID, m_MockScene, m_service.LibraryRootFolderID);
                f.Name = iarFileName;
                f.ParentID = UUID.Zero;
                f.ID = m_service.LibraryRootFolderID;
                f.Type = (int)AssetType.RootFolder;
                f.Version = 1;
                m_MockScene.InventoryService.UpdateFolder(f);
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[LIBRARY MODULE]: Exception when processing archive {0}: {1}", iarFileName,
                                  e.StackTrace);
            }
            finally
            {
                archread.Close();
            }
        }

        private void TraverseFolders(UUID ID, IScene m_MockScene)
        {
            List<InventoryFolderBase> folders = m_MockScene.InventoryService.GetFolderFolders(m_service.LibraryOwner, ID);
            foreach (InventoryFolderBase folder in folders)
            {
                InventoryFolderBase folder1 = folder;
#if (!ISWIN)
                foreach (KeyValuePair<string, AssetType> type in m_assetTypes)
                {
                    if (folder1.Name.ToLower().StartsWith(type.Key.ToLower()))
                    {
                        if (folder.Type == (short)type.Value) break;
                        folder.Type = (short)type.Value;
                        m_MockScene.InventoryService.UpdateFolder(folder);
                        break;
                    }
                }
#else
                foreach (KeyValuePair<string, AssetType> type in m_assetTypes.Where(type => folder1.Name.ToLower().StartsWith(type.Key.ToLower())).TakeWhile(type => folder.Type != (short) type.Value))
                {
                    folder.Type = (short) type.Value;
                    m_MockScene.InventoryService.UpdateFolder(folder);
                    break;
                }
#endif
                if (folder.Type == -1)
                {
                    folder.Type = (int)AssetType.Folder;
                    m_MockScene.InventoryService.UpdateFolder(folder);
                }
                TraverseFolders(folder.ID, m_MockScene);
            }
        }

        private void FixParent(UUID ID, IScene m_MockScene, UUID LibraryRootID)
        {
            List<InventoryFolderBase> folders = m_MockScene.InventoryService.GetFolderFolders(m_service.LibraryOwner, ID);
            foreach (InventoryFolderBase folder in folders)
            {
                if (folder.ParentID == ID)
                {
                    folder.ParentID = LibraryRootID;
                    m_Database.StoreFolder(folder);
                }
            }
        }

        private void FixPerms(InventoryNodeBase node)
        {
            if (node is InventoryItemBase)
            {
                InventoryItemBase item = (InventoryItemBase)node;
                item.BasePermissions = 0x7FFFFFFF;
                item.EveryOnePermissions = 0x7FFFFFFF;
                item.CurrentPermissions = 0x7FFFFFFF;
                item.NextPermissions = 0x7FFFFFFF;
            }
        }

        private string GetInventoryPathFromName(string name)
        {
            string[] parts = name.Split(new[] { ' ' });
            if (parts.Length == 3)
            {
                name = string.Empty;
                // cut the last part
                for (int i = 0; i < parts.Length - 1; i++)
                    name = name + ' ' + parts[i];
            }

            return name;
        }
    }
}