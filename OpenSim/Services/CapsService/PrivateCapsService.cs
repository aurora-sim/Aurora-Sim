using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using log4net;
using Nini.Config;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using OpenSim.Framework.Capabilities;
using OpenSim.Framework.Servers.HttpServer;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Services.DataService;
using OpenSim.Server.Handlers.Base;

namespace OpenSim.Services.CapsService
{
    /// <summary>
    /// This handles the seed requests from the client and forwards the request onto the the simulator, as well as dealing with individual server based CAPS
    /// </summary>
    public class PrivateCapsService : IPrivateCapsService
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private IGridUserService m_GridUserService;
        private IPresenceService m_PresenceService;
        private IInventoryService m_InventoryService;
        private ILibraryService m_LibraryService;
        private IGridService m_GridService;
        public IGridUserService GridUserService
        {
            get { return m_GridUserService; }
        }
        public IPresenceService PresenceService
        {
            get { return m_PresenceService; }
        }
        public IInventoryService InventoryService
        {
            get { return m_InventoryService; }
        }
        public ILibraryService LibraryService
        {
            get { return m_LibraryService; }
        }
        public IGridService GridService
        {
            get { return m_GridService; }
        }
        private IHttpServer m_server;
        public IHttpServer HttpServer
        {
            get { return m_server; }
            set { m_server = value; }
        }
        private string m_SimToInform;
        public string SimToInform
        {
            get { return m_SimToInform; }
            set { m_SimToInform = value; }
        }
        private UUID m_AgentID;
        public UUID AgentID
        {
            get { return m_AgentID; }
        }
        //X cap name to path
        public OSDMap registeredCAPS = new OSDMap();
        //Paths to X cap
        public OSDMap registeredCAPSPath = new OSDMap();
        private EventQueueService EQMHandler = new EventQueueService();
        public IInternalEventQueueService EventQueueService
        {
            get { return EQMHandler; }
        }
        private string m_HostName;
        public string HostName
        {
            get { return m_HostName; }
            set { m_HostName = value; }
        }
        private OSDMap postToSendToSim = new OSDMap();

        public OSDMap PostToSendToSim
        {
            get { return postToSendToSim; }
            set { postToSendToSim = value; }
        }

        private bool m_runTheEQM;
        private ulong m_regionHandle = 0;
        public ulong RegionHandle
        {
            get { return m_regionHandle; }
        }
        private ICapsService m_publicHandler;
        public ICapsService PublicHandler
        {
            get { return m_publicHandler; }
        }
        private string m_capsURL;
        public string CapsURL
        {
            get { return m_capsURL; }
        }

        public PrivateCapsService(IHttpServer server, IInventoryService inventoryService, ILibraryService libraryService, IGridUserService guService, IGridService gService, IPresenceService presenceService, string URL, UUID agentID, string HostName, bool runTheEQM, ulong regionHandle, ICapsService handler, string capsURL)
        {
            m_server = server;
            m_InventoryService = inventoryService;
            m_LibraryService = libraryService;
            m_GridUserService = guService;
            m_GridService = gService;
            m_PresenceService = presenceService;
            SimToInform = URL;
            m_AgentID = agentID;
            m_HostName = HostName;
            m_runTheEQM = runTheEQM;
            m_regionHandle = regionHandle;
            m_publicHandler = handler;
            m_capsURL = capsURL;
        }

        public void Initialise()
        {
            if (m_server != null)
                AddServerCAPS();
        }

        public List<IRequestHandler> GetServerCAPS()
        {
            List<IRequestHandler> handlers = new List<IRequestHandler>();

            if (m_runTheEQM)
            {
                // The EventQueue module is now handled by the CapsService (if we arn't disabling it) as it needs to be completely protected
                //  This means we deal with all teleports and keeping track of the passwords for the agents
                IRequestHandler handle = EQMHandler.RegisterCap(m_AgentID, m_server, this);
                if (handle != null)
                    handlers.Add(handle);
            }

            foreach (ICapsServiceConnector conn in m_publicHandler.CapsModules)
            {
                handlers.AddRange(conn.RegisterCaps(m_AgentID, m_server, this));
            }

            return handlers;
        }

        private void AddServerCAPS()
        {
            List<IRequestHandler> handlers = GetServerCAPS();
            foreach (IRequestHandler handle in handlers)
            {
                m_server.AddStreamHandler(handle);
            }
        }

        public string CapsRequest(string request, string path, string param,
                                  OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        SimToInform,
                        OSDParser.SerializeLLSDXmlString(postToSendToSim));
                m_log.Debug("[CAPSService]: Seed request was added for region " + SimToInform + " at " + CapsURL);
                if (reply != "")
                {
                    OSDMap hash = (OSDMap)OSDParser.DeserializeLLSDXml(OpenMetaverse.Utils.StringToBytes(reply));
                    foreach (string key in hash.Keys)
                    {
                        if (!registeredCAPS.ContainsKey(key))
                            registeredCAPS[key] = hash[key].AsString();
                        //else
                        //    m_log.WarnFormat("[CAPSService]: Simulator tried to override grid CAPS setting! @ {0}", SimToInform);
                    }
                }
                //m_log.Warn("[CAPS]: EQM Request for " + registeredCAPS["EventQueueGet"].AsString());
            }
            catch
            {
            }
            return OSDParser.SerializeLLSDXmlString(registeredCAPS);
        }

        public string CreateCAPS(string method)
        {
            string caps = "/CAPS/" + method + "/" + UUID.Random() + "/";
            AddCAPS(method, caps);
            return caps;
        }

        public string CreateCAPS(string method, string appendedPath)
        {
            string caps = "/CAPS/" + method + "/" + UUID.Random() + appendedPath + "/";
            AddCAPS(method, caps);
            return caps;
        }

        public void AddCAPS(string method, string caps)
        {
            registeredCAPS[method] = m_HostName + caps;
            registeredCAPSPath[m_HostName + caps] = method;
        }

        public string GetCAPS(string method)
        {
            if (registeredCAPS.ContainsKey(method))
                return registeredCAPS[method].ToString();
            return "";
        }
    }

    #region EQM

    public class CAPSEQMHandler// : ICapsServiceModule
    {
        
    }

    #endregion
}
