using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using Nini.Config;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using RegionFlags = Aurora.Framework.RegionFlags;

namespace Aurora.Services.DataService
{
    public class LocalInventoryConnector : IInventoryData
    {
        private IGenericData GD = null;
        private string m_foldersrealm = "inventoryfolders";
        private string m_itemsrealm = "inventoryitems";

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("AuthConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                string connectionString = defaultConnectionString;
                if (source.Configs[Name] != null)
                    connectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                GD.ConnectToDatabase(connectionString, "Inventory", source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

                DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IInventoryData"; }
        }

        public void Dispose()
        {
        }

        #region IInventoryData Members

        public List<InventoryFolderBase> GetFolders(string[] fields, string[] vals)
        {
            Dictionary<string, List<string>> retVal = GD.QueryNames (fields, vals, m_foldersrealm, "*");
            return ParseInventoryFolders (ref retVal);
        }

        public List<InventoryItemBase> GetItems(string[] fields, string[] vals)
        {
            string query = "";
            for (int i = 0; i < fields.Length; i++)
            {
                query += String.Format("{0} = '{1}' and ", fields[i], vals[i]);
                i++;
            }
            query = query.Remove(query.Length - 5);
            using (IDataReader reader = GD.QueryData(query, m_itemsrealm, "*"))
            {
                return ParseInventoryItems(reader);
            }
        }

        public OSDArray GetLLSDItems(string[] fields, string[] vals)
        {
            string query = "";
            for (int i = 0; i < fields.Length; i++)
            {
                query += String.Format("{0} = '{1}' and ", fields[i], vals[i]);
                i++;
            }
            query = query.Remove(query.Length - 5);
            using (IDataReader reader = GD.QueryData(query, m_itemsrealm, "*"))
            {
                return ParseLLSDInventoryItems(reader);
            }
        }

        private OSDArray ParseLLSDInventoryItems(IDataReader retVal)
        {
            OSDArray array = new OSDArray();

            while (retVal.Read())
            {
                for (int i = 0; i < retVal.FieldCount; i++)
                {
                    OSDMap item = new OSDMap();
                    OSDMap permissions = new OSDMap();
                    item["asset_id"] = UUID.Parse(retVal["assetID"].ToString());
                    item["name"] = retVal["inventoryName"].ToString();
                    item["desc"] = retVal["inventoryDescription"].ToString();
                    permissions["next_owner_mask"] = uint.Parse(retVal["inventoryNextPermissions"].ToString());
                    permissions["owner_mask"] = uint.Parse(retVal["inventoryCurrentPermissions"].ToString());
                    UUID creator;
                    if(UUID.TryParse(retVal["creatorID"].ToString(), out creator))
                        permissions["creator_id"] = creator;
                    else
                        permissions["creator_id"] = UUID.Zero;
                    permissions["base_mask"] = uint.Parse(retVal["inventoryBasePermissions"].ToString());
                    permissions["everyone_mask"] = uint.Parse(retVal["inventoryEveryOnePermissions"].ToString());
                    OSDMap sale_info = new OSDMap();
                    sale_info["sale_price"] = int.Parse(retVal["salePrice"].ToString());
                    switch (byte.Parse(retVal["saleType"].ToString()))
                    {
                        default:
                            sale_info["sale_type"] = "not";
                            break;
                        case 1:
                            sale_info["sale_type"] = "original";
                            break;
                        case 2:
                            sale_info["sale_type"] = "copy";
                            break;
                        case 3:
                            sale_info["sale_type"] = "contents";
                            break;
                    }
                    item["sale_info"] = sale_info;
                    item["created_at"] = int.Parse(retVal["creationDate"].ToString());
                    permissions["group_id"] = UUID.Parse(retVal["groupID"].ToString());
                    permissions["is_owner_group"] = int.Parse(retVal["groupOwned"].ToString()) == 1;
                    item["flags"] = uint.Parse(retVal["flags"].ToString());
                    item["item_id"] = UUID.Parse(retVal["inventoryID"].ToString());
                    item["parent_id"] = UUID.Parse(retVal["parentFolderID"].ToString());
                    permissions["group_mask"] = uint.Parse(retVal["inventoryGroupPermissions"].ToString());
                    item["agent_id"] = UUID.Parse(retVal["avatarID"].ToString());
                    permissions["owner_id"] = item["agent_id"];
                    permissions["last_owner_id"] = item["agent_id"];

                    item["type"] = Utils.AssetTypeToString((AssetType)int.Parse(retVal["assetType"].ToString()));
                    item["inv_type"] = Utils.InventoryTypeToString((InventoryType)int.Parse(retVal["invType"].ToString()));

                    item["permissions"] = permissions;

                    array.Add(item);
                }
            }
            retVal.Close();

            return array;
        }

        private List<InventoryFolderBase> ParseInventoryFolders(ref Dictionary<string, List<string>> retVal)
        {
            List<InventoryFolderBase> folders = new List<InventoryFolderBase> ();
            if (retVal.Count == 0)
                return folders;
            for (int i = 0; i < retVal.ElementAt(0).Value.Count; i++)
            {
                InventoryFolderBase folder = new InventoryFolderBase ();
                folder.Name = retVal["folderName"][i];
                folder.Type = short.Parse (retVal["type"][i]);
                folder.Version = (ushort)int.Parse (retVal["version"][i]);
                folder.ID = UUID.Parse (retVal["folderID"][i]);
                folder.Owner = UUID.Parse (retVal["agentID"][i]);
                folder.ParentID = UUID.Parse (retVal["parentFolderID"][i]);
                folders.Add (folder);
            }
            retVal.Clear();
            return folders;
        }

        private List<InventoryItemBase> ParseInventoryItems(IDataReader retVal)
        {
            List<InventoryItemBase> items = new List<InventoryItemBase> ();
            while (retVal.Read())
            {
                for (int i = 0; i < retVal.FieldCount; i++)
                {
                    InventoryItemBase item = new InventoryItemBase();
                    item.AssetID = UUID.Parse(retVal["assetID"].ToString());
                    item.AssetType = int.Parse(retVal["assetType"].ToString());
                    item.Name = retVal["inventoryName"].ToString();
                    item.Description = retVal["inventoryDescription"].ToString();
                    item.NextPermissions = uint.Parse(retVal["inventoryNextPermissions"].ToString());
                    item.CurrentPermissions = uint.Parse(retVal["inventoryCurrentPermissions"].ToString());
                    item.InvType = int.Parse(retVal["invType"].ToString());
                    item.CreatorId = retVal["creatorID"].ToString();
                    item.BasePermissions = uint.Parse(retVal["inventoryBasePermissions"].ToString());
                    item.EveryOnePermissions = uint.Parse(retVal["inventoryEveryOnePermissions"].ToString());
                    item.SalePrice = int.Parse(retVal["salePrice"].ToString());
                    item.SaleType = byte.Parse(retVal["saleType"].ToString());
                    item.CreationDate = int.Parse(retVal["creationDate"].ToString());
                    item.GroupID = UUID.Parse(retVal["groupID"].ToString());
                    item.GroupOwned = int.Parse(retVal["groupOwned"].ToString()) == 1;
                    item.Flags = uint.Parse(retVal["flags"].ToString());
                    item.ID = UUID.Parse(retVal["inventoryID"].ToString());
                    item.Owner = UUID.Parse(retVal["avatarID"].ToString());
                    item.Folder = UUID.Parse(retVal["parentFolderID"].ToString());
                    item.GroupPermissions = uint.Parse(retVal["inventoryGroupPermissions"].ToString());
                    items.Add(item);
                }
            }
            retVal.Close();
            return items;
        }

        public bool StoreFolder (InventoryFolderBase folder)
        {
            GD.Delete(m_foldersrealm, new string[1] { "folderID" }, new object[1] { folder.ID });
            return GD.Insert(m_foldersrealm, new string[6]{"folderName","type","version","folderID","agentID","parentFolderID"},
                new object[6]{folder.Name, folder.Type, folder.Version, folder.ID, folder.Owner, folder.ParentID});
        }

        public bool StoreItem (InventoryItemBase item)
        {
            GD.Delete(m_itemsrealm, new string[1] { "inventoryID" }, new object[1] { item.ID });
            return GD.Insert (m_itemsrealm, new string[20]{"assetID","assetType","inventoryName","inventoryDescription",
                "inventoryNextPermissions","inventoryCurrentPermissions","invType","creatorID","inventoryBasePermissions",
                "inventoryEveryOnePermissions","salePrice","saleType","creationDate","groupID","groupOwned",
                "flags","inventoryID","avatarID","parentFolderID","inventoryGroupPermissions"}, new object[20]{
                    item.AssetID, item.AssetType, item.Name, item.Description, item.NextPermissions, item.CurrentPermissions,
                    item.InvType, item.CreatorId, item.BasePermissions, item.EveryOnePermissions, item.SalePrice, item.SaleType,
                    item.CreationDate, item.GroupID, item.GroupOwned ? "1" : "0", item.Flags, item.ID, item.Owner,
                    item.Folder, item.GroupPermissions});
        }

        public bool DeleteFolders (string field, string val)
        {
            return GD.Delete (m_foldersrealm, new string[1] { field }, new object[1] { val });
        }

        public bool DeleteItems (string field, string val)
        {
            return GD.Delete (m_itemsrealm, new string[1] { field }, new object[1] { val });
        }

        public bool MoveItem (string id, string newParent)
        {
            return GD.Update (m_itemsrealm, new object[1] { newParent }, new string[1] { "parentFolderID" },
                new string[1] { "inventoryID" }, new object[1] { id });
        }

        public InventoryItemBase[] GetActiveGestures (UUID principalID)
        {
            string query = String.Format("{0} = '{1}' and {2} = '{3}'", "avatarID", principalID, "assetType", (int)AssetType.Gesture);

            using (IDataReader reader = GD.QueryData(query, m_itemsrealm, "*"))
            {
                List<InventoryItemBase> items = ParseInventoryItems(reader);
                items.RemoveAll(delegate(InventoryItemBase item)
                {
                    return !((item.Flags & 1) == 1); //1 means that it is active, so remove all ones that do not have a 1
                });
                return items.ToArray();
            }
        }

        public int GetAssetPermissions (UUID principalID, UUID assetID)
        {
            List<string> query = GD.Query (new string[2] { "avatarID", "assetID" }, new object[2] { principalID, assetID },
                m_itemsrealm, "inventoryCurrentPermissions");
            if (query.Count > 0)
                return int.Parse (query[0]);
            else
                return 0;
        }

        #endregion
    }
}
