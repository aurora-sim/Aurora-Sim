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
        private string m_ServerURI = "";

        public void Initialize(IGenericData unneeded, ISimulationBase simBase, string defaultConnectionString)
        {
            IConfigSource source = simBase.ConfigSource;
            if (source.Configs["AuroraConnectors"].GetString("RegionConnector", "LocalConnector") == "RemoteConnector")
            {
                //Later thought... we really do need just the AuroraServerURI, since it won't work with the normal grid server most of the time
                //We want the grid server URL, but if we can't find it, resort to trying the default Aurora one
                /*if (source.Configs["GridService"].GetString("GridServerURI", string.Empty) != string.Empty)
                    m_ServerURI = source.Configs["GridService"].GetString("GridServerURI", string.Empty);
                else */
                if (simBase.ApplicationRegistry.RequestModuleInterface<IAutoConfigurationService>().FindValueOf("RemoteRegionServerURI", "AuroraData") != string.Empty)
                    m_ServerURI = simBase.ApplicationRegistry.RequestModuleInterface<IAutoConfigurationService>().FindValueOf("RemoteRegionServerURI", "AuroraData");
                else
                    m_ServerURI = simBase.ApplicationRegistry.RequestModuleInterface<IAutoConfigurationService>().FindValueOf("RemoteServerURI", "AuroraData");
                
                //If both are blank, no connector
                if (m_ServerURI != string.Empty)
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

        public void AddTelehub(Telehub telehub, UUID SessionID)
        {
            Dictionary<string, object> sendData = telehub.ToKeyValuePairs();

            sendData["SESSIONID"] = SessionID.ToString();
            sendData["METHOD"] = "addtelehub";

            string reqString = WebUtils.BuildQueryString(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/grid",
                        reqString);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteRegionConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public void RemoveTelehub(UUID regionID, UUID SessionID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["REGIONID"] = regionID.ToString();
            sendData["SESSIONID"] = SessionID.ToString();
            sendData["METHOD"] = "removetelehub";

            string reqString = WebUtils.BuildQueryString(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/grid",
                        reqString);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteRegionConnector]: Exception when contacting server: {0}", e.Message);
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
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/grid",
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
                return null;
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteRegionConnector]: Exception when contacting server: {0}", e.Message);
            }
            return null;
        }

        #endregion
    }
}
