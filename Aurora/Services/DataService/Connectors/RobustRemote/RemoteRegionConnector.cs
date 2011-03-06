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
    public class RemoteRegionConnector : IRegionConnector
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);
        private IRegistryCore m_registry;

        public void Initialize(IGenericData unneeded, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("RegionConnector", "LocalConnector") == "RemoteConnector")
            {
                m_registry = simBase;
                DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IRegionConnector"; }
        }

        public void Dispose()
        {
        }

        #region IGridConnector Members

        public void AddTelehub(Telehub telehub)
        {
            Dictionary<string, object> sendData = telehub.ToKeyValuePairs();
            sendData["METHOD"] = "addtelehub";

            string reqString = WebUtils.BuildQueryString(sendData);

            try
            {
                List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridServerURI");
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    AsynchronousRestObjectRequester.MakeRequest("POST",
                        m_ServerURI,
                        reqString);
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteRegionConnector]: Exception when contacting server: {0}", e.ToString());
            }
        }

        public void RemoveTelehub(UUID regionID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            sendData["METHOD"] = "removetelehub";

            string reqString = WebUtils.BuildQueryString(sendData);

            try
            {
                List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridServerURI");
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    AsynchronousRestObjectRequester.MakeRequest("POST",
                        m_ServerURI,
                        reqString);
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteRegionConnector]: Exception when contacting server: {0}", e.ToString());
            }
        }

        public Telehub FindTelehub(UUID regionID)
        {
            Dictionary<string, object> sendData = new Dictionary<string,object>();

            sendData["METHOD"] = "findtelehub";
            sendData["REGIONID"] = regionID.ToString();

            string reqString = WebUtils.BuildQueryString(sendData);

            try
            {
                List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridServerURI");
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                           m_ServerURI,
                           reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if (replyData != null)
                        {
                            if (replyData.Count != 0)
                            {
                                Telehub t = new Telehub();
                                t.FromKVP(replyData);
                                return t;
                            }
                        }
                        else
                        {
                            m_log.DebugFormat("[AuroraRemoteRegionConnector]: RemoveTelehub {0} received null response",
                                regionID.ToString());
                        }
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteRegionConnector]: Exception when contacting server: {0}", e.ToString());
            }
            return null;
        }

        #endregion
    }
}
