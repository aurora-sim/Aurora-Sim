using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using log4net;
using System.IO;
using System.Reflection;
using Nini.Config;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using Aurora.Simulation.Base;

namespace Aurora.Services.DataService
{
    public class RemoteOfflineMessagesConnector : IOfflineMessagesConnector
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private IRegistryCore m_registry;

        public void Initialize(IGenericData unneeded, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            m_registry = simBase;
            if (source.Configs["AuroraConnectors"].GetString ("OfflineMessagesConnector", "LocalConnector") == "RemoteConnector")
            {
                DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IOfflineMessagesConnector"; }
        }

        public void Dispose()
        {
        }

        #region IOfflineMessagesConnector Members

        public GridInstantMessage[] GetOfflineMessages(UUID PrincipalID)
        {
            OSDMap map = new OSDMap ();

            map["PrincipalID"] = PrincipalID;
            map["Method"] = "getofflinemessages";

            List<GridInstantMessage> Messages = new List<GridInstantMessage>();
            try
            {
                List<string> urls = m_registry.RequestModuleInterface<IConfigurationService> ().FindValueOf (PrincipalID.ToString (), "RemoteServerURI");
                foreach (string url in urls)
                {
                    OSDMap result = WebUtils.PostToService (url + "osd", map, true, false);
                    OSDArray array = (OSDArray)OSDParser.DeserializeJson (result["_RawResult"]);
                    foreach (OSD o in array)
                    {
                        GridInstantMessage message = new GridInstantMessage ();
                        message.FromOSD ((OSDMap)o);
                        Messages.Add (message);
                    }
                }
                return Messages.ToArray();
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteOfflineMessagesConnector]: Exception when contacting server: {0}", e.ToString());
            }
            return Messages.ToArray();
        }

        public bool AddOfflineMessage (GridInstantMessage message)
        {
            OSDMap sendData = message.ToOSD();

            sendData["Method"] = "addofflinemessage";

            try
            {
                List<string> urls = m_registry.RequestModuleInterface<IConfigurationService> ().FindValueOf (message.toAgentID.ToString (), "RemoteServerURI");
                foreach (string url in urls)
                {
                    OSDMap result = WebUtils.PostToService (url + "osd", sendData, true, false);
                    return ((OSDMap)OSDParser.DeserializeJson (result["_RawResult"]))["Result"].AsBoolean();
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteOfflineMessagesConnector]: Exception when contacting server: {0}", e.ToString());
            }
            return false;
        }

        #endregion
    }
}
