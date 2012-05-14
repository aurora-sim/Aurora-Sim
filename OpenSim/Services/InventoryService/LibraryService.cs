/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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
using System.IO;
using System.Collections.Generic;

using log4net.Core;

using Nini.Ini;
using Nini.Config;

using OpenMetaverse;
using OpenSim.Services.Interfaces;

using Aurora.Framework;
using Aurora.Simulation.Base;

namespace OpenSim.Services.InventoryService
{
    /// <summary>
    ///   Basically a hack to give us a Inventory library while we don't have a inventory server
    ///   once the server is fully implemented then should read the data from that
    /// </summary>
    public class LibraryService : ILibraryService, IService
    {
        private readonly UUID libOwner = new UUID("11111111-1111-0000-0000-000100bba000");

        public UUID LibraryRootFolderID { get { return new UUID("00000112-000f-0000-0000-000100bba000"); } }

        private string libOwnerName = "Library Owner";
        private bool m_enabled;
        private IRegistryCore m_registry;
        private string pLibName = "Aurora Library";

        #region ILibraryService Members

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

        #endregion

        #region IService Members

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

            //MainConsole.Instance.Debug("[LIBRARY]: Starting library service...");

            registry.RegisterModuleInterface<ILibraryService>(this);
            m_registry = registry;
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            if (m_enabled)
            {
                if (MainConsole.Instance != null)
                    MainConsole.Instance.Commands.AddCommand("clear default inventory", "clear default inventory",
                                                             "Clears the Default Inventory stored for this grid",
                                                             ClearDefaultInventory);
            }
        }

        public void FinishedStartup()
        {
            LoadLibraries(m_registry);
        }

        #endregion

        public void LoadLibraries(IRegistryCore registry)
        {
            if (!m_enabled)
            {
                return;
            }
            else if (!File.Exists("DefaultInventory/Inventory.ini") && !File.Exists("DefaultInventory/Inventory.ini.example"))
            {
                MainConsole.Instance.Error("Could not find DefaultInventory/Inventory.ini or DefaultInventory/Inventory.ini.example");
                return;
            }
            List<IDefaultLibraryLoader> Loaders = AuroraModuleLoader.PickupModules<IDefaultLibraryLoader>();
            try
            {
                if (!File.Exists("DefaultInventory/Inventory.ini"))
                {
                    File.Copy("DefaultInventory/Inventory.ini.example", "DefaultInventory/Inventory.ini");
                }
                IniConfigSource iniSource = new IniConfigSource("DefaultInventory/Inventory.ini", IniFileType.AuroraStyle);
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

        private void ClearDefaultInventory(string[] cmd)
        {
            string sure = MainConsole.Instance.Prompt("Are you sure you want to delete the default inventory?", "yes");
            if (!sure.Equals("yes", StringComparison.CurrentCultureIgnoreCase))
                return;
            ClearDefaultInventory();
        }

        public void ClearDefaultInventory()
        {
            IInventoryService InventoryService = m_registry.RequestModuleInterface<IInventoryService>();
            //Delete the root folders
            InventoryFolderBase root = InventoryService.GetRootFolder(LibraryOwner);
            while (root != null)
            {
                MainConsole.Instance.Info("Removing folder " + root.Name);
                InventoryService.ForcePurgeFolder(root);
                root = InventoryService.GetRootFolder(LibraryOwner);
            }
            List<InventoryFolderBase> rootFolders = InventoryService.GetRootFolders(LibraryOwner);
            foreach (InventoryFolderBase rFolder in rootFolders)
            {
                MainConsole.Instance.Info("Removing folder " + rFolder.Name);
                InventoryService.ForcePurgeFolder(rFolder);
            }
            MainConsole.Instance.Info("Finished removing default inventory");
        }
    }
}