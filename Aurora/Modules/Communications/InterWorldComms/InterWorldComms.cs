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
    public class ConnectionIdentifier
    {
        public string Connection;
        public TrustLevel TrustLevel;
        public string SessionHash;
        public string ForeignPassword;

        public override string ToString()
        {
            return "Connection: " + Connection +
                ", ForeignPassword: " + ForeignPassword +
                ", TrustLevel: " + TrustLevel.ToString();
        }
    }

    public class Connector
    {
        public string SessionHash;
        public string Password;

        public Connector() { }
        public Connector(Dictionary<string, object> KVP)
        {
            Password = KVP["Password"].ToString();
            SessionHash = KVP["SessionHash"].ToString();
        }

        public Connector(ConnectionIdentifier connection)
        {
            Password = connection.ForeignPassword;
            SessionHash = connection.SessionHash;
        }

        public Dictionary<string, object> ToKVP()
        {
            Dictionary<string, object> KVP = new Dictionary<string, object>();
            KVP["Password"] = Password;
            KVP["SessionHash"] = SessionHash;
            return KVP;
        }
    }

    public enum TrustLevel : int
    {
        Full = 4, // No restrictions on data transfers.
        High = 3, // Save regions in the database.
        Medium = 2, // Save agent information.
        Low = 1 // Only showing on the map.
    }

    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule")]
    public class InterWorldComms : ISharedRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public string OurPassword = "";
        private IConfig m_config;
        private List<Scene> m_scenes = new List<Scene>();
        private bool m_Enabled = true;
        private ConnectionIdentifier[] Connections;
        public IWCOutgoingConnections OutgoingComms;
        private SimMapConnector SimMapConnector;

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

        public bool IsSharedModule{ get { return true; } }

        public string Name { get { return "InterWorldCommunications"; } }

        public void Close() { }

        public void PostInitialise()
        {
        }

        public void AddRegion(Scene scene)
        {
            m_scenes.Add(scene);
        }

        public void RemoveRegion(Scene scene)
        {

        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_Enabled)
                return;

            SimMapConnector = new SimMapConnector(scene.GridService);
            MainServer.Instance.AddStreamHandler(new IWCIncomingConnections(this));
            MainServer.Instance.AddStreamHandler(new IWCForeignAgentsConnector(this));

            OutgoingComms = new IWCOutgoingConnections(m_scenes, this);
            //Make our connection strings.
            Connections = BuildConnections();

            ContactOtherServers();
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        private void ContactOtherServers()
        {
            Action<ConnectionIdentifier> action = new Action<ConnectionIdentifier>(
                delegate(ConnectionIdentifier connection)
                {
                    OutgoingComms.AskOtherServerForConnection(connection);
                }
                );

            Parallel.ForEach(Connections, action);
        }

        #region Helpers

        public void RetriveOtherServersRegions(Dictionary<string, object> replyData)
        {
            //Retrive their sims...
            List<OpenSim.Services.Interfaces.GridRegion> Sims = new List<OpenSim.Services.Interfaces.GridRegion>();
            foreach (object f in replyData)
            {
                if (f is KeyValuePair<string, object>)
                {
                    Dictionary<string, object> value = ((KeyValuePair<string, object>)f).Value as Dictionary<string, object>;
                    OpenSim.Services.Interfaces.GridRegion map = new OpenSim.Services.Interfaces.GridRegion(value);
                    Sims.Add(map);
                }
            }
            foreach (OpenSim.Services.Interfaces.GridRegion region in Sims)
            {
                string result;
                SimMapConnector.TryAddSimMap(region, out result);
                m_scenes[0].GridService.RegisterRegion(UUID.Zero, region);
            }
        }

        public Dictionary<string, object> BuildOurLocalRegions()
        {
            Dictionary<string, object> Regions = new Dictionary<string, object>();
            foreach (Scene scene in m_scenes)
            {
                OpenSim.Services.Interfaces.GridRegion region = scene.GridService.GetRegionByUUID(UUID.Zero, scene.RegionInfo.RegionID);
                Regions.Add(scene.RegionInfo.RegionID.ToString(), region.ToKeyValuePairs());
            }
            return Regions;
        }

        private ConnectionIdentifier[] BuildConnections()
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
                ident.Connection = tempConn[a];
                ident.ForeignPassword = tempIdent[a];
                ident.TrustLevel = (TrustLevel)int.Parse(tempTrustLev[a]);
                ident.SessionHash = Guid.NewGuid().ToString();
                ConnectingTo.Add(ident);
                a++;
            }
            return ConnectingTo.ToArray();
        }

        #endregion
    }

    public class IWCOutgoingConnections
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private List<Scene> m_Scenes = new List<Scene>();
        private InterWorldComms IWC;
        
        public IWCOutgoingConnections(List<Scene> Scenes, InterWorldComms iwc)
        {
            IWC = iwc;
            m_Scenes = Scenes;
        }

        /// <summary>
        /// This contacts the foreign server and sends all the SimMaps of the local regions 
        /// to the foreign server. This will either get back a refused connection, or a 
        /// successful connection with a List of SimMap's that are that server's local regions
        /// </summary>
        /// <param name="connection">Foreign server to connect to</param>
        public void AskOtherServerForConnection(ConnectionIdentifier connection)
        {
            Connector connector = new Connector(connection);

            Dictionary<string, object> sendData = IWC.BuildOurLocalRegions();

            sendData["Password"] = connector.Password;
            sendData["SessionHash"] = connector.SessionHash;
            
            sendData["METHOD"] = "newconnection";

            string reqString = ServerUtils.BuildQueryString(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        connection.Connection + "/IWCConnection",
                        reqString);

                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        if (!replyData.ContainsKey("result"))
                        {
                            m_log.Warn("[IWC]: Unable to connect successfully to " + connection.Connection);
                            return;
                        }

                        if (replyData["result"] == "Refused")
                        {
                            m_log.Warn("[IWC]: Unable to connect successfully to " + connection.Connection + ", the connection was refused.");
                            return;
                        }

                        if (replyData["result"] == "WrongPassword")
                        {
                            m_log.Warn("[IWC]: Unable to connect successfully to " + connection.Connection + ", the foreign password was incorrect.");
                            return;
                        }

                        IWC.RetriveOtherServersRegions(replyData);

                        // Success
                    }

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[IWCOutgoingConnector]: Exception when contacting server: {0}", e.Message);
            }
        }
    }

    public class IWCIncomingConnections : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private InterWorldComms IWC;

        public IWCIncomingConnections(InterWorldComms iwc) :
            base("POST", "/IWCConnection")
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
                        ServerUtils.ParseQueryString(body);

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
            Connector connector = new Connector(request);

            Dictionary<string, object> result = new Dictionary<string, object>();
            
            if (connector.Password != IWC.OurPassword)
            {
                result["result"] = "WrongPassword";
            }

            IWC.RetriveOtherServersRegions(request);

            result["result"] = "Successful";
            result = IWC.BuildOurLocalRegions();

            return Return(result);
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

    public class IWCForeignAgentsConnector : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private InterWorldComms IWC;

        public IWCForeignAgentsConnector(InterWorldComms iwc) :
            base("POST", "/IWCAgents")
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
                        ServerUtils.ParseQueryString(body);

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
            Connector connector = new Connector(request);

            Dictionary<string, object> result = new Dictionary<string, object>();

            if (connector.Password != IWC.OurPassword)
            {
                result["result"] = "WrongPassword";
            }

            IWC.RetriveOtherServersRegions(request);

            result["result"] = "Successful";
            result = IWC.BuildOurLocalRegions();

            return Return(result);
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
}
