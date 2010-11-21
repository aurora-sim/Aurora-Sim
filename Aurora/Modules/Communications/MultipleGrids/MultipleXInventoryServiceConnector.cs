using System;
using System.Collections.Generic;
using Nini.Config;
using log4net;
using System.Reflection;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using OpenSim.Region.CoreModules.ServiceConnectorsOut;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.Inventory;

namespace Aurora.Modules.Communications.MultipleGrids
{
    public class MultipleXInventoryServicesConnector : ISharedRegionModule, IInventoryService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private bool m_Enabled = false;
        private List<IInventoryService> AllServices = new List<IInventoryService>();

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public string Name
        {
            get { return "MultipleXInventoryServicesConnector"; }
        }

        public void Initialise(IConfigSource source)
        {
            IConfig moduleConfig = source.Configs["Modules"];
            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("InventoryServices", "");
                if (name == Name)
                {
                    IConfig multipleConfig = source.Configs["MultipleGridsModule"];
                    if (multipleConfig != null)
                    {
                        IConfig UAS = source.Configs["InventoryService"];
                        if (UAS != null)
                        {
                            string[] Grids = multipleConfig.GetString("InventoryServerURIs", "").Split(',');
                            //Set it so that it works for them
                            moduleConfig.Set("InventoryServices", "RemoteXInventoryServicesConnector");
                            foreach (string gridURL in Grids)
                            {
                                //Set their gridURL
                                UAS.Set("InventoryServerURI", gridURL);
                                //Start it up
                                RemoteXInventoryServicesConnector connector = new RemoteXInventoryServicesConnector();
                                connector.Initialise(source);
                                AllServices.Add(connector);
                                m_log.Info("[INVENTORY CONNECTOR]: Multiple inventory services enabled for " + gridURL);
                            }
                        }
                    }
                    //Reset the name
                    moduleConfig.Set("InventoryServices", Name);
                    m_Enabled = true;
                }
            }
        }

        public void PostInitialise()
        {
            if (!m_Enabled)
                return;
        }

        public void Close()
        {
            if (!m_Enabled)
                return;
        }

        public void AddRegion(Scene scene)
        {
            if (!m_Enabled)
                return;

            scene.RegisterModuleInterface<IInventoryService>(this);
        }

        public void RemoveRegion(Scene scene)
        {
            if (!m_Enabled)
                return;
        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_Enabled)
                return;
        }

        #region IInventoryService Members

        public bool CreateUserInventory(UUID user)
        {
            bool r = false;
            foreach (IInventoryService service in AllServices)
            {
                r = service.CreateUserInventory(user);
                if (r)
                    return r;
            }
            return r;
        }

        public List<OpenSim.Framework.InventoryFolderBase> GetInventorySkeleton(UUID userId)
        {
            List<OpenSim.Framework.InventoryFolderBase> r = new List<OpenSim.Framework.InventoryFolderBase>();
            foreach (IInventoryService service in AllServices)
            {
                r = service.GetInventorySkeleton(userId);
                if (r != null && r.Count != 0)
                    return r;
            }
            return r;
        }

        public OpenSim.Framework.InventoryCollection GetUserInventory(UUID userID)
        {
            OpenSim.Framework.InventoryCollection r = new OpenSim.Framework.InventoryCollection();
            foreach (IInventoryService service in AllServices)
            {
                r = service.GetUserInventory(userID);
                if (r != null && r.Folders.Count != 0)
                    return r;
            }
            return r;
        }

        public void GetUserInventory(UUID userID, InventoryReceiptCallback callback)
        {
            foreach (IInventoryService service in AllServices)
            {
                service.GetUserInventory(userID, callback);
            }
        }

        public OpenSim.Framework.InventoryFolderBase GetRootFolder(UUID userID)
        {
            OpenSim.Framework.InventoryFolderBase r = new OpenSim.Framework.InventoryFolderBase();
            foreach (IInventoryService service in AllServices)
            {
                r = service.GetRootFolder(userID);
                if (r != null)
                    return r;
            }
            return r;
        }

        public OpenSim.Framework.InventoryFolderBase GetFolderForType(UUID userID, AssetType type)
        {
            OpenSim.Framework.InventoryFolderBase r = new OpenSim.Framework.InventoryFolderBase();
            foreach (IInventoryService service in AllServices)
            {
                r = service.GetFolderForType(userID, type);
                if (r != null)
                    return r;
            }
            return r;
        }

        public OpenSim.Framework.InventoryCollection GetFolderContent(UUID userID, UUID folderID)
        {
            OpenSim.Framework.InventoryCollection r = new OpenSim.Framework.InventoryCollection();
            foreach (IInventoryService service in AllServices)
            {
                r = service.GetFolderContent(userID, folderID);
                if (r != null)
                    return r;
            }
            return r;
        }

        public List<OpenSim.Framework.InventoryItemBase> GetFolderItems(UUID userID, UUID folderID)
        {
            List<OpenSim.Framework.InventoryItemBase> r = new List<OpenSim.Framework.InventoryItemBase>();
            foreach (IInventoryService service in AllServices)
            {
                r.AddRange(service.GetFolderItems(userID, folderID));
            }
            return r;
        }

        public bool AddFolder(OpenSim.Framework.InventoryFolderBase folder)
        {
            bool r = false;
            foreach (IInventoryService service in AllServices)
            {
                r = service.AddFolder(folder);
                if (r)
                    return r;
            }
            return r;
        }

        public bool UpdateFolder(OpenSim.Framework.InventoryFolderBase folder)
        {
            bool r = false;
            foreach (IInventoryService service in AllServices)
            {
                r = service.UpdateFolder(folder);
                if (r)
                    return r;
            }
            return r;
        }

        public bool MoveFolder(OpenSim.Framework.InventoryFolderBase folder)
        {
            bool r = false;
            foreach (IInventoryService service in AllServices)
            {
                r = service.MoveFolder(folder);
                if (r)
                    return r;
            }
            return r;
        }

        public bool DeleteFolders(UUID userID, List<UUID> folderIDs)
        {
            bool r = false;
            foreach (IInventoryService service in AllServices)
            {
                r = service.DeleteFolders(userID, folderIDs);
                if (r)
                    return r;
            }
            return r;
        }

        public bool PurgeFolder(OpenSim.Framework.InventoryFolderBase folder)
        {
            bool r = false;
            foreach (IInventoryService service in AllServices)
            {
                r = service.PurgeFolder(folder);
                if (r)
                    return r;
            }
            return r;
        }

        public bool AddItem(OpenSim.Framework.InventoryItemBase item)
        {
            bool r = false;
            foreach (IInventoryService service in AllServices)
            {
                r = service.AddItem(item);
                if (r)
                    return r;
            }
            return r;
        }

        public bool UpdateItem(OpenSim.Framework.InventoryItemBase item)
        {
            bool r = false;
            foreach (IInventoryService service in AllServices)
            {
                r = service.UpdateItem(item);
                if (r)
                    return r;
            }
            return r;
        }

        public bool MoveItems(UUID ownerID, List<OpenSim.Framework.InventoryItemBase> items)
        {
            bool r = false;
            foreach (IInventoryService service in AllServices)
            {
                r = service.MoveItems(ownerID, items);
                if (r)
                    return r;
            }
            return r;
        }

        public bool LinkItem(OpenSim.Framework.IClientAPI client, UUID oldItemID, UUID parentID, uint Callback)
        {
            bool r = false;
            foreach (IInventoryService service in AllServices)
            {
                r = service.LinkItem(client, oldItemID, parentID, Callback);
                if (r)
                    return r;
            }
            return r;
        }

        public bool DeleteItems(UUID userID, List<UUID> itemIDs)
        {
            bool r = false;
            foreach (IInventoryService service in AllServices)
            {
                r = service.DeleteItems(userID, itemIDs);
                if (r)
                    return r;
            }
            return r;
        }

        public OpenSim.Framework.InventoryItemBase GetItem(OpenSim.Framework.InventoryItemBase item)
        {
            OpenSim.Framework.InventoryItemBase r = new OpenSim.Framework.InventoryItemBase();
            foreach (IInventoryService service in AllServices)
            {
                r = service.GetItem(item);
                if (r != null)
                    return r;
            }
            return r;
        }

        public OpenSim.Framework.InventoryFolderBase GetFolder(OpenSim.Framework.InventoryFolderBase folder)
        {
            OpenSim.Framework.InventoryFolderBase r = new OpenSim.Framework.InventoryFolderBase();
            foreach (IInventoryService service in AllServices)
            {
                r = service.GetFolder(folder);
                if (r != null)
                    return r;
            }
            return r;
        }

        public bool HasInventoryForUser(UUID userID)
        {
            bool r = false;
            foreach (IInventoryService service in AllServices)
            {
                r = service.HasInventoryForUser(userID);
                if (r)
                    return r;
            }
            return r;
        }

        public List<OpenSim.Framework.InventoryItemBase> GetActiveGestures(UUID userId)
        {
            List<OpenSim.Framework.InventoryItemBase> r = new List<OpenSim.Framework.InventoryItemBase>();
            foreach (IInventoryService service in AllServices)
            {
                r.AddRange(service.GetActiveGestures(userId));
            }
            return r;
        }

        public int GetAssetPermissions(UUID userID, UUID assetID)
        {
            int r = 0;
            foreach (IInventoryService service in AllServices)
            {
                r = service.GetAssetPermissions(userID, assetID);
                if (r != 0)
                    return r;
            }
            return r;
        }

        #endregion
    }
}
