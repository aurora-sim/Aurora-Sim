/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aurora.Framework;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using log4net;

namespace Aurora.Services.DataService
{
    public class RemoteEstateConnector : IEstateConnector
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private IRegistryCore m_registry;

        #region IEstateConnector Members

        public void Initialize(IGenericData unneeded, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("EstateConnector", "LocalConnector") == "RemoteConnector")
            {
                m_registry = simBase;
                DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IEstateConnector"; }
        }

        public bool LoadEstateSettings(UUID regionID, out EstateSettings settings)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            settings = null;

            //This DOES have a reason, the RemoteEstateService will not send back
            //  the EstatePass anywhere (for security reasons),
            //  so we need to save it so that we can restore it later.
            EstateSettings ES = new EstateSettings();
            string Password = ES.EstatePass;

            sendData["REGIONID"] = regionID.ToString();
            sendData["METHOD"] = "loadestatesettings";

            string reqString = WebUtils.BuildQueryString(sendData);

            try
            {
                IConfigurationService configService = m_registry.RequestModuleInterface<IConfigurationService>();
                List<string> m_ServerURIs = configService.FindValueOf("RemoteServerURI");
                foreach (Dictionary<string, object> replyData in from m_ServerURI in m_ServerURIs select SynchronousRestFormsRequester.MakeRequest("POST",
                                                                                                                                   m_ServerURI,
                                                                                                                                   reqString) into reply where reply != string.Empty select WebUtils.ParseXmlResponse(reply))
                {
                    if (replyData != null &&
                        (!replyData.ContainsKey("Result") || replyData["Result"].ToString() != "Failure"))
                    {
                        settings = new EstateSettings(replyData);
                        settings.OnSave += SaveEstateSettings;
                        settings.EstatePass = Password; //Restore it here, see above for explaination
                        return true;
                    }
                    else if (replyData.ContainsKey("Result") && replyData["Result"].ToString() == "Failure")
                    {
                        return true;
                    }
                    else
                        m_log.DebugFormat(
                            "[AuroraRemoteEstateConnector]: LoadEstateSettings {0} received null response",
                            regionID);
                    return false;
                }
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[AuroraRemoteEstateConnector]: Exception when contacting server: {0}", e);
            }

            return false;
        }

        public void SaveEstateSettings(EstateSettings es)
        {
            Dictionary<string, object> sendData = es.ToKeyValuePairs(true);

            sendData["METHOD"] = "saveestatesettings";

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    AsynchronousRestObjectRequester.MakeRequest("POST",
                                                                m_ServerURI,
                                                                reqString);
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteEstateConnector]: Exception when contacting server: {0}", e);
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
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                foreach (Dictionary<string, object> replyData in from m_ServerURI in m_ServerURIs select SynchronousRestFormsRequester.MakeRequest("POST",
                                                                                                                                   m_ServerURI,
                                                                                                                                   reqString) into reply where reply != string.Empty select WebUtils.ParseXmlResponse(reply))
                {
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
                m_log.DebugFormat("[AuroraRemoteEstateConnector]: Exception when contacting server: {0}", e);
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
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                foreach (Dictionary<string, object> replyData in from m_ServerURI in m_ServerURIs select SynchronousRestFormsRequester.MakeRequest("POST",
                                                                                                                                   m_ServerURI,
                                                                                                                                   reqString) into reply where reply != string.Empty select WebUtils.ParseXmlResponse(reply))
                {
                    if (replyData != null)
                    {
                        if (!replyData.ContainsKey("result"))
                            return Estates;
                        Estates.AddRange(from obj in replyData.Values.OfType<Dictionary<string, object>>() from object value in obj as Dictionary<string, object> select (KeyValuePair<string, object>) value into valuevalue select int.Parse(valuevalue.Value.ToString()));
                        return Estates;
                    }

                    else
                        m_log.DebugFormat("[AuroraRemoteEstateConnector]: GetEstates {0} received null response",
                                          search);
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteEstateConnector]: Exception when contacting server: {0}", e);
            }

            return null;
        }

        public List<UUID> GetRegions(uint estateID)
        {
            //Regions arn't allowed to find other regions in their estate.
            return null;
        }

        public List<EstateSettings> GetEstates(UUID owner)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["SEARCH"] = owner;
            sendData["METHOD"] = "getestatesowner";
            List<EstateSettings> Estates = new List<EstateSettings>();
            string reqString = WebUtils.BuildQueryString(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                foreach (Dictionary<string, object> replyData in from m_ServerURI in m_ServerURIs select SynchronousRestFormsRequester.MakeRequest("POST",
                                                                                                                                   m_ServerURI,
                                                                                                                                   reqString) into reply where reply != string.Empty select WebUtils.ParseXmlResponse(reply))
                {
                    if (replyData != null)
                    {
                        if (!replyData.ContainsKey("result"))
                            return Estates;
                        Estates.AddRange(from obj in replyData.Values.OfType<Dictionary<string, object>>() select obj as Dictionary<string, object> into dictionary from objobj in dictionary.Values select new EstateSettings(objobj as Dictionary<string, object>));
                        return Estates;
                    }

                    else
                        m_log.DebugFormat("[AuroraRemoteEstateConnector]: GetEstates {0} received null response",
                                          owner);
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteEstateConnector]: Exception when contacting server: {0}", e);
            }

            return null;
        }

        public bool LinkRegion(UUID regionID, int estateID, string password)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            sendData["REGIONID"] = regionID;
            sendData["ESTATEID"] = estateID;
            sendData["PASSWORD"] = password;
            sendData["METHOD"] = "linkregionestate";

            string reqString = WebUtils.BuildQueryString(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                foreach (Dictionary<string, object> replyData in from m_ServerURI in m_ServerURIs select SynchronousRestFormsRequester.MakeRequest("POST",
                                                                                                                                   m_ServerURI,
                                                                                                                                   reqString) into reply where reply != string.Empty select WebUtils.ParseXmlResponse(reply))
                {
                    if (replyData != null)
                    {
                        if (!replyData.ContainsKey("Result") ||
                            (replyData["Result"].ToString().ToLower() == "failure"))
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
                m_log.DebugFormat("[AuroraRemoteEstateConnector]: Exception when contacting server: {0}", e);
            }

            return false;
        }

        public bool DelinkRegion(UUID regionID, string password)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            sendData["REGIONID"] = regionID;
            sendData["PASSWORD"] = password;
            sendData["METHOD"] = "delinkregionestate";

            string reqString = WebUtils.BuildQueryString(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                foreach (Dictionary<string, object> replyData in from m_ServerURI in m_ServerURIs select SynchronousRestFormsRequester.MakeRequest("POST",
                                                                                                                                   m_ServerURI,
                                                                                                                                   reqString) into reply where reply != string.Empty select WebUtils.ParseXmlResponse(reply))
                {
                    if (replyData != null)
                    {
                        if (!replyData.ContainsKey("Result") ||
                            (replyData["Result"].ToString().ToLower() == "failure"))
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
                m_log.DebugFormat("[AuroraRemoteEstateConnector]: Exception when contacting server: {0}", e);
            }

            return false;
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
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                foreach (Dictionary<string, object> replyData in from m_ServerURI in m_ServerURIs select SynchronousRestFormsRequester.MakeRequest("POST",
                                                                                                                                   m_ServerURI,
                                                                                                                                   reqString) into reply where reply != string.Empty select WebUtils.ParseXmlResponse(reply))
                {
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
                m_log.DebugFormat("[AuroraRemoteEstateConnector]: Exception when contacting server: {0}", e);
            }

            return false;
        }

        #endregion

        public void Dispose()
        {
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
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                foreach (Dictionary<string, object> replyData in from m_ServerURI in m_ServerURIs select SynchronousRestFormsRequester.MakeRequest("POST",
                                                                                                                                   m_ServerURI,
                                                                                                                                   reqString) into reply where reply != string.Empty select WebUtils.ParseXmlResponse(reply))
                {
                    if (replyData != null)
                    {
                        if (!replyData.ContainsKey("result"))
                            return Regions;
                        Regions.AddRange(from obj in replyData.Values.OfType<Dictionary<string, object>>() from object value in obj as Dictionary<string, object> select UUID.Parse(value.ToString()));
                        return Regions;
                    }

                    else
                        m_log.DebugFormat("[AuroraRemoteEstateConnector]: GetEstates {0} received null response",
                                          estateID);
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteEstateConnector]: Exception when contacting server: {0}", e);
            }

            return Regions;
        }
    }
}