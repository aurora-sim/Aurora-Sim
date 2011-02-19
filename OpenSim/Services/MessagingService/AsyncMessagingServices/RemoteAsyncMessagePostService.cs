using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using Aurora.Framework;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using Nini.Config;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.MessagingService
{
    /// <summary>
    /// This class deals with putting async messages into the regions 'queues' and sending them to them
    ///   when they request them. This is used for Aurora.Server
    /// </summary>
    public class RemoteAsyncMessagePostService : IService, IAsyncMessagePostService
    {
        protected IRegistryCore m_registry;
        protected IAsyncMessageRecievedService m_asyncReceiverService;

        protected Dictionary<ulong, List<OSDMap>> m_regionMessages = new Dictionary<ulong, List<OSDMap>>();

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public string Name
        {
            get { return GetType().Name; }
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AsyncMessagePostServiceHandler", "") != Name)
                return;

            m_registry = registry;
            registry.RegisterModuleInterface<IAsyncMessagePostService>(this);
            m_asyncReceiverService = registry.RequestModuleInterface<IAsyncMessageRecievedService>();

            //Read any messages received to see whether they are for the async service
            m_asyncReceiverService.OnMessageReceived += OnMessageReceived;
        }

        public void FinishedStartup()
        {
        }

        protected OSDMap OnMessageReceived(OSDMap message)
        {
            //If it is an async message request, make sure that the request is valid and check it
            if (message["Method"] == "AsyncMessageRequest")
            {
                OSDMap response = new OSDMap();
                OSDArray array = new OSDArray();
                if (m_regionMessages.ContainsKey(message["RegionHandle"].AsULong()))
                {
                    foreach (OSDMap asyncMess in m_regionMessages[message["RegionHandle"].AsULong()])
                    {
                        array.Add(asyncMess);
                    }
                }
                response["Messages"] = array;
                return response;
            }
            return null;
        }

        /// <summary>
        /// Post a new message to the given region by region handle
        /// </summary>
        /// <param name="RegionHandle"></param>
        /// <param name="request"></param>
        public void Post(ulong RegionHandle, OSDMap request)
        {
            if (!m_regionMessages.ContainsKey(RegionHandle))
                m_regionMessages.Add(RegionHandle, new List<OSDMap>());

            m_regionMessages[RegionHandle].Add(request);
        }
    }
}
