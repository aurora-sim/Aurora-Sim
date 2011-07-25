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
using System.Data;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using Nini.Config;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using RegionFlags = Aurora.Framework.RegionFlags;
using Aurora.Services.DataService;
using OpenSim.Services.Connectors;

namespace Aurora.Addon.Hypergrid
{
    public class RobustInventoryConnector : LocalInventoryConnector
    {
        private IRegistryCore m_registry;

        public override void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("InventoryConnector", "LocalConnector") == "RobustConnector")
            {
                GD = GenericData;
                m_registry = simBase;

                string connectionString = defaultConnectionString;
                if (source.Configs[Name] != null)
                    connectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                GD.ConnectToDatabase(connectionString, "Inventory", source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

                DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        #region IInventoryData Members

        public override byte[] FetchInventoryReply (OSDArray fetchRequest, UUID AgentID, UUID forceOwnerID)
        {
            LLSDSerializationDictionary contents = new LLSDSerializationDictionary();
            contents.WriteStartMap("llsd"); //Start llsd

            contents.WriteKey("folders"); //Start array items
            contents.WriteStartArray("folders"); //Start array folders

            foreach (OSD m in fetchRequest)
            {
                contents.WriteStartMap("internalContents"); //Start internalContents kvp
                OSDMap invFetch = (OSDMap)m;

                //UUID agent_id = invFetch["agent_id"].AsUUID();
                UUID owner_id = invFetch["owner_id"].AsUUID();
                UUID folder_id = invFetch["folder_id"].AsUUID();
                bool fetch_folders = invFetch["fetch_folders"].AsBoolean();
                bool fetch_items = invFetch["fetch_items"].AsBoolean();
                int sort_order = invFetch["sort_order"].AsInteger();

                //Set the normal stuff
                contents["agent_id"] = forceOwnerID == UUID.Zero ? owner_id : forceOwnerID;
                contents["owner_id"] = forceOwnerID == UUID.Zero ? owner_id : forceOwnerID;
                contents["folder_id"] = folder_id;

                contents.WriteKey("items"); //Start array items
                contents.WriteStartArray("items");
                List<UUID> moreLinkedItems = new List<UUID> ();
                int count = 0;
                bool addToCount = true;
                string invServer = "";
                bool isForeign = GetIsForeign (AgentID, "InventoryServerURI", m_registry, out invServer);
                IDataReader fretVal = null;
                if (isForeign)
                    fretVal = GetForeignInventory (AgentID, folder_id, invServer);
                string query = String.Format("where {0} = '{1}' and {2} = '{3}'", "parentFolderID", folder_id, "avatarID", AgentID);
            redoQuery:
                using (IDataReader retVal = isForeign ? fretVal : GD.QueryData (query, m_itemsrealm, "*"))
                {
                    try
                    {
                        while (retVal.Read ())
                        {
                            contents.WriteStartMap ("item"); //Start item kvp
                            UUID assetID = UUID.Parse (retVal["assetID"].ToString ());
                            contents["asset_id"] = assetID;
                            contents["name"] = retVal["inventoryName"].ToString ();
                            contents["desc"] = retVal["inventoryDescription"].ToString ();


                            contents.WriteKey ("permissions"); //Start permissions kvp
                            contents.WriteStartMap ("permissions");
                            contents["group_id"] = UUID.Parse (retVal["groupID"].ToString ());
                            contents["is_owner_group"] = int.Parse (retVal["groupOwned"].ToString ()) == 1;
                            contents["group_mask"] = uint.Parse (retVal["inventoryGroupPermissions"].ToString ());
                            contents["owner_id"] = forceOwnerID == UUID.Zero ?  UUID.Parse (retVal["avatarID"].ToString ()) : forceOwnerID;
                            contents["last_owner_id"] = UUID.Parse (retVal["avatarID"].ToString ());
                            contents["next_owner_mask"] = uint.Parse (retVal["inventoryNextPermissions"].ToString ());
                            contents["owner_mask"] = uint.Parse (retVal["inventoryCurrentPermissions"].ToString ());
                            UUID creator;
                            if (UUID.TryParse (retVal["creatorID"].ToString (), out creator))
                                contents["creator_id"] = creator;
                            else
                                contents["creator_id"] = UUID.Zero;
                            contents["base_mask"] = uint.Parse (retVal["inventoryBasePermissions"].ToString ());
                            contents["everyone_mask"] = uint.Parse (retVal["inventoryEveryOnePermissions"].ToString ());
                            contents.WriteEndMap (/*Permissions*/);

                            contents.WriteKey ("sale_info"); //Start permissions kvp
                            contents.WriteStartMap ("sale_info"); //Start sale_info kvp
                            contents["sale_price"] = int.Parse (retVal["salePrice"].ToString ());
                            switch (byte.Parse (retVal["saleType"].ToString ()))
                            {
                                default:
                                    contents["sale_type"] = "not";
                                    break;
                                case 1:
                                    contents["sale_type"] = "original";
                                    break;
                                case 2:
                                    contents["sale_type"] = "copy";
                                    break;
                                case 3:
                                    contents["sale_type"] = "contents";
                                    break;
                            }
                            contents.WriteEndMap (/*sale_info*/);


                            contents["created_at"] = int.Parse (retVal["creationDate"].ToString ());
                            contents["flags"] = uint.Parse (retVal["flags"].ToString ());
                            UUID inventoryID = UUID.Parse (retVal["inventoryID"].ToString ());
                            contents["item_id"] = inventoryID;
                            contents["parent_id"] = UUID.Parse (retVal["parentFolderID"].ToString ());
                            UUID avatarID = forceOwnerID == UUID.Zero ? UUID.Parse (retVal["avatarID"].ToString ()) : forceOwnerID;
                            contents["agent_id"] = avatarID;

                            AssetType assetType = (AssetType)int.Parse (retVal["assetType"].ToString ());
                            if(assetType == AssetType.Link)
                                moreLinkedItems.Add(assetID);
                            contents["type"] = Utils.AssetTypeToString (assetType);
                            InventoryType invType = (InventoryType)int.Parse (retVal["invType"].ToString ());
                            contents["inv_type"] = Utils.InventoryTypeToString (invType);

                            if(addToCount)
                                count++;
                            contents.WriteEndMap (/*"item"*/); //end array items
                        }
                    }
                    catch
                    {
                    }
                    finally
                    {
                        try
                        {
                            //if (retVal != null)
                            //{
                            //    retVal.Close ();
                            //    retVal.Dispose ();
                            //}
                        }
                        catch { }
                        GD.CloseDatabase ();
                    }
                }
                if(moreLinkedItems.Count > 0)
                {
                    addToCount = false;
                    query = String.Format("where {0} = '{1}' and (", "avatarID", AgentID);
                    for(int i = 0; i < moreLinkedItems.Count; i++)
                        query += String.Format("{0} = '{1}' or ", "inventoryID", moreLinkedItems[i]);
                    query = query.Remove (query.Length - 4, 4);
                    query += ")";
                    moreLinkedItems.Clear ();
                    goto redoQuery;
                }
                contents.WriteEndArray(/*"items"*/); //end array items

                contents.WriteStartArray ("categories"); //We don't send any folders
                int version = 0;
                List<string> versionRetVal = GD.Query ("folderID", folder_id, m_foldersrealm, "version, type");
                List<InventoryFolderBase> foldersToAdd = new List<InventoryFolderBase> ();
                if (versionRetVal.Count > 0)
                {
                    version = int.Parse (versionRetVal[0]);
                    if(int.Parse(versionRetVal[1]) == (int)AssetType.TrashFolder ||
                        int.Parse (versionRetVal[1]) == (int)AssetType.CurrentOutfitFolder ||
                        int.Parse (versionRetVal[1]) == (int)AssetType.LinkFolder)
                    {
                        //If it is the trash folder, we need to send its descendents, because the viewer wants it
                        query = String.Format ("where {0} = '{1}' and {2} = '{3}'", "parentFolderID", folder_id, "agentID", AgentID);
                        using (IDataReader retVal = GD.QueryData (query, m_foldersrealm, "*"))
                        {
                            try
                            {
                                while (retVal.Read ())
                                {
                                    contents.WriteStartMap ("folder"); //Start item kvp
                                    contents["folder_id"] = UUID.Parse (retVal["folderID"].ToString ());
                                    contents["parent_id"] = UUID.Parse (retVal["parentFolderID"].ToString ());
                                    contents["name"] = retVal["folderName"].ToString ();
                                    int type = int.Parse(retVal["type"].ToString ());
                                    contents["type"] = type;
                                    contents["preferred_type"] = type;
                                    
                                    count++;
                                    contents.WriteEndMap (/*"folder"*/); //end array items
                                }
                            }
                            catch
                            {
                            }
                            finally
                            {
                                try
                                {
                                    //if (retVal != null)
                                    //{
                                    //    retVal.Close ();
                                    //    retVal.Dispose ();
                                    //}
                                }
                                catch
                                {
                                }
                                GD.CloseDatabase ();
                            }
                        }
                    }
                }

                contents.WriteEndArray(/*"categories"*/);
                contents["descendents"] = count;
                contents["version"] = version;

                //Now add it to the folder array
                contents.WriteEndMap(); //end array internalContents
            }

            contents.WriteEndArray(); //end array folders
            contents.WriteEndMap(/*"llsd"*/); //end llsd

            try
            {
                return contents.GetSerializer ();
            }
            finally
            {
                contents = null;
            }
        }

        public static bool GetIsForeign (UUID AgentID, string server, IRegistryCore registry, out string serverURL)
        {
            serverURL = "";
            if (registry == null)
                return true;
            ICapsService caps = registry.RequestModuleInterface<ICapsService> ();
            IClientCapsService clientCaps = caps.GetClientCapsService (AgentID);
            if (clientCaps == null)
                return false;
            IRegionClientCapsService regionClientCaps = clientCaps.GetRootCapsService ();
            if (regionClientCaps == null)
                return false;
            Dictionary<string, object> urls = regionClientCaps.CircuitData.ServiceURLs;
            if (urls != null && urls.Count > 0)
            {
                serverURL = urls[server].ToString ();
                return true;
            }
            return false;
        }

        private IDataReader GetForeignInventory (UUID AgentID, UUID folder_id, string serverURL)
        {
            FakeDataReader d = new FakeDataReader ();
            IConfigurationService configService = m_registry.RequestModuleInterface<IConfigurationService> ();
            if (serverURL == "" && configService != null)
            {
                List<string> urls = configService.FindValueOf ("InventoryServerURI");
                if (urls.Count > 0)
                    serverURL = urls[0];
                else
                    return null;
            }
            XInventoryServicesConnector xinv = new XInventoryServicesConnector (serverURL + "xinventory");
            InventoryCollection c = xinv.GetFolderContent (AgentID, folder_id);
            if (c != null)
            {
                foreach (InventoryItemBase item in c.Items)
                {
                    d.items.Add (item);
                }
            }
            return d;
        }

        private class FakeDataReader : IDataReader
        {
            public List<InventoryItemBase> items = new List<InventoryItemBase> ();
            private int currentItem = 0;
            public void Close ()
            {
                items.Clear ();
            }

            public int Depth
            {
                get
                {
                    return items.Count;
                }
            }

            public DataTable GetSchemaTable ()
            {
                throw new NotImplementedException ();
            }

            public bool IsClosed
            {
                get
                {
                    return items.Count == 0;
                }
            }

            public bool NextResult ()
            {
                currentItem++;
                if (currentItem >= items.Count)
                    return false;
                return true;
            }

            public bool Read ()
            {
                currentItem++;
                if (currentItem >= items.Count)
                    return false;
                return true;
            }

            public int RecordsAffected
            {
                get
                {
                    throw new NotImplementedException ();
                }
            }

            public void Dispose ()
            {
            }

            public int FieldCount
            {
                get
                {
                    return items.Count;
                }
            }

            public bool GetBoolean (int i)
            {
                throw new NotImplementedException ();
            }

            public byte GetByte (int i)
            {
                throw new NotImplementedException ();
            }

            public long GetBytes (int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
            {
                throw new NotImplementedException ();
            }

            public char GetChar (int i)
            {
                throw new NotImplementedException ();
            }

            public long GetChars (int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
            {
                throw new NotImplementedException ();
            }

            public IDataReader GetData (int i)
            {
                throw new NotImplementedException ();
            }

            public string GetDataTypeName (int i)
            {
                throw new NotImplementedException ();
            }

            public DateTime GetDateTime (int i)
            {
                throw new NotImplementedException ();
            }

            public decimal GetDecimal (int i)
            {
                throw new NotImplementedException ();
            }

            public double GetDouble (int i)
            {
                throw new NotImplementedException ();
            }

            public Type GetFieldType (int i)
            {
                throw new NotImplementedException ();
            }

            public float GetFloat (int i)
            {
                throw new NotImplementedException ();
            }

            public Guid GetGuid (int i)
            {
                throw new NotImplementedException ();
            }

            public short GetInt16 (int i)
            {
                throw new NotImplementedException ();
            }

            public int GetInt32 (int i)
            {
                throw new NotImplementedException ();
            }

            public long GetInt64 (int i)
            {
                throw new NotImplementedException ();
            }

            public string GetName (int i)
            {
                throw new NotImplementedException ();
            }

            public int GetOrdinal (string name)
            {
                throw new NotImplementedException ();
            }

            public string GetString (int i)
            {
                throw new NotImplementedException ();
            }

            public object GetValue (int i)
            {
                throw new NotImplementedException ();
            }

            public int GetValues (object[] values)
            {
                throw new NotImplementedException ();
            }

            public bool IsDBNull (int i)
            {
                throw new NotImplementedException ();
            }

            public object this[string name]
            {
                get
                {
                    InventoryItemBase item = items[currentItem];
                    switch (name)
                    {
                        case "assetID":
                            return item.AssetID;
                            
                        case "inventoryName":
                            return item.Name;
                            
                        case "inventoryDescription":
                            return item.Description;
                            
                        case "groupID":
                            return item.GroupID;
                            
                        case "groupOwned":
                            return item.GroupOwned ? 1 : 0;
                            
                        case "inventoryGroupPermissions":
                            return item.GroupPermissions;
                            
                        case "avatarID":
                            return item.Owner;
                            
                        case "inventoryNextPermissions":
                            return item.NextPermissions;
                            
                        case "inventoryCurrentPermissions":
                            return item.CurrentPermissions;

                        case "creatorID":
                            return item.CreatorId;

                        case "creatorData":
                            return item.CreatorData;
                            
                        case "inventoryBasePermissions":
                            return item.BasePermissions;
                            
                        case "inventoryEveryOnePermissions":
                            return item.EveryOnePermissions;
                            
                        case "salePrice":
                            return item.SalePrice;
                            
                        case "saleType":
                            return item.SaleType;
                            
                        case "creationDate":
                            return item.CreationDate;
                            
                        case "flags":
                            return item.Flags;
                            
                        case "inventoryID":
                            return item.ID;
                            
                        case "parentFolderID":
                            return item.Folder;
                            
                        case "assetType":
                            return item.AssetType;
                            
                        case "invType":
                            return item.InvType;
                            
                        default:
                            return DBNull.Value;
                            
                    }
                }
            }

            public object this[int i]
            {
                get
                {
                    throw new NotImplementedException ();
                }
            }
        }

        #endregion
    }
}
