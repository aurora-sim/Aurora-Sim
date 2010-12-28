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
    public class RemoteAssetConnector : IAssetConnector
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);
        private string m_ServerURI = "";

        public void Initialize(IGenericData unneeded, ISimulationBase simBase, string defaultConnectionString)
        {
            IConfigSource source = simBase.ConfigSource;
            if (source.Configs["AuroraConnectors"].GetString("AssetConnector", "LocalConnector") == "RemoteConnector")
            {
                //Later thought... we really do need just the AuroraServerURI, since it won't work with the normal grid server most of the time
                //We want the grid server URL, but if we can't find it, resort to trying the default Aurora one
                /*if (source.Configs["GridService"].GetString("GridServerURI", string.Empty) != string.Empty)
                    m_ServerURI = source.Configs["GridService"].GetString("GridServerURI", string.Empty);
                else */
                m_ServerURI = simBase.ApplicationRegistry.RequestModuleInterface<IAutoConfigurationService>().FindValueOf("RemoteServerURI", "AuroraData");
                
                //If both are blank, no connector
                if (m_ServerURI != string.Empty)
                    DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IAssetConnector"; }
        }

        public void Dispose()
        {
        }

        #region IAssetConnector Members

        public void UpdateLSLData(string token, string key, string value)
        {
            Dictionary<string, object> sendData = new Dictionary<string,object>();

            sendData["token"] = token;
            sendData["key"] = key;
            sendData["value"] = value;
            sendData["METHOD"] = "updatelsldata";

            string reqString = WebUtils.BuildQueryString(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteAssetConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public List<string> FindLSLData(string token, string key)
        {
            List<string> data = new List<string>();
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["token"] = token;
            sendData["key"] = key;
            sendData["METHOD"] = "findlsldata";

            string reqString = WebUtils.BuildQueryString(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);

                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        if (!replyData.ContainsKey("result"))
                            return data;
                        foreach (object obj in replyData.Values)
                        {
                            if (obj is Dictionary<string, object>)
                            {
                                Dictionary<string, object> dictionary = obj as Dictionary<string, object>;
                                foreach (object value in dictionary)
                                {
                                    KeyValuePair<string, object> valuevalue = (KeyValuePair<string, object>)value;
                                    data.Add(valuevalue.Value.ToString());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteAssetConnector]: Exception when contacting server: {0}", e.Message);
            }
            return data;
        }

        #endregion
    }
}
