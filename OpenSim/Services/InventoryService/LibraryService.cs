/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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
using System.IO.Compression;
using System.Reflection;
using System.Xml;

using OpenSim.Framework;
using OpenSim.Services.Interfaces;

using log4net;
using Nini.Config;
using OpenMetaverse;
using Aurora.Simulation.Base;

namespace OpenSim.Services.InventoryService
{
    /// <summary>
    /// Basically a hack to give us a Inventory library while we don't have a inventory server
    /// once the server is fully implemented then should read the data from that
    /// </summary>
    public class LibraryService : ILibraryService, IService
    {
        private InventoryFolderImpl m_LibraryRootFolder;

        private UUID libOwner = new UUID("11111111-1111-0000-0000-000100bba000");

        private string libOwnerName = "Library Owner";
        private IRegistryCore m_registry;
        private string pLibName = "Aurora Library";
        private bool m_enabled = false;

        public InventoryFolderImpl LibraryRootFolder
        {
            get { return m_LibraryRootFolder; }
        }

        public UUID LibraryOwner
        {
            get { return libOwner; }
        }

        public string LibraryOwnerName
        {
            get { return libOwnerName; }
        }

        public string LibraryName
        {
            get { return pLibName; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            string pLibOwnerName = "Library Owner";

            IConfig libConfig = config.Configs["LibraryService"];
            if (libConfig != null)
            {
                m_enabled = true;
                pLibName = libConfig.GetString("LibraryName", pLibName);
                libOwnerName = libConfig.GetString("LibraryOwnerName", pLibOwnerName);
            }

            //m_log.Debug("[LIBRARY]: Starting library service...");

            m_LibraryRootFolder = new InventoryFolderImpl();
            m_LibraryRootFolder.Owner = libOwner;
            m_LibraryRootFolder.ID = new UUID("00000112-000f-0000-0000-000100bba000");
            m_LibraryRootFolder.Name = pLibName;
            m_LibraryRootFolder.ParentID = UUID.Zero;
            m_LibraryRootFolder.Type = (short)8;
            m_LibraryRootFolder.Version = (ushort)1;

            registry.RegisterModuleInterface<ILibraryService>(this);
            m_registry = registry;
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            if (m_enabled)
            {
                MainConsole.Instance.Commands.AddCommand ("clear default inventory", "clear default inventory",
                    "Clears the Default Inventory stored for this grid", ClearDefaultInventory);
            }
        }

        public void FinishedStartup()
        {
            LoadLibraries(m_registry);
        }

        public void AddToDefaultInventory(InventoryFolderImpl folder)
        {
            foreach (InventoryFolderImpl f in folder.RequestListOfFolderImpls())
                m_LibraryRootFolder.AddChildFolder(f);
            foreach (InventoryItemBase i in folder.RequestListOfItems())
                m_LibraryRootFolder.Items.Add(i.ID, i);
        }

        public void LoadLibraries(IRegistryCore registry)
        {
            LoadPreviouslyLoadedArchives(registry);
            if (!m_enabled)
                return;
            List<IDefaultLibraryLoader> Loaders = Aurora.Framework.AuroraModuleLoader.PickupModules<IDefaultLibraryLoader>();
            try
            {
                IniConfigSource iniSource = new IniConfigSource("DefaultInventory/Inventory.ini", Nini.Ini.IniFileType.AuroraStyle);
                if (iniSource != null)
                {
                    foreach (IDefaultLibraryLoader loader in Loaders)
                    {
                        loader.LoadLibrary(this, iniSource, registry);
                    }
                }
            }
            catch
            {
            }
        }

        private void LoadPreviouslyLoadedArchives(IRegistryCore registry)
        {
            IUserAccountService UserAccountService = registry.RequestModuleInterface<IUserAccountService>();
            UserAccount uinfo = UserAccountService.GetUserAccount(UUID.Zero, LibraryOwner);
            IInventoryService InventoryService = registry.RequestModuleInterface<IInventoryService>();
            //Make the user account for the default IAR
            if (uinfo == null)
            {
                uinfo = new UserAccount(LibraryOwner);
                uinfo.Name = LibraryOwnerName;
                uinfo.Email = "";
                uinfo.ServiceURLs = new Dictionary<string, object>();
                uinfo.UserLevel = 0;
                uinfo.UserFlags = 0;
                uinfo.ScopeID = UUID.Zero;
                uinfo.UserTitle = "";
                UserAccountService.StoreUserAccount(uinfo);
                InventoryService.CreateUserInventory(uinfo.PrincipalID);
                uinfo = UserAccountService.GetUserAccount(UUID.Zero, LibraryOwner);
                if (uinfo == null)
                {
                    //Grid mode, can't create the user... leave
                    return;
                }
            }
            InventoryCollection col = InventoryService.GetFolderContent(LibraryOwner, UUID.Zero);
            foreach (InventoryFolderBase folder in col.Folders)
            {
                if (folder.Name == "My Inventory") continue; //Pass My Inventory by
                InventoryFolderImpl f = new InventoryFolderImpl(folder);

                TraverseFolders(f, folder.ID, InventoryService);
                //This is our loaded folder
                AddToDefaultInventory(f);
            }
        }

        private void TraverseFolders(InventoryFolderImpl folderimp, UUID ID, IInventoryService InventoryService)
        {
            InventoryCollection col = InventoryService.GetFolderContent(LibraryOwner, ID);
            foreach (InventoryItemBase item in col.Items)
            {
                folderimp.Items.Add(item.ID, item);
            }
            foreach (InventoryFolderBase folder in col.Folders)
            {
                InventoryFolderImpl childFolder = new InventoryFolderImpl(folder);
                TraverseFolders(childFolder, folder.ID, InventoryService);
                folderimp.AddChildFolder(childFolder);
            }
        }

        /// <summary>
        /// Looks like a simple getter, but is written like this for some consistency with the other Request
        /// methods in the superclass
        /// </summary>
        /// <returns></returns>
        public Dictionary<UUID, InventoryFolderImpl> GetAllFolders()
        {
            Dictionary<UUID, InventoryFolderImpl> fs = new Dictionary<UUID, InventoryFolderImpl>();
            fs.Add(LibraryRootFolder.ID, LibraryRootFolder);
            List<InventoryFolderImpl> fis = TraverseFolder(LibraryRootFolder);
            foreach (InventoryFolderImpl f in fis)
            {
                fs.Add(f.ID, f);
            }
            //return libraryFolders;
            return fs;
        }

        private List<InventoryFolderImpl> TraverseFolder(InventoryFolderImpl node)
        {
            List<InventoryFolderImpl> folders = node.RequestListOfFolderImpls();
            List<InventoryFolderImpl> subs = new List<InventoryFolderImpl>();
            foreach (InventoryFolderImpl f in folders)
                subs.AddRange(TraverseFolder(f));

            folders.AddRange(subs);
            return folders;
        }

        private void ClearDefaultInventory (string module, string[] cmd)
        {
            string sure = MainConsole.Instance.CmdPrompt ("Are you sure you want to delete the default inventory?", "yes");
            if (!sure.Equals ("yes", StringComparison.CurrentCultureIgnoreCase))
                return;
            IUserAccountService UserAccountService = m_registry.RequestModuleInterface<IUserAccountService>();
            UserAccount uinfo = UserAccountService.GetUserAccount(UUID.Zero, LibraryOwner);
            IInventoryService InventoryService = m_registry.RequestModuleInterface<IInventoryService> ();
            if (uinfo != null)
            {
                //Delete the root folders
                InventoryFolderBase root = InventoryService.GetRootFolder (uinfo.PrincipalID);
                while (root != null)
                {
                    InventoryService.ForcePurgeFolder (root);
                    root = InventoryService.GetRootFolder (uinfo.PrincipalID);
                }
            }
        }
    }
}
