using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;   
using OpenSim.Framework.Servers.HttpServer;
using Nwc.XmlRpc;
using Nini.Config;
using Aurora.Framework;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Server.Base;
using log4net;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using Aurora.DataManager;
using OpenSim.Services.PresenceService;
using OpenMetaverse.StructuredData;

namespace Aurora.Modules
{
    public class ConnectionIdentifier
    {
        public string Connection;
        public string Identifier;
        public string TrustLevel;
        public string SessionHash;
        public override string ToString()
        {
            return "Connection: " + Connection +
                ", Identifier: " + Identifier +
                ", TrustLevel: " + TrustLevel;
        }
    }
    public class InterWorldComms : IRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IAuth a_AuthService = null;
        private bool a_Enabled = true;
        private string WorldIdentifier = "";
        
        /// <summary>
        /// Connection, have we asked them for a connection
        /// </summary>
        public Dictionary<ConnectionIdentifier, bool> Servers = new Dictionary<ConnectionIdentifier, bool>();
        public Dictionary<ConnectionIdentifier, bool> servers = new Dictionary<ConnectionIdentifier, bool>();

        /// <summary>
        /// Foreign agents that are in our world.
        /// </summary>
        Dictionary<UUID, string> ForeignAgents = new Dictionary<UUID, string>();
        /// <summary>
        /// Local Agents that are in foreign worlds.
        /// </summary>
        List<UUID> LocalAgents = new List<UUID>();

        private IConfig m_config;
        private Scene m_Scene;
        private List<Scene> m_scenes = new List<Scene>();

        /// <summary>
        /// All the regions that we have connected and have saved
        /// </summary>
        public Dictionary<OpenSim.Services.Interfaces.GridRegion, ConnectionIdentifier> IWCConnectedRegions = new Dictionary<OpenSim.Services.Interfaces.GridRegion, ConnectionIdentifier>();
        public List<UUID> OurRegions = new List<UUID>();
        
        private IPresenceService presenceServer;

        /// <summary>
        /// All Connections
        /// </summary>
        public List<ConnectionIdentifier> Connections = new List<ConnectionIdentifier>();
        
        private IGenericData GD = null;
        private IProfileData ProfileDataManager = null;

        #region IRegionModule 
        public string Name{ get { return "InterWorldComms"; } }

        public void Initialise(Scene scene, IConfigSource source)
        {
            m_Scene = scene;
            m_scenes.Add(scene);
            
            MainServer.Instance.AddXmlRPCHandler("InterWorldNewWorldConnection", InterWorldNewWorldConnection);
            MainServer.Instance.AddXmlRPCHandler("InterWorldAddNewRootPresence", InterWorldAddNewRootPresence);
            MainServer.Instance.AddXmlRPCHandler("InterWorldRemovePresence", InterWorldRemovePresence);
            
            m_config = source.Configs["AuroraInterWorldConnectors"];
            WorldIdentifier = m_config.GetString("WorldIdentifier", "");
            
            scene.RegisterModuleInterface<InterWorldComms>(this);
            
            
            IConfig m_LoginServerConfig = source.Configs["LoginService"];
            string presenceService = m_LoginServerConfig.GetString("PresenceService", String.Empty);
            Object[] args = new Object[] { source };
            presenceServer = ServerUtils.LoadPlugin<IPresenceService>(presenceService, args);
        }

        public bool IsSharedModule{ get { return true; } }

        public void Close() { }

        public void PostInitialise()
        {
            foreach (Scene scene in m_scenes)
            {
                OurRegions.Add(scene.RegionInfo.RegionID);
            }
            a_AuthService = m_Scene.RequestModuleInterface<IAuth>();
            if (a_AuthService == default(IAuth))
            {
                a_Enabled = false;
                m_log.Debug("[AuroraInterWorldComms]: No Auth Service defined! Disabling...");
            }
            GD = Aurora.DataManager.DataManager.GetGenericPlugin();
            ProfileDataManager = Aurora.DataManager.DataManager.GetProfilePlugin();
            InformOtherWorldsAboutUs();
        }

        private void InformOtherWorldsAboutUs()
        {
            string connections = m_config.GetString("WorldsToInformOnStartup", "");
            string[] tempConn = connections.Split(',');
            string identifiers = m_config.GetString("WorldsToInformIdentifiers", "");
            string[] tempIdent = identifiers.Split(',');
            string trustLevels = m_config.GetString("WorldsToInformTrustLevels", "");
            string[] tempTrustLev = identifiers.Split(',');
            int i = 0;
            foreach (string unnneeded in tempConn)
            {
                ConnectionIdentifier ident = new ConnectionIdentifier();
                ident.Connection = tempConn[i];
                ident.Identifier = tempIdent[i];
                ident.TrustLevel = tempTrustLev[i];
                ident.SessionHash = Guid.NewGuid().ToString();
                Connections.Add(ident);
            }
            foreach (ConnectionIdentifier Connection in Connections)
            {
                AskServerForConnection(Connection);
            }
            foreach (KeyValuePair<ConnectionIdentifier, bool> con in servers)
            {
                if (Servers.ContainsKey(con.Key))
                    Servers.Remove(con.Key);
                Servers.Add(con.Key, con.Value);
            }
            servers.Clear();
        }

        #endregion

        #region New Connection from a world

        public XmlRpcResponse InterWorldNewWorldConnection(XmlRpcRequest request, IPEndPoint IPEndPoint)
        {
            if (!a_Enabled)
                return new XmlRpcResponse();
            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable requestData = (Hashtable)request.Params[0];
            Hashtable responseData = new Hashtable();
            string SentWorldIdentifier = Framework.Utils.Decrypt((string)requestData["WorldIdentifier"], WorldIdentifier, WorldIdentifier);
            
            #region Check Authentication
            if (SentWorldIdentifier != WorldIdentifier)
            {
                responseData["Connected"] = "false";
                responseData["Reason"] = "Incorrect World Identifier";
                response.Value = responseData;
                return response;
            }
            bool authed = a_AuthService.CheckAuthenticationServer(IPEndPoint);
            if (!authed)
            {
                responseData["Connected"] = "false";
                responseData["Reason"] = "Blocked Connection";
                response.Value = responseData;
                return response;
            }
            responseData["Connected"] = "true";
            #endregion

            #region Get Our Regions
            ArrayList tempRegions = new ArrayList();
            
            foreach (Scene scene in m_scenes)
                tempRegions.Add((object)scene.GridService.GetRegionByUUID(UUID.Zero, scene.RegionInfo.RegionID).ToKeyValuePairs());

            foreach (KeyValuePair<OpenSim.Services.Interfaces.GridRegion, ConnectionIdentifier> region in IWCConnectedRegions)
                tempRegions.Add((object)region.Key.ToKeyValuePairs());
            
            #endregion

            responseData["AllWorlds"] = tempRegions;
            response.Value = responseData;

            #region Callback on the connecting server
            foreach (KeyValuePair<ConnectionIdentifier, bool> con in Servers)
            {
                string[] IPs = con.Key.Connection.Split(':');
                string IP = IPs[1].Substring(2);
                if (Dns.GetHostByName(IP).AddressList[0].ToString() == IPEndPoint.Address.ToString())
                {
                    if (con.Value == false)
                    {
                        lock(servers)
                            AskServerForConnection(con.Key);
                    }
                }
            }
            foreach (KeyValuePair<ConnectionIdentifier, bool> con in servers)
            {
                if (Servers.ContainsKey(con.Key))
                    Servers.Remove(con.Key);
                Servers.Add(con.Key, con.Value);
            }
            servers.Clear();
            #endregion

            return response;
        }

        private void AskServerForConnection(ConnectionIdentifier Connection)
        {
            Hashtable request = new Hashtable();
            request["WorldIdentifier"] = Framework.Utils.Encrypt(Connection.Identifier, Connection.Identifier, Connection.Identifier);
            
            m_log.Info("[IWC MODULE]: Connecting to " + Connection + ".");
            servers.Add(Connection, false);
            Hashtable result = Framework.Utils.GenericXMLRPCRequest(request, "InterWorldNewWorldConnection", Connection.Connection);

            if ((string)result["success"] == "true")
            {
                if (result["Connected"].ToString() == "true")
                {
                    ArrayList tempregion = result["AllWorlds"] as ArrayList;
                    foreach (object gridreg in tempregion)
                    {
                        Hashtable table = (Hashtable)gridreg;
                        Dictionary<string, object> regTemp = new Dictionary<string, object>();
                        foreach (DictionaryEntry keyval in table)
                        {
                            regTemp.Add(keyval.Key.ToString(), keyval.Value);
                        }

                        #region Null check
                        if (regTemp["Token"] == null)
                        {
                            regTemp.Remove("Token");
                            regTemp.Add("Token", "");
                        }
                        #endregion

                        #region Add Region to the grid and IWC databases
                        OpenSim.Services.Interfaces.GridRegion region = new OpenSim.Services.Interfaces.GridRegion(regTemp);
                        if (m_Scene.GridService.GetRegionByUUID(UUID.Zero,region.RegionID) != null)
                            m_Scene.GridService.RegisterRegion(UUID.Zero, region);
                        
                        IWCConnectedRegions.Add(region, Connection);
                        #endregion
                    }

                    servers.Remove(Connection);
                    servers.Add(Connection, true);

                    m_log.Info("[IWC MODULE]: Connected to " + Connection + " successfully.");
                }
                else
                {
                    m_log.Warn("[IWC MODULE]: Did not connect to " + Connection + " successfully. Reason: '" + result["Reason"].ToString() + "'.");
                }
            }
            else
            {
                m_log.Warn("[IWC MODULE]: Did not connect to " + Connection + " successfully.");
            }
        }

        #endregion

        #region Finding Users

        public void AddForeignAgent(string client, string homeConnection, string first, string last)
        {
            UUID agent = new UUID(client);
            if(!ForeignAgents.ContainsKey(agent))
                ForeignAgents.Add(agent, homeConnection);
            AuroraProfileData profile = ProfileDataManager.GetProfileInfo(agent);
            if (profile == null)
            {
                ProfileDataManager.CreateTemperaryAccount(client, first, last);
            }
        }

        public void RemoveForeignAgent(string client)
        {
            UUID agent = new UUID(client);
            ForeignAgents.Remove(agent);
        }

        public bool IsForeignAgent(UUID agentID)
        {
            return ForeignAgents.ContainsKey(agentID);
        }

        public void AddLocalAgent(UUID AgentID)
        {
            LocalAgents.Add(AgentID);
        }

        public void RemoveLocalAgent(UUID AgentID)
        {
            LocalAgents.Remove(AgentID);
        }

        public bool IsLocalAgent(UUID AgentID)
        {
            return LocalAgents.Contains(AgentID);
        }

        public ConnectionIdentifier GetForeignAgentsHomeConnection(UUID client)
        {
            string homeConnection = "";
            ForeignAgents.TryGetValue(client, out homeConnection);
            foreach (ConnectionIdentifier ident in Servers.Keys)
            {
                if (ident.Connection == homeConnection)
                    return ident;
            }
            return null;
        }

        public ConnectionIdentifier GetConnectionByRegion(OpenSim.Services.Interfaces.GridRegion region)
        {
            ConnectionIdentifier connection = null;
            foreach (OpenSim.Services.Interfaces.GridRegion IWCregion in IWCConnectedRegions.Keys)
            {
                if(IWCregion.RegionHandle == region.RegionHandle)
                    IWCConnectedRegions.TryGetValue(IWCregion, out connection);
            }
            return connection;
        }

        #endregion

        #region Finding regions

        public OpenSim.Services.Interfaces.GridRegion GetDefaultRegion(AgentCircuitData aCircuit, out Vector3 pos, out Vector3 lookAt)
        {
            pos = new Vector3();
            lookAt = new Vector3();
            return null;
        }

        public OpenSim.Services.Interfaces.GridRegion TryGetRegion(int x, int y)
        {
            foreach (OpenSim.Services.Interfaces.GridRegion region in IWCConnectedRegions.Keys)
            {
                if (region.RegionLocX == x && region.RegionLocY == y)
                {
                    return region;
                }
            }
            return null;
        }

        public bool IsRegionForeign(ulong p)
        {
            foreach (OpenSim.Services.Interfaces.GridRegion region in IWCConnectedRegions.Keys)
            {
                if (region.RegionHandle == p)
                    return true;
            }
            return false;
        }

        #endregion

        #region Adding New Presence/Removing New Presence to/from another world

        public bool FireNewIWCRootAgent(AgentCircuitData aCircuit, OpenSim.Services.Interfaces.GridRegion reg, OpenSim.Services.Interfaces.GridRegion finalDestination, bool createPresence, out string reason)
        {
            try
            {
                reason = string.Empty;

                ConnectionIdentifier connIdent = GetConnectionByRegion(finalDestination);
                if (connIdent == null)
                {
                    reason = "Can not find the region.";
                    m_log.DebugFormat("[IWC MODULE]: [FireNewIWCUser] Request to login foreign agent {0} {1} failed: Can not find the region.", aCircuit.firstname, aCircuit.lastname);
                    return false;
                }

                m_log.DebugFormat("[IWC MODULE]: [FireNewIWCUser] Request to login foreign agent {0} {1} @ ({2}) at destination {3}",
                    aCircuit.firstname, aCircuit.lastname, aCircuit.AgentID, finalDestination.RegionName);

                UserAccount account = null;
                if (m_Scene.UserAccountService != null)
                {
                    // Check to see if we have a local user with that UUID
                    account = m_Scene.UserAccountService.GetUserAccount(UUID.Zero, aCircuit.AgentID);
                    if (account == null)
                    {
                        if (!IsForeignAgent(aCircuit.AgentID))
                        {
                            reason = "Your account was not found.";
                            m_log.Debug("[IWC MODULE]: [FireNewIWCUser] User was not found. Refusing.");
                            return false;
                        }
                    }
                }

                bool fired = FireStartNewRootPresence(aCircuit, connIdent, finalDestination, createPresence, out reason);
                if (!fired)
                {
                    reason = "Logging in presence into foreign world failed.";
                    m_log.Debug("[IWC MODULE]: [FireNewIWCUser] Logging in presence into foreign world failed.");
                    return false;
                }

                if(!IsForeignAgent(aCircuit.AgentID))
                    if (!IsLocalAgent(aCircuit.AgentID))
                        AddLocalAgent(aCircuit.AgentID);

                m_log.DebugFormat("[IWC MODULE]: Local Agent added to Foreign World.");
                return true;
            }
            catch (Exception ex)
            {
                m_log.Error("[IWC Module]: [FireNewIWCUser] Error on Adding New Presence to foreign world: " + ex);
                reason = "Error on Adding New Presence to foreign world: " + ex;
                return false;
            }
        }

        private bool FireStartNewRootPresence(AgentCircuitData aCircuit, ConnectionIdentifier connection, OpenSim.Services.Interfaces.GridRegion region, bool createPresence, out string reason)
        {
            Hashtable request = new Hashtable();

            request["AgentID"] = Framework.Utils.Encrypt(aCircuit.AgentID.ToString(), connection.Identifier, connection.Identifier);
            request["SessionID"] = Framework.Utils.Encrypt(aCircuit.SessionID.ToString(), connection.Identifier, connection.Identifier);
            request["SecureSessionID"] = Framework.Utils.Encrypt(aCircuit.SecureSessionID.ToString(), connection.Identifier, connection.Identifier);
            request["FirstName"] = Framework.Utils.Encrypt(aCircuit.firstname, connection.Identifier, connection.Identifier);
            request["LastName"] = Framework.Utils.Encrypt(aCircuit.lastname, connection.Identifier, connection.Identifier);
            request["HomeConnection"] = Framework.Utils.Encrypt(connection.Connection, connection.Identifier, connection.Identifier);
            request["RegionID"] = Framework.Utils.Encrypt(region.RegionID.ToString(), connection.Identifier, connection.Identifier);
            request["AddPresence"] = createPresence.ToString();

            OSDMap map = aCircuit.PackAgentCircuitData();
            KeyValuePair<string, string>[] pairs = new KeyValuePair<string, string>[map.Count];
            int i = 0;
            foreach (KeyValuePair<string, OSD> pair in map)
            {
                pairs[i] = new KeyValuePair<string,string>(pair.Key,pair.Value.ToString());
                i++;
            }
            ArrayList ListKeys = new ArrayList();
            ArrayList ListValues = new ArrayList();
            foreach (KeyValuePair<string, string> pair in pairs)
            {
                ListKeys.Add(pair.Key);
            }
            foreach (KeyValuePair<string, string> pair in pairs)
            {
                ListValues.Add(pair.Value);
            }
            request["CircuitKeys"] = ListKeys;
            request["CircuitValues"] = ListValues;

            m_log.Info("[IWC MODULE]: [FireStartPresence] Connecting to " + connection + ".");

            Hashtable result = Framework.Utils.GenericXMLRPCRequest(request, "InterWorldAddNewRootPresence", connection.Connection);
            if ((string)result["success"] == "true")
            {
                if (result["reason"] == null)
                    result["reason"] = "";
                
                reason = result["reason"].ToString();
                return Convert.ToBoolean(result["addedpresence"]);
            }
            
            else
            {
                reason = "Can not connect to the region.";
                return false;
            }
        }

        private XmlRpcResponse InterWorldAddNewRootPresence(XmlRpcRequest request, IPEndPoint IPEndPoint)
        {
            if (!a_Enabled)
                return new XmlRpcResponse();
            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable requestData = (Hashtable)request.Params[0];
            Hashtable responseData = new Hashtable();
            string AgentID = Framework.Utils.Decrypt((string)requestData["AgentID"], WorldIdentifier, WorldIdentifier);
            string SessionID = Framework.Utils.Decrypt((string)requestData["SessionID"], WorldIdentifier, WorldIdentifier);
            string SecureSessionID = Framework.Utils.Decrypt((string)requestData["SecureSessionID"], WorldIdentifier, WorldIdentifier);
            string FirstName = Framework.Utils.Decrypt((string)requestData["FirstName"], WorldIdentifier, WorldIdentifier);
            string LastName = Framework.Utils.Decrypt((string)requestData["LastName"], WorldIdentifier, WorldIdentifier);
            string HomeConnection = Framework.Utils.Decrypt((string)requestData["HomeConnection"], WorldIdentifier, WorldIdentifier);
            string RegionUUID = Framework.Utils.Decrypt((string)requestData["RegionID"], WorldIdentifier, WorldIdentifier);
            bool AddPresence = Convert.ToBoolean(requestData["AddPresence"]);
            OpenSim.Services.Interfaces.GridRegion foundregion = m_Scene.GridService.GetRegionByUUID(UUID.Zero, new UUID(RegionUUID));
            
            ArrayList ListKeys = requestData["CircuitKeys"] as ArrayList;
            ArrayList ListValues = requestData["CircuitValues"] as ArrayList;

            KeyValuePair<string, OSD>[] pairs = new KeyValuePair<string, OSD>[ListValues.Count];
            int i = 0;
            foreach (string key in ListKeys)
            {
                OSDString osd = new OSDString((string)ListValues[i]);
                pairs[i] = new KeyValuePair<string, OSD>((string)ListKeys[i], (OSD)osd);
                i++;
            }

            Dictionary<string, OSD> dictionary = new Dictionary<string, OSD>();
            foreach (KeyValuePair<string, OSD> pair in pairs)
            {
                dictionary.Add(pair.Key, pair.Value);
            }

            OSDMap map = new OSDMap(dictionary);
            AgentCircuitData aCircuit = new AgentCircuitData();
            aCircuit.UnpackAgentCircuitData(map);

            string reason = string.Empty;
            bool successful = false;

            #region Add New Presence
            if(AddPresence)
            {
                // Kick existing agent.
                if (presenceServer.GetAgent(new UUID(SessionID)) != null)
                    presenceServer.LogoutAgent(new UUID(SessionID), new Vector3(), new Vector3());
                
                if (!presenceServer.LoginAgent(AgentID, new UUID(SessionID), new UUID(SecureSessionID)))
                {
                    reason = "Unable to login presence";
                    m_log.InfoFormat("[IWC MODULE]: [InterWorldAddNewRootPresence] Presence login failed for foreign agent {0}. Refusing service.",
                        AgentID);
                    successful = false;
                    responseData["reason"] = reason;
                    responseData["addedpresence"] = successful;
                    response.Value = responseData;
                    return response;
                }
                else
                {
                    m_log.InfoFormat("[IWC MODULE]: [InterWorldAddNewRootPresence] Presence login allowed for foreign agent {0}.",
                        aCircuit.firstname + " " + aCircuit.lastname);
                    successful = true;
                }
            }
            #endregion

            //Deal with creating a user for the agent.
            if(!IsForeignAgent(new UUID(AgentID)))
                AddForeignAgent(AgentID, HomeConnection, FirstName, LastName);
            
            //Launch agent
            successful = m_Scene.SimulationService.CreateAgent(foundregion, aCircuit, (uint)Constants.TeleportFlags.ViaLogin, out reason);
            
            if(successful)
                m_log.InfoFormat("[IWC MODULE]: [InterWorldAddNewRootPresence] Foreign agent {0} was able to connect to {1}.",
                        aCircuit.firstname + " " + aCircuit.lastname, foundregion.RegionName);

            if (!successful)
            {
                m_log.InfoFormat("[IWC MODULE]: [InterWorldAddNewRootPresence] Foreign agent {0} was not able to connect to {1}.",
                        aCircuit.firstname + " " + aCircuit.lastname, foundregion.RegionName);
                //Kick the agent we just made.
                presenceServer.LogoutAgent(new UUID(SessionID), new Vector3(), new Vector3());
                IAgentData agentdata = null;
                //Just so if something went wrong after the agent was created.
                if (m_Scene.SimulationService.RetrieveAgent(foundregion, new UUID(AgentID), out agentdata) == true)
                    m_Scene.SimulationService.CloseAgent(foundregion, new UUID(AgentID));
            }
            responseData["reason"] = reason;
            responseData["addedpresence"] = successful;
            response.Value = responseData;
            return response;
        }

        internal bool FireLogOutIWCUser(AgentCircuitData aCircuit, out string reason)
        {
            reason = "";
            ConnectionIdentifier connection = GetForeignAgentsHomeConnection(aCircuit.AgentID);

            if (connection == null)
            {
                m_log.Info("[IWC MODULE]: Removing Presence for agent "+ aCircuit.firstname + " " +aCircuit.lastname +" failed: Could not find the connection.");
                reason = "Could not find the foreign agents home connection.";
                return false;
            }
            
            Hashtable request = new Hashtable();

            request["SessionID"] = Framework.Utils.Encrypt(aCircuit.SessionID.ToString(), connection.Identifier, connection.Identifier);
            request["AgentID"] = Framework.Utils.Encrypt(aCircuit.AgentID.ToString(), connection.Identifier, connection.Identifier);
            
            m_log.Info("[IWC MODULE]: Removing Presence for agent "+ aCircuit.firstname + " " +aCircuit.lastname +" at " + connection + ".");
            
            Hashtable result = Framework.Utils.GenericXMLRPCRequest(request, "InterWorldRemovePresence", connection.Connection);
            
            if ((string)result["success"] == "true")
            {
                if (result["reason"] == null)
                    result["reason"] = "";

                reason = result["reason"].ToString();
                m_log.Info("[IWC MODULE]: Removed Presence for agent " + aCircuit.firstname + " " + aCircuit.lastname + " successfully.");
                return Convert.ToBoolean(result["removedpresence"]);
            }
            else
            {
                m_log.Info("[IWC MODULE]: Removed Presence for agent " + aCircuit.firstname + " " + aCircuit.lastname + " unsuccessfully.");
                reason = "Can not connect to the region.";
                return false;
            }
        }

        private XmlRpcResponse InterWorldRemovePresence(XmlRpcRequest request, IPEndPoint IPEndPoint)
        {
            if (!a_Enabled)
                return new XmlRpcResponse();
            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable requestData = (Hashtable)request.Params[0];
            Hashtable responseData = new Hashtable();
            string AgentID = Framework.Utils.Decrypt((string)requestData["AgentID"], WorldIdentifier, WorldIdentifier);
            string SessionID = Framework.Utils.Decrypt((string)requestData["SessionID"], WorldIdentifier, WorldIdentifier);
            string reason = string.Empty;
            bool successful = false;

            #region Logout Presence
            if (!presenceServer.LogoutAgent(new UUID(SessionID),new Vector3(),new Vector3()))
            {
                reason = "Unable to login presence";
                m_log.Info("[IWC MODULE]: Presence logout failed for local agent.");
                successful = false;
            }
            else
                successful = true;

            #endregion

            //Deal with removing the agent.
            if(IsForeignAgent(new UUID(AgentID)))
                RemoveForeignAgent(AgentID);
            if (IsLocalAgent(new UUID(AgentID)))
                RemoveLocalAgent(new UUID(AgentID));

            m_log.Info("[IWC MODULE]: Presence logout was successful for local agent.");
            responseData["reason"] = reason;
            responseData["removedpresence"] = successful;
            response.Value = responseData;
            return response;
        }

        #endregion

        #region Generic Data Transfers between worlds

        private object[] RequestIWCAgentData(UUID agentID, string FunctionName, object param)
        {
            /*Hashtable request = new Hashtable();
            string connection = GetIWCUserHomeConnection(agentID).Connection;
            request["AgentID"] = Framework.Utils.Encrypt(agentID.ToString(), connection, connection);
            request["FunctionName"] = Framework.Utils.Encrypt(FunctionName, connection, connection);
            request["param"] = param;
            
            Hashtable result = Framework.Utils.GenericXMLRPCRequest(request, "InterWorldRequestData", connection);
            if ((string)result["success"] == "true")
            {
                if (result["reason"] == null)
                    result["reason"] = "";

                //reason = result["reason"].ToString();
                return null;
            }
            else
            {*/
                return null;
            //}
        }

        private XmlRpcResponse InterWorldRequestData(XmlRpcRequest request, IPEndPoint IPEndPoint)
        {
            if (!a_Enabled)
                return new XmlRpcResponse();
            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable requestData = (Hashtable)request.Params[0];
            Hashtable responseData = new Hashtable();
            string AgentID = Framework.Utils.Decrypt((string)requestData["AgentID"], WorldIdentifier, WorldIdentifier);
            string FunctionName = Framework.Utils.Decrypt((string)requestData["FunctionName"], WorldIdentifier, WorldIdentifier);
            object param = requestData["param"];

            m_Scene.AuroraEventManager.FireGenericEventHandler("OnIWCRequestData", param);

            //responseData["reason"] = reason;
            //responseData["addedpresence"] = successful;
            response.Value = responseData;
            return response;
        }

        #endregion
    }
}
