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
        private bool a_Enabled = true;
        private string WorldIdentifier = "";
        private IIWCAuthenticationService IWCAuthService = null;

        /// <summary>
        /// Connection, have we asked them for a connection
        /// </summary>
        public List<ConnectionIdentifier> SuccessfullyConnectedToWorlds = new List<ConnectionIdentifier>();
        public List<ConnectionIdentifier> UnsuccessfulConnectedToWorlds = new List<ConnectionIdentifier>();

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

        private bool m_Enabled = true;
        private ConnectionIdentifier CurrentlyAsking = null;

        #region IRegionModule 
        public string Name{ get { return "InterWorldComms"; } }

        public void Initialise(Scene scene, IConfigSource source)
        {
            if (source.Configs["AuroraInterWorldConnectors"] != null)
            {
                if (source.Configs["AuroraInterWorldConnectors"].GetBoolean(
                        "Enabled", true) !=
                        true)
                {
                    m_Enabled = false;
                    return;
                }
            }
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
            if (!m_Enabled)
                return;
        	IWCAuthService = m_Scene.RequestModuleInterface<IIWCAuthenticationService>();
            foreach (Scene scene in m_scenes)
            {
                OurRegions.Add(scene.RegionInfo.RegionID);
            }
            System.Threading.Thread thread = new System.Threading.Thread(InformOtherWorldsAboutUs);
            thread.Start();
        }

        private void InformOtherWorldsAboutUs()
        {
            string connections = m_config.GetString("WorldsToInformOnStartup", "");
            string[] tempConn = connections.Split(',');
            string identifiers = m_config.GetString("WorldsToInformIdentifiers", "");
            string[] tempIdent = identifiers.Split(',');
            string trustLevels = m_config.GetString("WorldsToInformTrustLevels", "");
            string[] tempTrustLev = trustLevels.Split(',');
            int a = 0;
            List<ConnectionIdentifier> ConnectingTo = new List<ConnectionIdentifier>();
            List<ConnectionIdentifier> NewlyConnectedToWorlds = new List<ConnectionIdentifier>();
            foreach (string unnneeded in tempConn)
            {
                ConnectionIdentifier ident = new ConnectionIdentifier();
                ident.Connection = tempConn[a];
                ident.Identifier = tempIdent[a];
                ident.TrustLevel = tempTrustLev[a];
                ident.SessionHash = Guid.NewGuid().ToString();
                ConnectingTo.Add(ident);
                a++;
            }
            for(int i = 0; i < ConnectingTo.Count; i++)
            {
                CurrentlyAsking = ConnectingTo[i];
                bool successful = AskServerForConnection(ConnectingTo[i]);
                if (successful)
                {
                    if (!NewlyConnectedToWorlds.Contains(ConnectingTo[i]))
                        NewlyConnectedToWorlds.Add(ConnectingTo[i]);
                }
                else
                    if (!UnsuccessfulConnectedToWorlds.Contains(ConnectingTo[i]))
                        UnsuccessfulConnectedToWorlds.Add(ConnectingTo[i]);
                CurrentlyAsking = null;
            }
            foreach (ConnectionIdentifier connection in NewlyConnectedToWorlds)
            {
                if(UnsuccessfulConnectedToWorlds.Contains(connection))
                    UnsuccessfulConnectedToWorlds.Remove(connection);
                if(!SuccessfullyConnectedToWorlds.Contains(connection))
                    SuccessfullyConnectedToWorlds.Add(connection);
            }
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

            #region GetConnection
            ConnectionIdentifier con = null;
            foreach (ConnectionIdentifier connection in UnsuccessfulConnectedToWorlds)
            {
                string[] IPs = connection.Connection.Split(':');
                string IP = IPs[1].Substring(2);
                if (Dns.GetHostByName(IP).AddressList[0].ToString() == IPEndPoint.Address.ToString())
                {
                    con = connection;
                }
            }
            foreach (ConnectionIdentifier connection in SuccessfullyConnectedToWorlds)
            {
                string[] IPs = connection.Connection.Split(':');
                string IP = IPs[1].Substring(2);
                if (Dns.GetHostByName(IP).AddressList[0].ToString() == IPEndPoint.Address.ToString())
                {
                    con = connection;
                }
            }
            #endregion

            #region Check Authentication
            if (SentWorldIdentifier != WorldIdentifier)
            {
                responseData["Connected"] = "false";
                responseData["Reason"] = "Incorrect World Identifier";
                response.Value = responseData;
                return response;
            }
            bool authed = IWCAuthService.CheckAuthenticationServer(IPEndPoint);
            if (!authed)
            {
                responseData["Connected"] = "false";
                responseData["Reason"] = "Blocked Connection";
                response.Value = responseData;
                return response;
            }

            if (con == null)
            {
                responseData["Connected"] = "false";
                responseData["Reason"] = "Blocked Connection";
                response.Value = responseData;
                return response;
            } 
            responseData["Connected"] = "true";
            #endregion

            #region Get Our Regions and RegionInfos
            ArrayList tempRegions = new ArrayList();
            
            foreach (Scene scene in m_scenes)
                tempRegions.Add((object)scene.GridService.GetRegionByUUID(UUID.Zero, scene.RegionInfo.RegionID).ToKeyValuePairs());

            foreach (KeyValuePair<OpenSim.Services.Interfaces.GridRegion, ConnectionIdentifier> region in IWCConnectedRegions)
                tempRegions.Add((object)region.Key.ToKeyValuePairs());

            #region Add RegionInfos to the hashtable

            ArrayList RegionInfoKeys = new ArrayList();
            ArrayList RegionInfoValues = new ArrayList();
            if (GetTrustLevel("High",con))
            {
                foreach (Scene scene in m_scenes)
                {
                    OSDMap map = scene.RegionInfo.PackRegionInfoData();
                    KeyValuePair<string, string>[] pairs = new KeyValuePair<string, string>[map.Count];
                    int i = 0;
                    foreach (KeyValuePair<string, OSD> pair in map)
                    {
                        pairs[i] = new KeyValuePair<string, string>(pair.Key, pair.Value.ToString());
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
                    RegionInfoKeys.Add(ListKeys);
                    RegionInfoValues.Add(ListValues);
                }
            }

            responseData["RegionKeys"] = RegionInfoKeys;
            responseData["RegionValues"] = RegionInfoValues;

            #endregion

            #endregion

            responseData["AllWorlds"] = tempRegions;
            response.Value = responseData;

            #region Callback on the connecting server
            List<ConnectionIdentifier> NewlyConnectedToWorlds = new List<ConnectionIdentifier>();

            for (int i = 0; i < UnsuccessfulConnectedToWorlds.Count; i++)
            {
                string[] IPs = UnsuccessfulConnectedToWorlds[i].Connection.Split(':');
                string IP = IPs[1].Substring(2);
                if (UnsuccessfulConnectedToWorlds[i].Connection == con.Connection)
                {
                    if (UnsuccessfulConnectedToWorlds[i] != CurrentlyAsking)
                    {
                        bool successful = AskServerForConnection(UnsuccessfulConnectedToWorlds[i]);
                        if (successful)
                        {
                            if (!NewlyConnectedToWorlds.Contains(UnsuccessfulConnectedToWorlds[i]))
                                NewlyConnectedToWorlds.Add(UnsuccessfulConnectedToWorlds[i]);
                        }
                    }
                }
            }
            foreach (ConnectionIdentifier connection in NewlyConnectedToWorlds)
            {
                if (UnsuccessfulConnectedToWorlds.Contains(connection))
                    UnsuccessfulConnectedToWorlds.Remove(connection);
                if (!SuccessfullyConnectedToWorlds.Contains(connection))
                    SuccessfullyConnectedToWorlds.Add(connection);
            }

            NewlyConnectedToWorlds.Clear();
            #endregion

            return response;
        }

        private bool AskServerForConnection(ConnectionIdentifier Connection)
        {
            bool successful = false;
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

                        ArrayList ListKeys = result["RegionKeys"] as ArrayList;
                        ArrayList ListValues = result["RegionValues"] as ArrayList;
                        int a = 0;
                        List<RegionInfo> RegionInfos = new List<RegionInfo>();
                        foreach (ArrayList Keys in ListKeys)
                        {
                            ArrayList Values = ListValues[a] as ArrayList;
                            KeyValuePair<string, OSD>[] pairs = new KeyValuePair<string, OSD>[Keys.Count];
                            int i = 0;
                            foreach (string key in Keys)
                            {
                                OSDString osd = new OSDString((string)Values[i]);
                                pairs[i] = new KeyValuePair<string, OSD>((string)Keys[i], (OSD)osd);
                                i++;
                            }

                            Dictionary<string, OSD> dictionary = new Dictionary<string, OSD>();
                            foreach (KeyValuePair<string, OSD> pair in pairs)
                            {
                                dictionary.Add(pair.Key, pair.Value);
                            }

                            OSDMap map = new OSDMap(dictionary);
                            RegionInfo regionInfo = new RegionInfo();
                            regionInfo.UnpackRegionInfoData(map);
                            RegionInfos.Add(regionInfo);
                        }
                        

                        //Add neighbors
                        foreach (Scene scene in m_scenes)
                        {
                            foreach (RegionInfo info in RegionInfos)
                            {
                                bool found = TryFindNeighborRegion((int)scene.RegionInfo.RegionLocX, (int)info.RegionLocX, (int)scene.RegionInfo.RegionLocY, (int)info.RegionLocY);
                                if (found)
                                    scene.AddNeighborRegion(info);
                            }
                        }
                        IWCConnectedRegions.Add(region, Connection);
                        #endregion
                    }

                    successful = true;

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
            return successful;
        }

        #endregion

        #region Trust Level

        /// <summary>
        /// This determines whether the trust level between the two worlds is great enough to allow the action.
        /// The level is what is needed to fire the action. The connectionIdentifier is the connection.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="connectionIdentifier"></param>
        /// <returns></returns>
        private bool GetTrustLevel(string level, ConnectionIdentifier connectionIdentifier)
        {
            int NeededTrust = GetTrustLevelFromString(level);
            int ForeignWorldTrust = GetTrustLevelFromString(connectionIdentifier.TrustLevel);
            if (ForeignWorldTrust >= NeededTrust)
                return true;
            return false;
        }

        private int GetTrustLevelFromString(string level)
        {
            int CurrentWorldTrustLevel = 0;
            if (level == "Full")
                CurrentWorldTrustLevel = 4;
            if (level == "High")
                CurrentWorldTrustLevel = 3;
            if (level == "Medium")
                CurrentWorldTrustLevel = 2;
            if (level == "Low")
                CurrentWorldTrustLevel = 1;
            if (level == "None")
                CurrentWorldTrustLevel = 0;
            return CurrentWorldTrustLevel;
        }

        #endregion

        #region Finding Users

        public void AddForeignAgent(string client, string homeConnection, string first, string last)
        {
            UUID agent = new UUID(client);
            if(!ForeignAgents.ContainsKey(agent))
                ForeignAgents.Add(agent, homeConnection);
            //IUserProfileInfo profile = ProfileDataManager.GetProfileInfo(agent);
            //if (profile == null)
            //{
                //ProfileDataManager.CreateTemperaryAccount(client, first, last);
            //}
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
            foreach (ConnectionIdentifier ident in SuccessfullyConnectedToWorlds)
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

        private bool TryFindNeighborRegion(int locX, int locXNew, int locY, int locYNew)
        {
            if (locXNew - 1 == locX)
                return true;
            if (locXNew + 1 == locX)
                return true;
            if (locYNew + 1 == locY)
                return true;
            if (locYNew - 1 == locY)
                return true;
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
                    presenceServer.LogoutAgent(new UUID(SessionID));
                
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
                presenceServer.LogoutAgent(new UUID(SessionID));
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
            if (!presenceServer.LogoutAgent(new UUID(SessionID)))
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
