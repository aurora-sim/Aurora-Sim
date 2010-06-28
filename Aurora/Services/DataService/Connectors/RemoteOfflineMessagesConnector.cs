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
using OpenSim.Server.Base;

namespace Aurora.Services.DataService
{
    public class RemoteOfflineMessagesConnector : IOfflineMessagesConnector, IAuroraDataPlugin
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private string m_ServerURI = "";

        public void Initialise(IGenericData unneeded, IConfigSource source)
        {
            if (source.Configs["AuroraConnectors"].GetString("OfflineMessagesConnector", "LocalConnector") == "RemoteConnector")
            {
                m_ServerURI = source.Configs["AuroraData"].GetString("RemoteServerURI", "");
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

        public OfflineMessage[] GetOfflineMessages(UUID PrincipalID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["PRINCIPALID"] = PrincipalID;
            sendData["METHOD"] = "getofflinemessages";

            string reqString = ServerUtils.BuildQueryString(sendData);
            List<OfflineMessage> Messages = new List<OfflineMessage>();
            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    foreach (object f in replyData)
                    {
                        KeyValuePair<string, object> value = (KeyValuePair<string, object>)f;
                        if (value.Value is Dictionary<string, object>)
                        {
                            Dictionary<string, object> valuevalue = value.Value as Dictionary<string, object>;
                            OfflineMessage message = new OfflineMessage(valuevalue);
                            Messages.Add(message);
                        }
                    }
                }
                return Messages.ToArray();
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteOfflineMessagesConnector]: Exception when contacting server: {0}", e.Message);
            }
            return Messages.ToArray();
        }

        public void AddOfflineMessage(OfflineMessage message)
        {
            Dictionary<string, object> sendData = message.ToKeyValuePairs();

            sendData["METHOD"] = "addofflinemessage";

            string reqString = ServerUtils.BuildQueryString(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteOfflineMessagesConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        #endregion
    }
}
