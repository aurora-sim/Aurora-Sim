using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using log4net;
using Nini.Config;
using OpenSim.Server.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Server.Handlers.Base;

using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Nwc.XmlRpc;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.DataManager.Frontends;
using Aurora.Services;
 
namespace OpenSim.Server.Handlers.AuroraData
{
    public class AuroraDataServiceConnector : ServiceConnector
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Aurora.DataManager.Frontends.ProfileFrontend ProfileFrontend = null;

        public AuroraDataServiceConnector(IConfigSource config, IHttpServer server, string configName) :
                base(config, server, configName)
        {
            m_log.Debug("[AuroraDataConnectors]: Starting...");

            Aurora.Services.DataService.LocalDataService LDS = new Aurora.Services.DataService.LocalDataService();
            LDS.Initialise(config);
            ProfileFrontend = new Aurora.DataManager.Frontends.ProfileFrontend(true, Aurora.Framework.Utils.GetAddress());
            server.AddXmlRPCHandler("aurora_data", HandleAuroraData, false);
        }

        public XmlRpcResponse HandleAuroraData(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            Hashtable requestData = (Hashtable)request.Params[0];

            Hashtable Hash = new Hashtable();
            if (requestData != null)
            {
                if (requestData.ContainsKey("Type"))
                {
                    string type = (string)requestData["Type"];
                    switch (type)
                    {
                        case "GetProfile":
                            IUserProfileInfo UPI = ProfileFrontend.GetUserProfile(new UUID((string)requestData["TargetUser"]));
                            //OSDMap profile = UPI.Pack();
                            //Hash["profile"] = new ArrayList(new string[]{profile.ToString()});
                            break;
                    }
                }
            }
            XmlRpcResponse Response = new XmlRpcResponse();
            Hash["success"] = "true";

            return Response;
        }
    }
}
