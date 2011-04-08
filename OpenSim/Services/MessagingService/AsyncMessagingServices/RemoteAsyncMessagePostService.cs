using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using Aurora.Framework;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using Nini.Config;
using OpenMetaverse;
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

        protected Dictionary<ulong, OSDArray> m_regionMessages = new Dictionary<ulong, OSDArray>();

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public string Name
        {
            get { return GetType().Name; }
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AsyncMessagePostServiceHandler", "") != Name)
                return;

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
                try
                {
                    IGridService service = m_registry.RequestModuleInterface<IGridService>();
                    OSDMap response = new OSDMap();
                    OSDMap mapresponse = new OSDMap();

                    if (message.ContainsKey("RegionHandles"))
                    {
                        OSDArray handles = (OSDArray)message["RegionHandles"];

                        for (int i = 0; i < handles.Count; i += 2)
                        {
                            ulong regionHandle = handles[i].AsULong();
                            int x, y;
                            Util.UlongToInts(regionHandle, out x, out y);
                            bool verified = service.VerifyRegionSessionID(service.GetRegionByPosition(UUID.Zero, x, y), handles[i+1].AsUUID());
                            if (verified)
                            {
                                if (m_regionMessages.ContainsKey(regionHandle))
                                {
                                    //Get the array, then remove it
                                    OSDArray array = m_regionMessages[regionHandle];
                                    m_regionMessages.Remove(regionHandle);
                                    mapresponse[regionHandle.ToString()] = array;
                                }
                            }
                        }
                    }

                    response["Messages"] = mapresponse;
                    return response;
                }
                catch
                {
                }
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
                m_regionMessages.Add(RegionHandle, new OSDArray());

            m_regionMessages[RegionHandle].Add(request);
        }
    }
}
