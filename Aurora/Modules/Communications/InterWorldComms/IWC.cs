using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using OpenSim.Framework.Servers.HttpServer;
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
using Mono.Addins;

namespace Aurora.Modules
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule")]
    public class IWComms : ISharedRegionModule
    {
        #region Declares
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public string OurPassword = "";
        private IConfig m_config;
        private List<Scene> m_scenes = new List<Scene>();
        private List<OpenSim.Services.Interfaces.GridRegion> m_ForeignRegions = new List<OpenSim.Services.Interfaces.GridRegion>();
        private bool m_Enabled = true;
        private bool m_RegionLoaded = false;
        private List<ConnectionIdentifier> Connections;
        public IWCOutgoingPublicConnections OutgoingPublicComms;
        public IWCOutgoingPrivateConnections OutgoingPrivateComms;
        private IGridService m_GridService;
        #endregion

        #region ISharedRegionModule

        public void Initialise(IConfigSource source)
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
            m_config = source.Configs["AuroraInterWorldConnectors"];
            OurPassword = m_config.GetString("OurPassword", "");
        }

        public bool IsSharedModule { get { return true; } }

        public string Name { get { return "InterWorldCommunications"; } }

        public void Close() { }

        public void PostInitialise()
        {
        }

        public void AddRegion(Scene scene)
        {
            if (!m_Enabled)
                return;
            scene.RegisterModuleInterface<IWComms>(this);
            m_scenes.Add(scene);
        }

        public void RemoveRegion(Scene scene)
        {
            m_scenes.Remove(scene);
        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_Enabled)
                return;

            if (m_RegionLoaded)
                return;

            m_GridService = scene.GridService;

            //Set up the public connection
            MainServer.Instance.AddStreamHandler(new IWCIncomingPublicConnections(this));

            //Startup outgoing
            OutgoingPrivateComms = new IWCOutgoingPrivateConnections(this);
            OutgoingPublicComms = new IWCOutgoingPublicConnections(this);

            //Make our connection strings.
            Connections = BuildConnections();

            ContactOtherServers();

            //Don't do this again
            m_RegionLoaded = true;
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion

        private void ContactOtherServers()
        {
            List<ConnectionIdentifier> BadConnections = new List<ConnectionIdentifier>();
            foreach (ConnectionIdentifier connection in Connections)
            {
                if (!OutgoingPublicComms.AskOtherServerForConnection(connection))
                    BadConnections.Add(connection);
            }
            foreach (ConnectionIdentifier connection in BadConnections)
            {
                Connections.Remove(connection);
            }

            foreach (ConnectionIdentifier connection in Connections)
            {
                OutgoingPrivateComms.GetRegions(connection);
            }
        }

        #region Helpers

        public void RetriveOtherServersRegions(Dictionary<string, object> replyData)
        {
            //Retrive their sims...
            List<OpenSim.Services.Interfaces.GridRegion> Sims = new List<OpenSim.Services.Interfaces.GridRegion>();
            foreach (object f in replyData)
            {
                KeyValuePair<string, object> value = (KeyValuePair<string, object>)f;
                if (value.Value is Dictionary<string, object>)
                {
                    Dictionary<string, object> valuevalue = value.Value as Dictionary<string, object>;
                    OpenSim.Services.Interfaces.GridRegion map = new OpenSim.Services.Interfaces.GridRegion(valuevalue);
                    m_ForeignRegions.Add(map);
                }
            }
            foreach (OpenSim.Services.Interfaces.GridRegion region in m_ForeignRegions)
            {
                UUID SecureSessionID = UUID.Zero;
                m_GridService.RegisterRegion(region.ScopeID, region, SecureSessionID, out SecureSessionID);
                region.Token = SecureSessionID.ToString();
            }
        }

        public Dictionary<string, object> BuildOurLocalRegions()
        {
            Dictionary<string, object> Regions = new Dictionary<string, object>();

            foreach (Scene scene in m_scenes)
            {
                OpenSim.Services.Interfaces.GridRegion region = scene.GridService.GetRegionByUUID(UUID.Zero, scene.RegionInfo.RegionID);
                if (region != null)
                {
                    string RegionName = region.RegionName.Replace(" ", ""); // Remove spaces
                    if (!Regions.ContainsKey(RegionName))
                        Regions.Add(RegionName, region.ToKeyValuePairs());
                }
            }
            return Regions;
        }

        private List<ConnectionIdentifier> BuildConnections()
        {
            string connections = m_config.GetString("WorldsToInformOnStartup", "");
            string[] tempConn = connections.Split(',');
            string identifiers = m_config.GetString("WorldsToInformPasswords", "");
            string[] tempIdent = identifiers.Split(',');
            string trustLevels = m_config.GetString("WorldsToInformTrustLevels", "");
            string[] tempTrustLev = trustLevels.Split(',');
            int a = 0;
            List<ConnectionIdentifier> ConnectingTo = new List<ConnectionIdentifier>();
            foreach (string unnneeded in tempConn)
            {
                ConnectionIdentifier ident = new ConnectionIdentifier();
                ident.ConnectionPath = tempConn[a];
                ident.ForeignPassword = tempIdent[a];
                ident.TrustLevel = (TrustLevel)int.Parse(tempTrustLev[a]);
                ident.SessionHash = UUID.Random().ToString();
                ConnectingTo.Add(ident);
                a++;
            }
            return ConnectingTo;
        }

        #endregion

        internal void TeleportingAgent(ScenePresence sp, OpenSim.Services.Interfaces.GridRegion finalDestination)
        {
            if (!IsLocalRegion(finalDestination.RegionID))
            {
                foreach (ConnectionIdentifier connection in Connections)
                {
                    if (connection.ConnectionPath.Contains(finalDestination.ExternalHostName.ToString()))
                    {
                        OutgoingPrivateComms.SendIncomingAgent(connection, sp.UUID, finalDestination.RegionID);
                    }
                }
            }
        }

        public bool IsLocalRegion(UUID id)
        {
            foreach (Scene s in m_scenes)
                if (s.RegionInfo.RegionID == id)
                    return true;
            return false;
        }

        public void IncomingIWCBasedAgent(ConnectionIdentifier connection, UUID userID, UUID regionID)
        {
            bool success = m_scenes[0].PresenceService.LoginAgent(userID.ToString(), UUID.Random(), UUID.Random());
            UserAccount account = OutgoingPrivateComms.GetUserAccount(connection, userID);
            m_scenes[0].UserAccountService.StoreUserAccount(account);
        }

        public UserAccount GetUserAccount(UUID userID)
        {
            return m_scenes[0].UserAccountService.GetUserAccount(UUID.Zero, userID);
        }
    }

    #region Public connectors

    /// <summary>
    /// This deals with making a secure connection with another instance.
    /// It calls IWCIncomingPublicConnections on the foreign server.
    /// </summary>
    public class IWCOutgoingPublicConnections
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IWComms IWC;

        public IWCOutgoingPublicConnections(IWComms iwc)
        {
            IWC = iwc;
        }

        /// <summary>
        /// This contacts the foreign server and sends all the SimMaps of the local regions 
        /// to the foreign server. This will either get back a refused connection, or a 
        /// successful connection with a List of SimMap's that are that server's local regions
        /// </summary>
        /// <param name="connection">Foreign server to connect to</param>
        public bool AskOtherServerForConnection(ConnectionIdentifier connector)
        {
            Dictionary<string, object> sendData = IWC.BuildOurLocalRegions();

            sendData["Password"] = connector.ForeignPassword;
            foreach(KeyValuePair<string, object> pair in connector.ToKVP())
            {
                sendData.Add(pair.Key, pair.Value);
            }

            sendData["METHOD"] = "newconnection";

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        connector.ConnectionPath + "/iwcconnection",
                        reqString);

                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        if (!replyData.ContainsKey("result"))
                        {
                            m_log.Warn("[IWC]: Unable to connect successfully to " + connector.ConnectionPath + ", connection did not have all the required data.");
                            return false;
                        }

                        if (replyData["result"].ToString() == "Refused")
                        {
                            m_log.Warn("[IWC]: Unable to connect successfully to " + connector.ConnectionPath + ", the connection was refused.");
                            return false;
                        }

                        if (replyData["result"].ToString() == "WrongPassword")
                        {
                            m_log.Warn("[IWC]: Unable to connect successfully to " + connector.ConnectionPath + ", the foreign password was incorrect.");
                            return false;
                        }

                        if (replyData["result"].ToString() == "Successful")
                        {
                            connector.ForeignURL = (string)replyData["URL"];
                            m_log.Warn("[IWC]: Connected successfully to " + connector.ConnectionPath);
                            return true;
                        }
                    }
                    else
                    {
                        m_log.Warn("[IWC]: Unable to connect successfully to " + connector.ConnectionPath);
                        return false;
                    }
                }
                else
                {
                    m_log.Warn("[IWC]: Unable to connect successfully to " + connector.ConnectionPath);
                    return false;
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[IWCOutgoingConnector]: Exception when contacting server: {0}", e.Message);
                return false;
            }
            return false;
        }
    }


    /// <summary>
    /// This deals with connections that want to become secure with the current instance.
    /// This handler is not secure and is public and is called by IWCOutgoingPublicConnections on the foreign server.
    /// This handler sets up IWCIncomingPrivateConnection, which is secured and protected.
    /// </summary>
    public class IWCIncomingPublicConnections : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IWComms IWC;

        public IWCIncomingPublicConnections(IWComms iwc) :
            base("POST", "/iwcconnection")
        {
            IWC = iwc;
        }

        public override byte[] Handle(string path, Stream requestData,
                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            StreamReader sr = new StreamReader(requestData);
            string body = sr.ReadToEnd();
            sr.Close();
            body = body.Trim();

            string method = "";
            try
            {
                Dictionary<string, object> request =
                        ServerUtils.ParseXmlResponse(body);

                if (!request.ContainsKey("METHOD"))
                    return FailureResult();

                method = request["METHOD"].ToString();

                switch (method)
                {
                    case "newconnection":
                        return NewConnection(request);

                }
                m_log.DebugFormat("[IWCConnector]: unknown method {0} request {1}", method.Length, method);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[IWCConnector]: Exception {0} in " + method, e);
            }

            return FailureResult();

        }

        /// <summary>
        /// This deals with incoming requests to add this server to their map.
        /// This refuses or successfully allows the foreign server to interact with
        /// this region.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private byte[] NewConnection(Dictionary<string, object> request)
        {
            ConnectionIdentifier connector = new ConnectionIdentifier(request);

            Dictionary<string, object> result = new Dictionary<string, object>();

            if (connector.ForeignPassword != IWC.OurPassword)
            {
                result["result"] = "WrongPassword";
            }

            result["result"] = "Successful";
            result["URL"] = BuildNewSecretURL(connector);

            return Return(result);
        }

        private string BuildNewSecretURL(ConnectionIdentifier connector)
        {
            string URL = "/" + connector.SessionHash + "/" + UUID.Random() + "/";
            MainServer.Instance.AddStreamHandler(new IWCIncomingPrivateConnection(IWC, URL, connector));
            return URL;
        }

        #region Misc

        private byte[] Return(Dictionary<string, object> result)
        {
            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] SuccessResult()
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                    "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                    "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "Result", "");
            result.AppendChild(doc.CreateTextNode("Success"));

            rootElement.AppendChild(result);

            return DocToBytes(doc);
        }

        private byte[] FailureResult()
        {
            return FailureResult(String.Empty);
        }

        private byte[] FailureResult(string msg)
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                    "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                    "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "Result", "");
            result.AppendChild(doc.CreateTextNode("Failure"));

            rootElement.AppendChild(result);

            XmlElement message = doc.CreateElement("", "Message", "");
            message.AppendChild(doc.CreateTextNode(msg));

            rootElement.AppendChild(message);

            return DocToBytes(doc);
        }

        private byte[] DocToBytes(XmlDocument doc)
        {
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xw = new XmlTextWriter(ms, null);
            xw.Formatting = Formatting.Indented;
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }

        #endregion
    }


    #endregion

    /// <summary>
    /// This deals with incoming connects that are known to be secure on both ends
    /// It deals with moving of agents and their information across instances
    /// </summary>
    public class IWCIncomingPrivateConnection : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IWComms IWC;
        private ConnectionIdentifier Connection;

        public IWCIncomingPrivateConnection(IWComms iwc, string URL, ConnectionIdentifier connection) :
            base("POST", URL)
        {
            IWC = iwc;
            Connection = connection;
        }

        public override byte[] Handle(string path, Stream requestData,
                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            StreamReader sr = new StreamReader(requestData);
            string body = sr.ReadToEnd();
            sr.Close();
            body = body.Trim();

            string method = "";
            try
            {
                Dictionary<string, object> request =
                        ServerUtils.ParseXmlResponse(body);

                if (!request.ContainsKey("METHOD"))
                    return FailureResult();

                method = request["METHOD"].ToString();

                switch (method)
                {
                    case "getregions":
                        return GetRegions(request);
                    case "sendincomingagent":
                        return SendIncomingAgent(request);
                    case "getuseraccount":
                        return GetUserAccount(request);

                }
                m_log.DebugFormat("[IWCConnector]: unknown method {0} request {1}", method.Length, method);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[IWCConnector]: Exception {0} in " + method, e);
            }

            return FailureResult();

        }

        private byte[] GetRegions(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            IWC.RetriveOtherServersRegions(request);

            result["result"] = "Successful";
            result = IWC.BuildOurLocalRegions();

            return Return(result);
        }

        private byte[] SendIncomingAgent(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            //TODO: Protection against unwanted agents here

            UUID userID = UUID.Parse(request["USERID"].ToString());
            UUID regionID = UUID.Parse(request["REGIONID"].ToString());

            IWC.IncomingIWCBasedAgent(Connection, userID, regionID);

            result["result"] = "Successful";

            return Return(result);
        }

        private byte[] GetUserAccount(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID userID = UUID.Parse(request["USERID"].ToString());

            UserAccount account = IWC.GetUserAccount(userID);

            result["result"] = account.ToKeyValuePairs();

            return Return(result);
        }

        #region Misc

        private byte[] Return(Dictionary<string, object> result)
        {
            string xmlString = ServerUtils.BuildXmlResponse(result);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] SuccessResult()
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                    "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                    "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "Result", "");
            result.AppendChild(doc.CreateTextNode("Success"));

            rootElement.AppendChild(result);

            return DocToBytes(doc);
        }

        private byte[] FailureResult()
        {
            return FailureResult(String.Empty);
        }

        private byte[] FailureResult(string msg)
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                    "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                    "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "Result", "");
            result.AppendChild(doc.CreateTextNode("Failure"));

            rootElement.AppendChild(result);

            XmlElement message = doc.CreateElement("", "Message", "");
            message.AppendChild(doc.CreateTextNode(msg));

            rootElement.AppendChild(message);

            return DocToBytes(doc);
        }

        private byte[] DocToBytes(XmlDocument doc)
        {
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xw = new XmlTextWriter(ms, null);
            xw.Formatting = Formatting.Indented;
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }

        #endregion
    }

    /// <summary>
    /// This handler calls IWCIncomingPrivateConnection with the secure URL and interacts with the foreign server
    /// </summary>
    public class IWCOutgoingPrivateConnections
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IWComms IWC;

        public IWCOutgoingPrivateConnections(IWComms iwc)
        {
            IWC = iwc;
        }

        /// <summary>
        /// This contacts the foreign server and sends all the SimMaps of the local regions 
        /// to the foreign server. This will either get back a refused connection, or a 
        /// successful connection with a List of SimMap's that are that server's local regions
        /// </summary>
        /// <param name="connection">Foreign server to connect to</param>
        public bool GetRegions(ConnectionIdentifier connection)
        {
            Dictionary<string, object> sendData = IWC.BuildOurLocalRegions();

            sendData["METHOD"] = "getregions";

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        connection.ConnectionPath + connection.ForeignURL, //The full secure URL
                        reqString);

                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        IWC.RetriveOtherServersRegions(replyData);
                    }
                    else
                    {
                        m_log.Warn("[IWC]: Unable to connect successfully to " + connection.ConnectionPath);
                        return false;
                    }
                }
                else
                {
                    m_log.Warn("[IWC]: Unable to connect successfully to " + connection.ConnectionPath);
                    return false;
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[IWCOutgoingConnector]: Exception when contacting server: {0}", e.Message);
                return false;
            }
            return false;
        }

        internal void SendIncomingAgent(ConnectionIdentifier connection, UUID userID, UUID regionID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["METHOD"] = "sendincomingagent";
            sendData["USERID"] = userID;
            sendData["REGIONID"] = regionID;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                SynchronousRestFormsRequester.MakeRequest("POST",
                        connection.ConnectionPath + connection.ForeignURL, //The full secure URL
                        reqString);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[IWCOutgoingConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        internal UserAccount GetUserAccount(ConnectionIdentifier connection, UUID userID)
        {
            Dictionary<string, object> sendData = new Dictionary<string,object>();

            sendData["METHOD"] = "getuseraccount";
            sendData["USERID"] = userID;

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        connection.ConnectionPath + connection.ForeignURL, //The full secure URL
                        reqString);

                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        UserAccount account = new UserAccount(replyData);
                        return account;
                    }
                    else
                    {
                        m_log.Warn("[IWC]: Unable to connect successfully to " + connection.ConnectionPath);
                    }
                }
                else
                {
                    m_log.Warn("[IWC]: Unable to connect successfully to " + connection.ConnectionPath);
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[IWCOutgoingConnector]: Exception when contacting server: {0}", e.Message);
            }
            return null;
        }
    }

    #region Connector classes

    public class ConnectionIdentifier
    {
        public string ConnectionPath;
        public TrustLevel TrustLevel;
        public string SessionHash;
        public string ForeignPassword;
        public string ForeignURL;

        public ConnectionIdentifier()
        {
        }

        public ConnectionIdentifier(Dictionary<string, object> KVP)
        {
            ConnectionPath = KVP["ConnectionPath"].ToString();
            SessionHash = KVP["SessionHash"].ToString();
            ForeignPassword = KVP["ForeignPassword"].ToString();
            ForeignURL = "";
            TrustLevel = 0;
        }

        public Dictionary<string, object> ToKVP()
        {
            Dictionary<string, object> KVP = new Dictionary<string, object>();
            KVP["ConnectionPath"] = ConnectionPath;
            KVP["SessionHash"] = SessionHash;
            KVP["ForeignPassword"] = ForeignPassword;
            return KVP;
        }

        public override string ToString()
        {
            return "ConnectionPath: " + ConnectionPath +
                ", ForeignPassword: " + ForeignPassword +
                ", TrustLevel: " + TrustLevel.ToString() +
                ", ForeignURL: " + ForeignURL;
        }
    }

    public enum TrustLevel : int
    {
        Full = 4, // No restrictions on data transfers.
        High = 3, // Save regions in the database.
        Medium = 2, // Save agent information.
        Low = 1 // Only showing on the map.
    }

    #endregion
}
