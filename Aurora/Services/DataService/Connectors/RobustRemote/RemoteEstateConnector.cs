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
    public class RemoteEstateConnector : IEstateConnector
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private string m_ServerURI = "";

        public void Initialize(IGenericData unneeded, ISimulationBase simBase, string defaultConnectionString)
        {
            IConfigSource source = simBase.ConfigSource;
            if (source.Configs["AuroraConnectors"].GetString("EstateConnector", "LocalConnector") == "RemoteConnector")
            {
                m_ServerURI = simBase.ApplicationRegistry.Get<IAutoConfigurationService>().FindValueOf("RemoteServerURI", "AuroraData");
                if(m_ServerURI != "")
                    DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IEstateConnector"; }
        }

        public void Dispose()
        {
        }

        public EstateSettings LoadEstateSettings(UUID regionID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            EstateSettings ES = new EstateSettings();
            ES.OnSave += SaveEstateSettings;

            //This DOES have a reason, the RemoteEstateService will not send back
            //  the EstatePass anywhere (for security reasons),
            //  so we need to save it so that we can restore it later.
            string Password = ES.EstatePass;

            sendData["REGIONID"] = regionID.ToString();
            sendData["METHOD"] = "loadestatesettings";

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
                        ES = new EstateSettings(replyData);
                        ES.OnSave += SaveEstateSettings;
                        ES.EstatePass = Password; //Restore it here, see above for explaination
                        return ES;
                    }

                    else
                        m_log.DebugFormat("[AuroraRemoteEstateConnector]: LoadEstateSettings {0} received null response",
                            regionID);
                    return new EstateSettings();
                }
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[AuroraRemoteEstateConnector]: Exception when contacting server: {0}", e.Message);
            }

            return null;
        }

        public EstateSettings LoadEstateSettings(int estateID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            EstateSettings ES = new EstateSettings();
            ES.OnSave += SaveEstateSettings;
            
            sendData["ESTATEID"] = estateID;
            sendData["METHOD"] = "loadestatesettings";

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
                        ES = new EstateSettings(replyData);
                        ES.OnSave += SaveEstateSettings;
                        return ES;
                    }

                    else
                        m_log.DebugFormat("[AuroraRemoteEstateConnector]: LoadEstateSettings {0} received null response",
                            estateID);

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteEstateConnector]: Exception when contacting server: {0}", e.Message);
            }

            return null;
        }

        public void SaveEstateSettings(EstateSettings es)
        {
            Dictionary<string, object> sendData = es.ToKeyValuePairs(true);

            sendData["METHOD"] = "saveestatesettings";

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteEstateConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public EstateSettings CreateEstate(EstateSettings es, UUID RegionID)
        {
            Dictionary<string, object> sendData = es.ToKeyValuePairs(true);

            sendData["REGIONID"] = RegionID.ToString();
            sendData["METHOD"] = "createestate";

            string reqString = WebUtils.BuildXmlResponse(sendData);

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
                        es = new EstateSettings(replyData);
                        es.OnSave += SaveEstateSettings;
                        return es;
                    }

                    else
                        m_log.DebugFormat("[AuroraRemoteEstateConnector]: CreateEstate {0} received null response",
                            RegionID);

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteEstateConnector]: Exception when contacting server: {0}", e.Message);
            }
            return null;
        }

        public List<int> GetEstates(string search)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["SEARCH"] = search;
            sendData["METHOD"] = "getestates";
            List<int> Estates = new List<int>();
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
                            return Estates;
                        foreach (object obj in replyData.Values)
                        {
                            if (obj is Dictionary<string, object>)
                            {
                                Dictionary<string, object> dictionary = obj as Dictionary<string, object>;
                                foreach (object value in dictionary)
                                {
                                    KeyValuePair<string, object> valuevalue = (KeyValuePair<string, object>)value;
                                    Estates.Add(int.Parse(valuevalue.Value.ToString()));
                                }
                            }
                        }
                        return Estates;
                    }

                    else
                        m_log.DebugFormat("[AuroraRemoteEstateConnector]: GetEstates {0} received null response",
                            search);
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteEstateConnector]: Exception when contacting server: {0}", e.Message);
            }

            return null;
        }

        public bool LinkRegion(UUID regionID, int estateID, string password)
        {
            Dictionary<string, object> sendData = new Dictionary<string,object>();
            sendData["REGIONID"] = regionID;
            sendData["ESTATEID"] = estateID;
            sendData["PASSWORD"] = password;
            sendData["METHOD"] = "linkregionestate";

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
                        if (!replyData.ContainsKey("Result") || (replyData["Result"].ToString().ToLower() == "failure"))
                            return false;

                        return true;
                    }

                    else
                        m_log.DebugFormat("[AuroraRemoteEstateConnector]: LinkRegion {0} received null response",
                            regionID);

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteEstateConnector]: Exception when contacting server: {0}", e.Message);
            }

            return false;
        }

        public List<UUID> GetRegions(int estateID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["ESTATEID"] = estateID;
            sendData["METHOD"] = "getregioninestate";
            List<UUID> Regions = new List<UUID>();
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
                            return Regions;
                        foreach (object obj in replyData.Values)
                        {
                            if (obj is Dictionary<string, object>)
                            {
                                Dictionary<string, object> dictionary = obj as Dictionary<string, object>;
                                foreach (object value in dictionary)
                                {
                                    Regions.Add(UUID.Parse(value.ToString()));
                                }
                            }
                        }
                        return Regions;
                    }

                    else
                        m_log.DebugFormat("[AuroraRemoteEstateConnector]: GetEstates {0} received null response",
                            estateID);
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteEstateConnector]: Exception when contacting server: {0}", e.Message);
            }

            return Regions;
        }

        public bool DeleteEstate(int estateID, string password)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            sendData["ESTATEID"] = estateID;
            sendData["PASSWORD"] = password;
            sendData["METHOD"] = "deleteestate";

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
                        if (!replyData.ContainsKey("Result") || (replyData["Result"].ToString().ToLower() == "null"))
                            return false;

                        return true;
                    }

                    else
                        m_log.DebugFormat("[AuroraRemoteEstateConnector]: DeleteEstate {0} received null response",
                            estateID);

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteEstateConnector]: Exception when contacting server: {0}", e.Message);
            }

            return false;
        }
    }
}
