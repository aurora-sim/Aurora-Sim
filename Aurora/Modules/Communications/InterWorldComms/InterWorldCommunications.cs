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
using Aurora.Simulation.Base;
using log4net;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using DataManager = Aurora.DataManager.DataManager;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace Aurora.Modules
{
    public class InterWorldCommunications : ISharedRegionStartupModule
    {
        #region Declares

        protected static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        /// <summary>
        /// The 'AuroraInterWorldConnectors' config 
        /// </summary>
        protected IConfig m_config;
        /// <summary>
        /// The registry where we can get services
        /// </summary>
        protected IRegistryCore m_registry;
        /// <summary>
        /// Whether we are enabled or not
        /// </summary>
        protected bool m_Enabled = false;
        /// <summary>
        /// All connections that we have to other hosts
        /// (Before sending the initial connection requests, 
        ///   this MAY contain connections that we do not currently have)
        /// </summary>
        protected List<Connection> Connections;
        /// <summary>
        /// The class that sends requests to other hosts
        /// </summary>
        public IWCOutgoingConnections OutgoingPublicComms;

        #endregion

        /// <summary>
        /// Send an initial request to get secure Urls from any and all connections we have
        /// </summary>
        private void ContactOtherServers()
        {
            List<Connection> NewConnections = new List<Connection>();
            foreach (Connection connection in Connections)
            {
                Connection newConn = OutgoingPublicComms.AskOtherServerForConnection(connection);
                if (newConn != null)
                    NewConnections.Add(newConn);
            }
            //Fix the list with the newly updated ones
            Connections = NewConnections;
        }

        /// <summary>
        /// Query the database for any connections that we have stored
        /// </summary>
        /// <returns></returns>
        private List<Connection> BuildConnections()
        {
            List<Connection> connections = new List<Connection>();
            //Ask the database for the connectors
            IGenericsConnector genericsConnector = DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            if (genericsConnector != null)
                connections = genericsConnector.GetGenerics<Connection>(UUID.Zero, "InterWorldConnections", new Connection());
            return connections;
        }

        /// <summary>
        /// Check the given incoming connection and make sure that we have a session for them
        ///   as well as a correct password.
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public bool VerifyIncomingConnection(Connection connection)
        {
            Connection verifiedConnection = FindConnectionBySessionHash(connection);
            if (verifiedConnection == null)
                //We don't have a connection to them with this session hash, lock them out!
                return false;
            
            //Now check that they have the correct password
            if(verifiedConnection.Password != connection.Password)
                return false;

            //They have the right password and we have a session for them... let them in
            return true;
        }

        /// <summary>
        /// This connection has been verified, add it to the database
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public bool UpdateIncomingConnection(Connection connection)
        {
            IGenericsConnector genericsConnector = DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            if (genericsConnector != null)
            {
                genericsConnector.AddGeneric(UUID.Zero, "InterWorldConnections", connection.SessionHash, connection.ToOSD());
                return true;
            }
            else
                return false;
        }

        private Connection FindConnectionBySessionHash(Connection connection)
        {
            foreach (Connection c in Connections)
            {
                if (c.SessionHash == connection.SessionHash)//This is the connection we are looking for
                {
                    return c;
                }
            }
            //No connection found
            return null;
        }

        #region ISharedRegionStartupModule Members

        public void Initialise(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
            m_config = source.Configs["AuroraInterWorldConnectors"];
            if (m_config != null)
                m_Enabled = m_config.GetBoolean("Enabled", false);

            m_registry = openSimBase.ApplicationRegistry;

            if (m_Enabled)
                scene.RegisterModuleInterface<InterWorldCommunications>(this);
        }

        public void PostInitialise(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
        }

        public void FinishStartup(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
        }

        public void Close(Scene scene)
        {
        }

        public void StartupComplete()
        {
            if (!m_Enabled)
                return;

            //Set up the public connection
            MainServer.Instance.AddStreamHandler(new IWCIncomingConnections(this));

            //Startup outgoing
            OutgoingPublicComms = new IWCOutgoingConnections(this);

            //Make our connection strings.
            Connections = BuildConnections();

            ContactOtherServers();
        }

        #endregion
    }

    /// <summary>
    /// This class deals with sending requests to other hosts
    /// </summary>
    public class IWCOutgoingConnections
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private InterWorldCommunications IWC;

        public IWCOutgoingConnections(InterWorldCommunications iwc)
        {
            IWC = iwc;
        }

        /// <summary>
        /// This contacts the foreign server and sends all the SimMaps of the local regions 
        /// to the foreign server. This will either get back a refused connection, or a 
        /// successful connection with a List of SimMap's that are that server's local regions
        /// </summary>
        /// <param name="connection">Foreign server to connect to</param>
        public Connection AskOtherServerForConnection(Connection connector)
        {
            OSDMap request = connector.ToOSD();
            request["Method"] = "Query";
            OSDMap reply = WebUtils.PostToService(connector.URL, request);
            if (reply["Success"].AsBoolean())
            {
                if (reply["_Result"].Type != OSDType.Map)
                {
                    m_log.Warn("[IWC]: Unable to connect successfully to " + connector.URL + ", connection did not have all the required data.");
                    return null;
                }
                OSDMap innerReply = (OSDMap)reply["_Result"];
                if (innerReply["Result"].AsString() == "Successful")
                {
                    Connection c = new Connection();
                    c.FromOSD(innerReply);
                    m_log.Error("[IWC]: Connected successfully to " + connector.URL);
                    return c;
                }
                m_log.Warn("[IWC]: Unable to connect successfully to " + connector.URL + ", " + innerReply["Result"]);
            }
            else
            {
                m_log.Warn("[IWC]: Unable to connect successfully to " + connector.URL);
            }
            return null;
        }
    }

    /// <summary>
    /// This class deals with incoming requests (secure and insecure) from other hosts
    /// </summary>
    public class IWCIncomingConnections : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private InterWorldCommunications IWC;

        public IWCIncomingConnections(InterWorldCommunications iwc) :
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

            OSDMap args = WebUtils.GetOSDMap(body);
            if (args == null)
            {
                //No data or not an json OSDMap
                return FailureResult();
            }
            else
            {
                if (!args.ContainsKey("Method"))
                {
                    string Method = args["Method"].AsString();
                    if (Method == "Query")
                        return Query(args);
                }
            }
            return FailureResult();
        }

        /// <summary>
        /// This is the initial request to join this host
        /// We need to verify passwords and add sessionHashes to our database
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private byte[] Query(OSDMap request)
        {
            Connection connector = new Connection();
            //Pull the connection info out of the request
            connector.FromOSD(request);

            //Lets make sure that they are allowed to connect to us
            if (!IWC.VerifyIncomingConnection(connector))
                return FailureResult();

            //Update them in the database so that they can connect again later
            if (!IWC.UpdateIncomingConnection(connector))
                return FailureResult();

            BuildSecureUrlsForConnection(connector);

            OSDMap result = connector.ToOSD();
            result["Result"] = "Successful";

            return Return(result);
        }

        /// <summary>
        /// Create secure Urls that only us and the sim that called us know of
        /// This Urls is used to add/remove agents and other information from the other sim
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private Connection BuildSecureUrlsForConnection(Connection c)
        {
            //TODO:
            return c;
        }

        #region Misc

        private byte[] FailureResult()
        {
            OSDMap result = new OSDMap();
            result["Result"] = "Failure";
            return Return(result);
        }

        private byte[] Return(OSDMap result)
        {
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            return Util.UTF8.GetBytes(OSDParser.SerializeJsonString(result));
        }

        #endregion
    }

    #region Connector classes

    /// <summary>
    /// The base Connection class
    /// This deals with saving info about other hosts so that we can contact them
    /// </summary>
    public class Connection : IDataTransferable
    {
        /// <summary>
        /// Our TrustLevel of the host (target)
        /// </summary>
        public TrustLevel TrustLevel;
        /// <summary>
        /// Our Session hash that identifies us (host)
        /// </summary>
        public string SessionHash;
        /// <summary>
        /// Our Password that is used to authenticate us (host)
        /// </summary>
        public string Password;
        /// <summary>
        /// The (base, unsecure) Url of the host (target) we are connecting to
        /// </summary>
        public string URL;
        /// <summary>
        /// Secure Urls that the host (target) has given us to be able to contact it
        /// </summary>
        public OSDMap SecureUrls = new OSDMap();

        public override void FromOSD(OSDMap map)
        {
            TrustLevel = (TrustLevel)map["TrustLevel"].AsInteger();
            SessionHash = map["SessionHash"].AsString();
            Password = map["Password"].AsString();
            URL = map["URL"].AsString();
            SecureUrls = (OSDMap)map["SecureUrls"];
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map.Add("TrustLevel", (int)TrustLevel);
            map.Add("SessionHash", SessionHash);
            map.Add("Password", Password);
            map.Add("URL", URL);
            map.Add("SecureUrls", SecureUrls);
            return map;
        }

        public override Dictionary<string, object> ToKeyValuePairs()
        {
            return Util.OSDToDictionary(ToOSD());
        }

        public override void FromKVP(Dictionary<string, object> KVP)
        {
            FromOSD(Util.DictionaryToOSD(KVP));
        }

        public override IDataTransferable Duplicate()
        {
            Connection m = new Connection();
            m.FromOSD(ToOSD());
            return m;
        }
    }

    /// <summary>
    /// The trust level enum
    /// Tells how much we trust another host
    /// </summary>
    public enum TrustLevel : int
    {
        Full = 4,
        High = 3,
        Medium = 2,
        Low = 1
    }

    #endregion
}
