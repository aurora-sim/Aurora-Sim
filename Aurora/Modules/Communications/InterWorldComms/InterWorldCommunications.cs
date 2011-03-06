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
    public class InterWorldCommunications : IService, ICommunicationService
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
        public ThreatLevel m_untrustedConnectionsDefaultTrust = ThreatLevel.Low;
        /// <summary>
        /// All connections that we have to other hosts
        /// (Before sending the initial connection requests, 
        ///   this MAY contain connections that we do not currently have)
        /// </summary>
        protected List<IWCCertificate> Connections;
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
            List<IWCCertificate> NewConnections = new List<IWCCertificate>(Connections);
            foreach (IWCCertificate connection in NewConnections)
            {
                TryAddConnection(connection);
            }
        }

        /// <summary>
        /// Query the database for any connections that we have stored
        /// </summary>
        /// <returns></returns>
        private List<IWCCertificate> BuildConnections()
        {
            List<IWCCertificate> connections = new List<IWCCertificate>();
            //Ask the database for the connectors
            IGenericsConnector genericsConnector = DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            if (genericsConnector != null)
                connections = genericsConnector.GetGenerics<IWCCertificate>(UUID.Zero, "InterWorldConnections", new IWCCertificate());
            return connections;
        }

        private void AddConnection(IWCCertificate c)
        {
            if (!Connections.Contains(c))
            {
                Connections.Add(c);
                IGenericsConnector genericsConnector = DataManager.DataManager.RequestPlugin<IGenericsConnector>();
                if (genericsConnector != null)
                    genericsConnector.AddGeneric(UUID.Zero, "InterWorldConnections", c.URL, c.ToOSD());
            }
        }

        private void RemoveConnection(IWCCertificate c)
        {
            if (Connections.Contains(c))
            {
                Connections.Remove(c);
                IGenericsConnector genericsConnector = DataManager.DataManager.RequestPlugin<IGenericsConnector>();
                if (genericsConnector != null)
                    genericsConnector.RemoveGeneric(UUID.Zero, "InterWorldConnections", c.URL);
            }
        }

        public void TryAddConnection(IWCCertificate c)
        {
            IWCCertificate cert = OutgoingPublicComms.QueryRemoteHost(c);
            if (cert != null)
            {
                c = cert;
                IConfigurationService configService = m_registry.RequestModuleInterface<IConfigurationService>();
                //Give the Urls to the config service
                configService.AddNewUrls(cert.UserName, cert.SecureUrls);
                AddConnection(c);
                c.Active = true;
            }
            else
                c.Active = false;
        }

        #endregion

        #region Find Connections

        public IWCCertificate FindConnectionByUserName(string username)
        {
            foreach (IWCCertificate c in Connections)
            {
                if (c.UserName == username)//This is the connection we are looking for
                {
                    return c;
                }
            }
            //No connection found
            return null;
        }

        private IWCCertificate FindConnectionByURL(string url)
        {
            foreach (IWCCertificate c in Connections)
            {
                if (c.URL == url)//This is the connection we are looking for
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
            MainConsole.Instance.Commands.AddCommand("IWC", true, "iwc add connection", "iwc add connection",
                "Add an IWC connection to another host.", AddIWCConnection);
            MainConsole.Instance.Commands.AddCommand("IWC", true, "iwc remove connection", "iwc remove connection",
                "Remove an IWC connection from another host.", RemoveIWCConnection);
            MainConsole.Instance.Commands.AddCommand("IWC", true, "iwc show connections", "iwc show connections",
                "Shows all active IWC connections.", ShowIWCConnections);
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
            //Be user friendly, add the http:// if needed as well as the final /
            Url = (Url.StartsWith("http://") || Url.StartsWith("https://")) ? Url : "http://" + Url;
            Url = Url.EndsWith("/") ? Url + "iwcconnection" : Url + "/iwcconnection";

            IWCCertificate con = FindConnectionByURL(Url);
            if(con != null)
            {
                if (con.Active)
                {
                    m_log.Warn("A connection to this server already exists.");
                }
                else
                {
                    string activate = MainConsole.Instance.CmdPrompt("A connection to this server already exists, do you wish to active it?");
                    if (activate == "yes" || activate == "true")
                    {
                        TryAddConnection(con);
                    }
                }
                return;
            }
            con = new IWCCertificate();
            con.URL = Url;
            string timeUntilExpires = MainConsole.Instance.CmdPrompt("Time until the connection expires (ends, in days)");
            string trustLevel = MainConsole.Instance.CmdPrompt("Trust level of this connection");
            int timeInDays = int.Parse(timeUntilExpires);
            string UserName = MainConsole.Instance.CmdPrompt("User Name for this connection (can be blank)");
            string Password = MainConsole.Instance.CmdPrompt("Password for this connection");
            
            //Build the certificate
            if (UserName == "")
                UserName = UUID.Random().ToString();
            con.UserName = UserName;
            con.Password = Password;
            con.ValidUntil = DateTime.Now.AddDays(timeInDays);

            //Add the certificate now
            CertificateVerification.AddCertificate(con);
            con.ThreatLevel = (ThreatLevel)Enum.Parse(typeof(ThreatLevel), trustLevel);

            TryAddConnection(con);
        }

        private void RemoveIWCConnection(string module, string[] cmds)
        {
            string Url = MainConsole.Instance.CmdPrompt("Url to the connection");
            IWCCertificate c = FindConnectionByURL(Url);
            if (c == null)
            {
                m_log.Warn("Could not find the connection.");
                return;
            }
            OutgoingPublicComms.DeleteRemoteHost(c);
            RemoveConnection(c);
            IConfigurationService configService = m_registry.RequestModuleInterface<IConfigurationService>();
            //Remove the Urls from the config service
            configService.RemoveUrls(c.UserName);
        }

        private void ShowIWCConnections(string module, string[] cmds)
        {
            m_log.InfoFormat("Showing {0} active IWC connections.", Connections.Count);
            for (int i = 0; i < Connections.Count; i++)
            {
                m_log.Info("Url: " + Connections[i].URL);
                m_log.Info("User Name: " + Connections[i].UserName);
                m_log.Info("Password: " + Connections[i].Password);
                m_log.Info("Trust Level: " + Connections[i].ThreatLevel);
                m_log.Info("Valid Until: " + Connections[i].ValidUntil);
                m_log.Info("Active: " + Connections[i].Active);
                m_log.Info("-------------");
            }
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
                m_untrustedConnectionsDefaultTrust = (ThreatLevel)Enum.Parse(typeof(ThreatLevel), m_config.GetString("UntrustedConnectionsDefaultTrust", m_untrustedConnectionsDefaultTrust.ToString()));
                registry.RegisterModuleInterface<ICommunicationService>(this);
                registry.RegisterModuleInterface<InterWorldCommunications>(this);
                m_registry = registry;
            }
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

        #region ICommunicationService

        public ThreatLevel GetThreatLevelForUrl(string URL)
        {
            IWCCertificate cert = CertificateVerification.GetCertificateByUrl(URL);
            if (cert != null)
                return cert.ThreatLevel;
            return m_untrustedConnectionsDefaultTrust;
        }

        public GridRegion GetRegionForGrid(string regionName, string Url)
        {
            IWCCertificate c = FindConnectionByURL(Url);
            if (c != null)
            {
                //If we are already connected, the grid services are together, so we already know of the region if it exists, therefore, it does not exist
                return null;
            }
            else
            {
                c = new IWCCertificate();

                //Build the certificate
                c.ValidUntil = DateTime.Now.AddDays(1); //One day for now...

                c.ThreatLevel = m_untrustedConnectionsDefaultTrust; //Least amount of our trust for them
                //Be user friendly, add the http:// if needed as well as the final /
                Url = (Url.StartsWith("http://") || Url.StartsWith("https://")) ? Url : "http://" + Url;
                Url = Url.EndsWith("/") ? Url + "iwcconnection" : Url + "/iwcconnection";
                c.URL = Url;
                c.UserName = UUID.Random().ToString();

                //Add the certificate now
                CertificateVerification.AddCertificate(c);

                TryAddConnection(c);
                IGridService gridService = m_registry.RequestModuleInterface<IGridService>();
                if (gridService != null)
                {
                    List<GridRegion> regions = gridService.GetRegionsByName(UUID.Zero, regionName, 1);
                    if (regions != null && regions.Count > 0)
                        return regions[0];
                }
            }
            return null;
        }

        public OSDMap GetUrlsForUser(GridRegion region, UUID userID)
        {
            IGridRegistrationService r = Registry.RequestModuleInterface<IGridRegistrationService>();
            return r.GetUrlForRegisteringClient(userID.ToString(), region.RegionHandle);
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
        public IWCCertificate QueryRemoteHost(IWCCertificate connection)
        {
            OSDMap request = connection.ToOSD(false);
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

        public void DeleteRemoteHost(IWCCertificate connection)
        {
            OSDMap request = connection.ToOSD(false);
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
                if (CertificateVerification.GetCertificateByUserName(Certificate.UserName) != null)
                {
                    //SPOOF! XXXXXX
                    return FailureResult();
                }
                //This is an untrusted connection otherwise
                if (!IWC.m_allowUntrustedConnections)
                    return FailureResult(); //We don't allow them

                //Give them the default untrusted connection level
                Certificate.ThreatLevel = IWC.m_untrustedConnectionsDefaultTrust;
            }

            //Update them in the database so that they can connect again later
            CertificateVerification.AddCertificate(Certificate);

            IWCCertificate ourConnectionToThem = IWC.FindConnectionByUserName(Certificate.UserName);
            if (ourConnectionToThem != null)
            {
                //Verify that our connection is ok with them as well
                //Tenitively set to active so that we don't get a loop
                ourConnectionToThem.Active = true;
                Util.FireAndForget(QueryOtherHost, ourConnectionToThem);
            }

            BuildSecureUrlsForConnection(Certificate);

            OSDMap result = Certificate.ToOSD(false);
            result["Result"] = "Successful";

            m_log.WarnFormat("[IWC]: {0} successfully connected to us.", Certificate.URL);

            return Return(result);
        }

        private void QueryOtherHost(object o)
        {
            IWCCertificate ourConnectionToThem = (IWCCertificate)o;
            IWC.TryAddConnection(ourConnectionToThem);
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
            IGridRegistrationService gridRegistration = IWC.Registry.RequestModuleInterface<IGridRegistrationService>();
            if (gridRegistration != null)
            {
                //Give the basic Urls that we have
                c.SecureUrls = gridRegistration.GetUrlForRegisteringClient(c.UserName, 0);
            }
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
    /// The base Certificate class
    /// This deals with saving info about other hosts so that we can contact them
    /// </summary>
    public class IWCCertificate : IDataTransferable
    {
        protected DateTime m_validUntil;
        protected OSDMap m_SecureUrls = new OSDMap();
        /// <summary>
        /// The UserName of the connecting instance
        /// </summary>
        public string UserName;
        /// <summary>
        /// The Password of the connecting instance
        /// </summary>
        public string Password;
        /// <summary>
        /// Our TrustLevel of the host (target)
        /// </summary>
        public ThreatLevel ThreatLevel;
        /// <summary>
        /// The (base, unsecure) Url of the host (target) we are connecting to
        /// </summary>
        public string URL;
        /// <summary>
        /// Whether this connection is currently active and able to be used
        /// </summary>
        public bool Active = true;

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

        public override void FromOSD(OSDMap map)
        {
            ValidUntil = map["ValidUntil"].AsDate();
            SecureUrls = (OSDMap)map["SecureUrls"];
            if (map.ContainsKey("ThreatLevel"))
                ThreatLevel = (ThreatLevel)map["ThreatLevel"].AsInteger();
            UserName = map["UserName"].AsString();
            Password = map["Password"].AsString();
            URL = map["URL"].AsString();
        }

        public OSDMap ToOSD(bool Secure)
        {
            OSDMap map = new OSDMap();
            map.Add("ValidUntil", ValidUntil);
            map.Add("SecureUrls", SecureUrls);
            map.Add("UserName", UserName);
            map.Add("Password", Password);
            if(Secure)
                map.Add("ThreatLevel", (int)ThreatLevel);
            map.Add("URL", URL);
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
            m_certificates[cert.UserName] = cert;
        }

        /// <summary>
        /// Check to make sure this IWC Certificate is valid
        /// </summary>
        /// <param name="cert"></param>
        /// <returns></returns>
        public static bool VerifyCertificate(IWCCertificate cert)
        {
            //Make sure we have the certificate
            if (m_certificates.ContainsKey(cert.UserName))
            {
                if (m_certificates[cert.UserName].Password == cert.Password)
                {
                    //Now verify that it hasn't expired yet
                    if (DateTime.Now < m_certificates[cert.UserName].ValidUntil)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Remove a certificate
        /// </summary>
        /// <param name="cert"></param>
        public static void RemoveCertificate(IWCCertificate cert)
        {
            m_certificates.Remove(cert.UserName);
        }

        public static IWCCertificate GetCertificateByUserName(string userName)
        {
            if (m_certificates.ContainsKey(userName))
                return m_certificates[userName];
            return null;
        }

        public static IWCCertificate GetCertificateByUrl(string url)
        {
            foreach (IWCCertificate cert in m_certificates.Values)
            {
                if (cert.URL == url)
                    return cert;
            }
            return null;
        }
    }
}
