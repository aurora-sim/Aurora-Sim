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
    public class InterWorldPlugin : IRegionModule
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
        
        private IConfig m_config;
        private Scene m_Scene;
        private List<Scene> m_scenes = new List<Scene>();

        /// <summary>
        /// All the regions that we have connected and have saved
        /// </summary>
        public Dictionary<OpenSim.Services.Interfaces.GridRegion, string> IWCConnectedRegions = new Dictionary<OpenSim.Services.Interfaces.GridRegion, string>();
        public List<UUID> OurRegions = new List<UUID>();
        IPresenceService presenceServer;

        /// <summary>
        /// All Connections
        /// </summary>
        public List<ConnectionIdentifier> Connections = new List<ConnectionIdentifier>();
        IGenericData GD = null;
        IProfileData PD = null;

        #region IRegionModule 
        public string Name
        {
            get { return "InterWorldComms"; }
        }

        public void Initialise(Scene scene, IConfigSource source)
        {
            m_Scene = scene;
            m_scenes.Add(scene);
            
            MainServer.Instance.AddXmlRPCHandler("InterWorldNewWorldConnection", InterWorldNewWorldConnection);
            MainServer.Instance.AddXmlRPCHandler("InterWorldAddNewPresence", InterWorldAddNewPresence);
            
            
            m_config = source.Configs["AuroraInterWorldConnectors"];
            WorldIdentifier = m_config.GetString("WorldIdentifier", "");
            
            scene.RegisterModuleInterface<InterWorldPlugin>(this);
            
            
            IConfig m_LoginServerConfig = source.Configs["LoginService"];
            string presenceService = m_LoginServerConfig.GetString("PresenceService", String.Empty);
            Object[] args = new Object[] { source };
            presenceServer = ServerUtils.LoadPlugin<IPresenceService>(presenceService, args);
        }

        public bool IsSharedModule
        {
            get { return true; }
        }

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
                m_log.Debug("[AuroraInterGridPlugin]: No Auth Service defined! Disabling...");
            }
            GD = Aurora.DataManager.DataManager.GetGenericPlugin();
            PD = Aurora.DataManager.DataManager.GetProfilePlugin();
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
            foreach (KeyValuePair<OpenSim.Services.Interfaces.GridRegion, string> region in IWCConnectedRegions)
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
            //It has asked the other server for a connection already.
            
            Hashtable request = new Hashtable();
            
            request["WorldIdentifier"] = Framework.Utils.Encrypt(Connection.Identifier, Connection.Identifier, Connection.Identifier);
            
            m_log.Info("[IWC MODULE]: Connecting to " + Connection + ".");
            
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
                        if (regTemp["Token"] == null)
                        {
                            regTemp.Remove("Token");
                            regTemp.Add("Token", "");
                        }
                        IWCConnectedRegions.Add(new OpenSim.Services.Interfaces.GridRegion(regTemp), Connection.Connection);
                    }
                    servers.Add(Connection, true);
                    m_log.Info("[IWC MODULE]: Connected to " + Connection + " successfully.");
                }
                else
                {
                    servers.Add(Connection, false);
                    m_log.Warn("[IWC MODULE]: Did not connect to " + Connection + " successfully. Reason: '" + result["Reason"].ToString() + "'.");
                }
            }
            else
            {
                servers.Add(Connection, false);
                m_log.Warn("[IWC MODULE]: Did not connect to " + Connection + " successfully.");
            }
        }

        #endregion

        #region Finding Users

        Dictionary<UUID, string> IWCUsers = new Dictionary<UUID, string>();
        public void AddIWCUser(string client, string homeConnection, string first, string last)
        {
            UUID agent = new UUID(client);
            IWCUsers.Add(agent, homeConnection);
            AuroraProfileData profile = PD.GetProfileInfo(agent);
            if (profile == null)
            {
                PD.CreateTemperaryAccount(client, first, last);
            }
        }

        public ConnectionIdentifier GetIWCUserHomeConnection(UUID client)
        {
            string homeConnection = "";
            IWCUsers.TryGetValue(client, out homeConnection);
            foreach (ConnectionIdentifier ident in Servers.Keys)
            {
                if (ident.Connection == homeConnection)
                    return ident;
            }
            return null;
        }

        #endregion

        #region Finding regions

        public GridRegionFlags GetRegionFlags(UUID UUID)
        {
            GridRegionFlags flags = new GridRegionFlags();
            foreach (OpenSim.Services.Interfaces.GridRegion region in IWCConnectedRegions.Keys)
            {
                int a = region.RegionID.CompareTo(UUID);
                if (region.RegionID.CompareTo(UUID) == 0)
                {
                    if (!OurRegions.Contains(region.RegionID))
                    {
                        flags.IsIWCConnected = true;
                        return flags;
                    }
                }
            }
            flags.IsIWCConnected = false;
            return flags;
        }

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
        #endregion

        #region Adding New Presence to another world

        public bool FireNewIWCUser(AgentCircuitData aCircuit, OpenSim.Services.Interfaces.GridRegion reg, OpenSim.Services.Interfaces.GridRegion finalDestination, out string reason)
        {
            string connection = string.Empty;
            reason = string.Empty;
            
            IWCConnectedRegions.TryGetValue(finalDestination, out connection);
            ConnectionIdentifier connIdent = null;
            foreach (ConnectionIdentifier con in Connections)
            {
                if (connection == con.Connection)
                    connIdent = con;
            }
            if (connIdent == null)
            {
                m_log.DebugFormat("[GATEKEEPER SERVICE]: Request to login foreign agent {0} {1} failed: Can not find the region.",aCircuit.firstname, aCircuit.lastname);
                return false;
            }
            
            m_log.DebugFormat("[GATEKEEPER SERVICE]: Request to login foreign agent {0} {1} @ ({2}) at destination {3}",
                aCircuit.firstname, aCircuit.lastname, aCircuit.AgentID, finalDestination.RegionName);

            UserAccount account = null;
            if (m_Scene.UserAccountService != null)
            {
                // Check to see if we have a local user with that UUID
                account = m_Scene.UserAccountService.GetUserAccount(UUID.Zero, aCircuit.AgentID);
                if (account == null)
                    return false;
            }

            m_log.Debug("[GATEKEEPER SERVICE]: User is ok");

            bool fired = FireStartPresence(aCircuit, connIdent, out reason);
            if (!fired)
            {
                m_log.Debug("[GATEKEEPER SERVICE]: Login new presence failed.");
                return false;
            }
            
            m_log.DebugFormat("[GATEKEEPER SERVICE]: Login presence is ok");


            //
            // Finally launch the agent at the destination
            //
            return m_Scene.SimulationService.CreateAgent(finalDestination, aCircuit, (uint)Constants.TeleportFlags.ViaLogin, out reason);
        }

        private bool FireStartPresence(AgentCircuitData aCircuit, ConnectionIdentifier connection, out string reason)
        {
            Hashtable request = new Hashtable();

            request["AgentID"] = Framework.Utils.Encrypt(aCircuit.AgentID.ToString(), connection.Identifier, connection.Identifier);
            request["SessionID"] = Framework.Utils.Encrypt(aCircuit.SessionID.ToString(), connection.Identifier, connection.Identifier);
            request["SecureSessionID"] = Framework.Utils.Encrypt(aCircuit.SecureSessionID.ToString(), connection.Identifier, connection.Identifier);
            request["FirstName"] = Framework.Utils.Encrypt(aCircuit.firstname, connection.Identifier, connection.Identifier);
            request["LastName"] = Framework.Utils.Encrypt(aCircuit.lastname, connection.Identifier, connection.Identifier);
            request["HomeConnection"] = Framework.Utils.Encrypt(connection.Connection, connection.Identifier, connection.Identifier);
            
            m_log.Info("[IWC MODULE]: Connecting to " + connection + ".");
            Hashtable result = Framework.Utils.GenericXMLRPCRequest(request, "InterWorldAddNewPresence", connection.Connection);
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

        private XmlRpcResponse InterWorldAddNewPresence(XmlRpcRequest request, IPEndPoint IPEndPoint)
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
            string reason = string.Empty;
            bool successful = false;

            #region Add New Presence
            if (!presenceServer.LoginAgent(AgentID, new UUID(SessionID), new UUID(SecureSessionID)))
            {
                reason = "Unable to login presence";
                m_log.InfoFormat("[GATEKEEPER SERVICE]: Presence login failed for foreign agent {0}. Refusing service.",
                    AgentID);
                successful = false;
            }
            else
            {
                m_log.InfoFormat("[GATEKEEPER SERVICE]: Presence login allowed for foreign agent {0}.",
                    AgentID);
                successful = true;
            }
            #endregion
            //Deal with creating a user for the agent.
            AddIWCUser(AgentID, HomeConnection, FirstName, LastName);
            
            responseData["reason"] = reason;
            responseData["addedpresence"] = successful;
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
