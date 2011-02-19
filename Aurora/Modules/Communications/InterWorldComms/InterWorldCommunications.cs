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
using OpenSim.Framework.Console;
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
    public class InterWorldCommunications : IService
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
        /// Should connections that come to us that are not authenticated be allowed to connect?
        /// </summary>
        public bool m_allowUntrustedConnections = false;
        /// <summary>
        /// Untrusted connections automatically get this trust level
        /// </summary>
        public TrustLevel m_untrustedConnectionsDefaultTrust = TrustLevel.Low;
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

        public IRegistryCore Registry
        {
            get { return m_registry; }
        }

        #endregion

        #region Private members

        #region Contact and build connections

        /// <summary>
        /// Send an initial request to get secure Urls from any and all connections we have
        /// </summary>
        private void ContactOtherServers()
        {
            List<Connection> NewConnections = new List<Connection>();
            foreach (Connection connection in Connections)
            {
                IWCCertificate cert = OutgoingPublicComms.QueryRemoteHost(connection);
                if (cert != null)
                {
                    //Add the new certificate to the connection
                    Connection newConnection = (Connection)connection.Duplicate();
                    newConnection.Certificate = cert;
                    NewConnections.Add(newConnection);
                }
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

        #endregion

        #region Find Connections

        private Connection FindConnectionBySessionHash(Connection connection)
        {
            foreach (Connection c in Connections)
            {
                if (c.Certificate.SessionHash == connection.Certificate.SessionHash)//This is the connection we are looking for
                {
                    return c;
                }
            }
            //No connection found
            return null;
        }

        #endregion

        #endregion

        #region Console Commands

        private void AddConsoleCommands()
        {
            MainConsole.Instance.Commands.AddCommand("IWC", true, "add connection", "add connection",
                "Add an IWC connection to another host.", AddIWCConnection);
            MainConsole.Instance.Commands.AddCommand("IWC", true, "remove connection", "remove connection",
                "Remove an IWC connection from another host.", RemoveIWCConnection);
        }

        #region Commands

        /// <summary>
        /// Add a certificate for the given connection
        /// </summary>
        /// <param name="module"></param>
        /// <param name="cmds"></param>
        private void AddIWCConnection(string module, string[] cmds)
        {
            string Url = MainConsole.Instance.CmdPrompt("Url to the connection");
            string timeUntilExpires = MainConsole.Instance.CmdPrompt("Time until the connection expires (ends, in days)");
            string trustLevel = MainConsole.Instance.CmdPrompt("Trust level of this connection");
            int timeInDays = int.Parse(timeUntilExpires);

            Connection con = new Connection();
            
            //Build the certificate
            IWCCertificate cert = new IWCCertificate();
            cert.SessionHash = UUID.Random().ToString();
            cert.ValidUntil = DateTime.Now.AddDays(timeInDays);

            //Add the certificate now
            CertificateVerification.AddCertificate(cert);

            con.Certificate = cert;
            con.TrustLevel = (TrustLevel)Enum.Parse(typeof(TrustLevel), trustLevel);
            //Be user friendly, add the http:// if needed as well as the final /
            Url = (Url.StartsWith("http://") || Url.StartsWith("https://")) ? Url : "http://" + Url;
            Url = Url.EndsWith("/") ? Url + "iwcconnection" : Url + "/iwcconnection";
            con.URL = Url;

            cert = OutgoingPublicComms.QueryRemoteHost(con);
            if (cert != null)
            {
                con.Certificate = cert;
                IConfigurationService configService = m_registry.RequestModuleInterface<IConfigurationService>();
                //Give the Urls to the config service
                configService.AddNewUrls(cert.SessionHash, cert.SecureUrls);
                Connections.Add(con);
                m_log.Warn("Added connection to " + Url + ".");
            }
            else
            {
                m_log.Warn("Could not add connection.");
            }
        }

        private void RemoveIWCConnection(string module, string[] cmds)
        {
            string Url = MainConsole.Instance.CmdPrompt("Url to the connection");
            //TODO:
        }

        #endregion

        #endregion

        #region IService Members

        public void Initialize(IConfigSource source, IRegistryCore registry)
        {
            m_config = source.Configs["AuroraInterWorldConnectors"];
            if (m_config != null)
            {
                m_Enabled = m_config.GetBoolean("Enabled", false);
                m_allowUntrustedConnections = m_config.GetBoolean("AllowUntrustedConnections", m_allowUntrustedConnections);
                m_untrustedConnectionsDefaultTrust = (TrustLevel)Enum.Parse(typeof(TrustLevel), m_config.GetString("UntrustedConnectionsDefaultTrust", m_untrustedConnectionsDefaultTrust.ToString()));
            }

            m_registry = registry;
        }

        public void Start(IConfigSource source, IRegistryCore registry)
        {
            if (m_Enabled)
            {
                registry.RegisterModuleInterface<InterWorldCommunications>(this);
                //Set up the public connection
                MainServer.Instance.AddStreamHandler(new IWCIncomingConnections(this));

                //Startup outgoing
                OutgoingPublicComms = new IWCOutgoingConnections(this);

                //Make our connection strings.
                Connections = BuildConnections();

                ContactOtherServers();

                AddConsoleCommands();
            }
        }

        public void FinishedStartup()
        {
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
        /// Query the given host (by connection) and verify that we can connect to it.
        /// </summary>
        /// <param name="connector">The host to connect to</param>
        /// <returns>The connection that has been recieved from the host</returns>
        public IWCCertificate QueryRemoteHost(Connection connection)
        {
            OSDMap request = connection.Certificate.ToOSD(false);
            request["Method"] = "Query";
            OSDMap reply = WebUtils.PostToService(connection.URL, request);
            if (reply["Success"].AsBoolean())
            {
                if (reply["_Result"].Type != OSDType.Map)
                {
                    m_log.Warn("[IWC]: Unable to connect successfully to " + connection.URL + ", connection did not have all the required data.");
                    return null;
                }
                OSDMap innerReply = (OSDMap)reply["_Result"];
                if (innerReply["Result"].AsString() == "Successful")
                {
                    IWCCertificate c = new IWCCertificate();
                    c.FromOSD(innerReply);
                    m_log.Error("[IWC]: Connected successfully to " + connection.URL);
                    return c;
                }
                m_log.Warn("[IWC]: Unable to connect successfully to " + connection.URL + ", " + innerReply["Result"]);
            }
            else
            {
                m_log.Warn("[IWC]: Unable to connect successfully to " + connection.URL);
            }
            return null;
        }

        public void DeleteRemoteHost(Connection connection)
        {
            OSDMap request = connection.Certificate.ToOSD(false);
            request["Method"] = "Delete";
            OSDMap reply = WebUtils.PostToService(connection.URL, request);
            if (!reply["Success"].AsBoolean())
            {
                m_log.Warn("[IWC]: Failed to delete remote host @ " + connection.URL);
            }
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
                if (args.ContainsKey("Method"))
                {
                    string Method = args["Method"].AsString();
                    if (Method == "Query")
                        return Query(args);
                    else if (Method == "Delete")
                        return Delete(args);
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
            IWCCertificate Certificate = new IWCCertificate();
            //Pull the connection info out of the request
            Certificate.FromOSD(request);

            //Lets make sure that they are allowed to connect to us
            if (!CertificateVerification.VerifyCertificate(Certificate))
            {
                //Make sure the other host is not trying to spoof one of our certificates
                if (CertificateVerification.GetCertificateBySessionHash(Certificate.SessionHash) != null)
                {
                    //SPOOF! XXXXXX
                    return FailureResult();
                }
                //This is an untrusted connection otherwise
                if (!IWC.m_allowUntrustedConnections)
                    return FailureResult(); //We don't allow them

                //Give them the default untrusted connection level
                Certificate.TrustLevel = IWC.m_untrustedConnectionsDefaultTrust;
            }

            //Update them in the database so that they can connect again later
            CertificateVerification.AddCertificate(Certificate);

            BuildSecureUrlsForConnection(Certificate);

            OSDMap result = Certificate.ToOSD(false);
            result["Result"] = "Successful";

            return Return(result);
        }

        /// <summary>
        /// This is a request to remove the remote host from our list of current connections.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private byte[] Delete(OSDMap request)
        {
            IWCCertificate Certificate = new IWCCertificate();
            //Pull the connection info out of the request
            Certificate.FromOSD(request);

            //Make sure that they are verified to connect
            if (!CertificateVerification.VerifyCertificate(Certificate))
                return FailureResult();

            //Remove them from our list of connections
            CertificateVerification.RemoveCertificate(Certificate);

            return SuccessfulResult();
        }

        /// <summary>
        /// Create secure Urls that only us and the sim that called us know of
        /// This Urls is used to add/remove agents and other information from the other sim
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private IWCCertificate BuildSecureUrlsForConnection(IWCCertificate c)
        {
            IConfigurationService service = IWC.Registry.RequestModuleInterface<IConfigurationService>();
            //Give the basic Urls that we have
            c.SecureUrls = service.GetValuesFor("default");
            c.SecureUrls["TeleportAgent"] = "";
            return c;
        }

        #region Misc

        private byte[] FailureResult()
        {
            OSDMap result = new OSDMap();
            result["Result"] = "Failure";
            return Return(result);
        }

        private byte[] SuccessfulResult()
        {
            OSDMap result = new OSDMap();
            result["Result"] = "Successful";
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
    /// This should not be passed to other host, use the IWCCertificate instead
    /// </summary>
    public class Connection : IDataTransferable
    {
        /// <summary>
        /// Our TrustLevel of the host (target)
        /// </summary>
        public TrustLevel TrustLevel;
        /// <summary>
        /// The Certificate (for us) of this connection
        /// </summary>
        public IWCCertificate Certificate;
        /// <summary>
        /// The (base, unsecure) Url of the host (target) we are connecting to
        /// </summary>
        public string URL;

        public override void FromOSD(OSDMap map)
        {
            TrustLevel = (TrustLevel)map["TrustLevel"].AsInteger();
            Certificate = new IWCCertificate();
            Certificate.FromOSD((OSDMap)OSDParser.DeserializeJson(map["Certificate"].AsString()));
            URL = map["URL"].AsString();
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map.Add("TrustLevel", (int)TrustLevel);
            map.Add("Certificate", OSDParser.SerializeJsonString(Certificate.ToOSD(true)));
            map.Add("URL", URL);
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

    public class IWCCertificate : IDataTransferable
    {
        protected DateTime m_validUntil;
        protected string m_SessionHash;
        protected OSDMap m_SecureUrls = new OSDMap();
        protected TrustLevel m_TrustLevel;

        /// <summary>
        /// The SessionID of the certificate
        /// This identifies this connection to the other host
        /// </summary>
        public string SessionHash
        {
            get { return m_SessionHash; }
            set { m_SessionHash = value; }
        }

        /// <summary>
        /// The time the certificate expires
        /// </summary>
        public DateTime ValidUntil
        {
            get { return m_validUntil; }
            set { m_validUntil = value; }
        }

        /// <summary>
        /// Secure Urls that this certificate is valid for
        /// </summary>
        public OSDMap SecureUrls
        {
            get { return m_SecureUrls; }
            set { m_SecureUrls = value; }
        }

        /// <summary>
        /// Our (this instance) trust of this certificate
        /// </summary>
        public TrustLevel TrustLevel
        {
            get { return m_TrustLevel; }
            set { m_TrustLevel = value; }
        }

        public override void FromOSD(OSDMap map)
        {
            SessionHash = map["SessionHash"].AsString();
            ValidUntil = map["ValidUntil"].AsDate();
            SecureUrls = (OSDMap)map["SecureUrls"];
            if(map.ContainsKey("TrustLevel"))
                TrustLevel = (TrustLevel)map["TrustLevel"].AsInteger();
        }

        public OSDMap ToOSD(bool Secure)
        {
            OSDMap map = new OSDMap();
            map.Add("SessionHash", SessionHash);
            map.Add("ValidUntil", ValidUntil);
            map.Add("SecureUrls", SecureUrls);
            map.Add("TrustLevel", (int)TrustLevel);
            return map;
        }

        public override OSDMap ToOSD()
        {
            return ToOSD(true);
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
            IWCCertificate m = new IWCCertificate();
            m.FromOSD(ToOSD());
            return m;
        }
    }

    #endregion

    public class CertificateVerification
    {
        protected static Dictionary<string, IWCCertificate> m_certificates = new Dictionary<string, IWCCertificate>();
        /// <summary>
        /// Add (or update) a certificate
        /// </summary>
        /// <param name="cert"></param>
        public static void AddCertificate(IWCCertificate cert)
        {
            m_certificates[cert.SessionHash] = cert;
        }

        /// <summary>
        /// Check to make sure this IWC Certificate is valid
        /// </summary>
        /// <param name="cert"></param>
        /// <returns></returns>
        public static bool VerifyCertificate(IWCCertificate cert)
        {
            //Make sure we have the certificate
            if (m_certificates.ContainsKey(cert.SessionHash))
            {
                //Now verify that it hasn't expired yet
                if (DateTime.Now < m_certificates[cert.SessionHash].ValidUntil)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Attempt to find a certificate for the given Session Hash
        /// </summary>
        /// <param name="SessionHash"></param>
        /// <returns></returns>
        public static IWCCertificate GetCertificateBySessionHash(string SessionHash)
        {
            IWCCertificate cert = null;
            m_certificates.TryGetValue(SessionHash, out cert);
            return cert;
        }

        /// <summary>
        /// Remove a certificate
        /// </summary>
        /// <param name="cert"></param>
        public static void RemoveCertificate(IWCCertificate cert)
        {
            m_certificates.Remove(cert.SessionHash);
        }
    }
}
