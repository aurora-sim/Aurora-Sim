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
using Aurora.DataManager;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace Aurora.Modules
{
    public class InterWorldCommunications : ISharedRegionStartupModule
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IConfig m_config;
        private IRegistryCore m_registry;
        private bool m_Enabled = false;
        private List<Connection> Connections;
        public IWCOutgoingConnections OutgoingPublicComms;

        #endregion

        public string Name { get { return "InterWorldCommunications"; } }

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

        private List<Connection> BuildConnections()
        {
            //We can't get the connections from the database yet
            return new List<Connection>();
        }

        #region ISharedRegionStartupModule Members

        public void Initialise(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
            m_config = source.Configs["AuroraInterWorldConnectors"];
            if (m_config != null)
                m_Enabled = m_config.GetBoolean("Enabled", false);

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

    #region Public connectors

    /// <summary>
    /// This deals with making a secure connection with another instance.
    /// It calls IWCIncomingPublicConnections on the foreign server.
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
            OSDMap reply = WebUtils.PostToService(connector.URL, connector.ToOSD());
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
    /// This deals with connections that want to become secure with the current instance.
    /// This handler is not secure and is public and is called by IWCOutgoingPublicConnections on the foreign server.
    /// This handler sets up IWCIncomingPrivateConnection, which is secured and protected.
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
            Connection connector = new Connection();
            return Return(null);
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

    #endregion

    #region Connector classes

    public class Connection : IDataTransferable
    {
        /// <summary>
        /// Our TrustLevel of the host
        /// </summary>
        public TrustLevel TrustLevel;
        /// <summary>
        /// Our Session hash that identifies us
        /// </summary>
        public string SessionHash;
        /// <summary>
        /// The password of the host that this will connect to
        /// </summary>
        public string Password;
        /// <summary>
        /// The (base, unsecure) Url of the host we are connecting to
        /// </summary>
        public string URL;
        /// <summary>
        /// Secure Urls that the host has given us to be able to contact it
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

    public enum TrustLevel : int
    {
        Full = 4,
        High = 3,
        Medium = 2,
        Low = 1
    }

    #endregion
}
