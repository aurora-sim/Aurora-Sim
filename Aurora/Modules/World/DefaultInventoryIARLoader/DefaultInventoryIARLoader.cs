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

using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.CoreModules.Avatar.Inventory.Archiver;

namespace Aurora.Modules.World.DefaultInventoryIARLoader
{
    public class DefaultInventoryIARLoader : IDefaultLibraryLoader
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected ILibraryService m_service;
        protected IRegistryCore m_registry;

        public void LoadLibrary(ILibraryService service, IConfigSource source, IRegistryCore registry)
        {
            m_service = service;
            m_registry = registry;

            IConfig libConfig = source.Configs["InventoryIARLoader"];
            string pLibrariesLocation = "DefaultInventory/";
            if (libConfig != null)
            {
                if (libConfig.GetBoolean("Loaded", false))
                    return; //If it is loaded, don't reload
                foreach (string iarFileName in Directory.GetFiles(pLibrariesLocation, "*.iar"))
                {
                    LoadLibraries(iarFileName);
                }
            }
        }

        /// <summary>
        /// Use the asset set information at path to load assets
        /// </summary>
        /// <param name="path"></param>
        /// <param name="assets"></param>
        protected void LoadLibraries(string iarFileName)
        {
            m_log.InfoFormat("[LIBRARY INVENTORY]: Loading iar file {0}", iarFileName);
            string simpleName = Path.GetFileNameWithoutExtension(iarFileName);

            m_log.InfoFormat("[LIBRARY MODULE]: Loading library archive {0} ({1})...", iarFileName, simpleName);
            simpleName = GetInventoryPathFromName(simpleName);

            RegionInfo regInfo = new RegionInfo();
            Scene m_MockScene = null;
            //Make the scene for the IAR loader
            if (m_registry is Scene)
                m_MockScene = (Scene)m_registry;
            else
            {
                new Scene(regInfo);
                m_MockScene.AddModuleInterfaces(m_registry.GetInterfaces());
            }

            UserAccount uinfo = m_MockScene.UserAccountService.GetUserAccount(UUID.Zero, m_service.LibraryOwner);
            //Make the user account for the default IAR
            if (uinfo == null)
            {
                uinfo = new UserAccount(m_service.LibraryOwner);
                uinfo.FirstName = m_service.LibraryOwnerName[0];
                uinfo.LastName = m_service.LibraryOwnerName[1];
                uinfo.ServiceURLs = new Dictionary<string, object>();
                uinfo.Created = Util.EnvironmentTickCount();
                uinfo.UserLevel = 0;
                uinfo.UserFlags = 0;
                uinfo.UserTitle = "";
                m_MockScene.UserAccountService.StoreUserAccount(uinfo);

                m_MockScene.InventoryService.CreateUserInventory(m_service.LibraryOwner);
            }
            
            InventoryArchiveReadRequest archread = new InventoryArchiveReadRequest(m_MockScene, uinfo, simpleName, iarFileName, false);

            try
            {
                HashSet<InventoryNodeBase> nodes = archread.Execute(true);
                if (nodes != null && nodes.Count == 0)
                {
                    // didn't find the subfolder with the given name; place it on the top
                    m_log.InfoFormat("[LIBRARY MODULE]: Didn't find {0} in library. Placing archive on the top level", simpleName);
                    archread.Close();
                    archread = new InventoryArchiveReadRequest(m_MockScene, uinfo, "/", iarFileName, false);
                    nodes = archread.Execute(true);
                }
                foreach (InventoryNodeBase node in nodes)
                    FixPerms(node);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[LIBRARY MODULE]: Exception when processing archive {0}: {1}", iarFileName, e.StackTrace);
            }
            finally
            {
                archread.Close();
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
            string[] parts = name.Split(new char[] { ' ' });
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
