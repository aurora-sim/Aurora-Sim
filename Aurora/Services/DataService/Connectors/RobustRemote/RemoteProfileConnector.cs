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
using System.Reflection;
using Aurora.Framework;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Services.Interfaces;

namespace Aurora.Services.DataService
{
    public class RemoteProfileConnector : IProfileConnector
    {
        private IRegistryCore m_registry;

        #region IProfileConnector Members

        public void Initialize(IGenericData unneeded, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            m_registry = simBase;
            if (source.Configs["AuroraConnectors"].GetString("ProfileConnector", "LocalConnector") == "RemoteConnector")
            {
                DataManager.DataManager.RegisterPlugin(this);
            }
        }

        public string Name
        {
            get { return "IProfileConnector"; }
        }

        public IUserProfileInfo GetUserProfile(UUID PrincipalID)
        {
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(PrincipalID.ToString(),
                                                                                           "RemoteServerURI");
                foreach (string url in serverURIs)
                {
                    OSDMap map = new OSDMap();
                    map["Method"] = "getprofile";
                    map["PrincipalID"] = PrincipalID;
                    OSDMap response = WebUtils.PostToService(url + "osd", map, true, true);
                    if (response["_Result"].Type == OSDType.Map)
                    {
                        OSDMap responsemap = (OSDMap) response["_Result"];
                        if (responsemap.Count == 0)
                            continue;
                        IUserProfileInfo info = new IUserProfileInfo();
                        info.FromOSD(responsemap);
                        return info;
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e);
            }

            return null;
        }

        public bool UpdateUserProfile(IUserProfileInfo Profile)
        {
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        Profile.PrincipalID.ToString(), "RemoteServerURI");
                foreach (string url in serverURIs)
                {
                    OSDMap map = new OSDMap();
                    map["Method"] = "updateprofile";
                    map["Profile"] = Profile.ToOSD();
                    WebUtils.PostToService(url + "osd", map, false, false);
                }
                return true;
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e);
            }
            return false;
        }

        public void CreateNewProfile(UUID PrincipalID)
        {
            //No user creation from sims
        }

        public bool AddClassified(Classified classified)
        {
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        classified.CreatorUUID.ToString(), "RemoteServerURI");
                foreach (string url in serverURIs)
                {
                    OSDMap map = new OSDMap();
                    map["Method"] = "addclassified";
                    map["Classified"] = classified.ToOSD();
                    WebUtils.PostToService(url + "osd", map, false, false);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e);
            }
            return true;
        }

        public Classified GetClassified(UUID queryClassifiedID)
        {
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                foreach (string url in serverURIs)
                {
                    OSDMap map = new OSDMap();
                    map["Method"] = "getclassified";
                    map["ClassifiedUUID"] = queryClassifiedID;
                    OSDMap response = WebUtils.PostToService(url + "osd", map, true, true);
                    if (response["_Result"].Type == OSDType.Map)
                    {
                        OSDMap responsemap = (OSDMap) response["_Result"];
                        Classified info = new Classified();
                        info.FromOSD(responsemap);
                        return info;
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e);
            }
            return null;
        }

        public List<Classified> GetClassifieds(UUID ownerID)
        {
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(ownerID.ToString(),
                                                                                           "RemoteServerURI");
                foreach (string url in serverURIs)
                {
                    OSDMap map = new OSDMap();
                    map["Method"] = "getclassifieds";
                    map["PrincipalID"] = ownerID;
                    OSDMap response = WebUtils.PostToService(url + "osd", map, true, true);
                    if (response["_Result"].Type == OSDType.Map)
                    {
                        OSDMap responsemap = (OSDMap) response["_Result"];
                        if (responsemap.ContainsKey("Result"))
                        {
                            List<Classified> list = new List<Classified>();
                            OSDArray picks = (OSDArray) responsemap["Result"];
                            foreach (OSD o in picks)
                            {
                                Classified info = new Classified();
                                info.FromOSD((OSDMap) o);
                                list.Add(info);
                            }
                            return list;
                        }
                        return new List<Classified>();
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e);
            }
            return null;
        }

        public void RemoveClassified(UUID queryClassifiedID)
        {
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                foreach (string url in serverURIs)
                {
                    OSDMap map = new OSDMap();
                    map["Method"] = "removeclassified";
                    map["ClassifiedUUID"] = queryClassifiedID;
                    WebUtils.PostToService(url + "osd", map, false, false);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e);
            }
        }

        public bool AddPick(ProfilePickInfo pick)
        {
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(pick.CreatorUUID.ToString(),
                                                                                           "RemoteServerURI");
                foreach (string url in serverURIs)
                {
                    OSDMap map = new OSDMap();
                    map["Method"] = "addpick";
                    map["Pick"] = pick.ToOSD();
                    WebUtils.PostToService(url + "osd", map, false, false);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e);
            }
            return true;
        }

        public ProfilePickInfo GetPick(UUID queryPickID)
        {
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                foreach (string url in serverURIs)
                {
                    OSDMap map = new OSDMap();
                    map["Method"] = "getpick";
                    map["PickUUID"] = queryPickID;
                    OSDMap response = WebUtils.PostToService(url + "osd", map, true, true);
                    if (response["_Result"].Type == OSDType.Map)
                    {
                        OSDMap responsemap = (OSDMap) response["_Result"];
                        ProfilePickInfo info = new ProfilePickInfo();
                        info.FromOSD(responsemap);
                        return info;
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e);
            }
            return null;
        }

        public List<ProfilePickInfo> GetPicks(UUID ownerID)
        {
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(ownerID.ToString(),
                                                                                           "RemoteServerURI");
                foreach (string url in serverURIs)
                {
                    OSDMap map = new OSDMap();
                    map["Method"] = "getpicks";
                    map["PrincipalID"] = ownerID;
                    OSDMap response = WebUtils.PostToService(url + "osd", map, true, true);
                    if (response["_Result"].Type == OSDType.Map)
                    {
                        OSDMap responsemap = (OSDMap) response["_Result"];
                        if (responsemap.ContainsKey("Result"))
                        {
                            List<ProfilePickInfo> list = new List<ProfilePickInfo>();
                            OSDArray picks = (OSDArray) responsemap["Result"];
                            foreach (OSD o in picks)
                            {
                                ProfilePickInfo info = new ProfilePickInfo();
                                info.FromOSD((OSDMap) o);
                                list.Add(info);
                            }
                            return list;
                        }
                        return new List<ProfilePickInfo>();
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e);
            }
            return null;
        }

        public void RemovePick(UUID queryPickID)
        {
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                foreach (string url in serverURIs)
                {
                    OSDMap map = new OSDMap();
                    map["Method"] = "removepick";
                    map["PickUUID"] = queryPickID;
                    WebUtils.PostToService(url + "osd", map, false, false);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e);
            }
        }

        #endregion

        public void Dispose()
        {
        }
    }
}