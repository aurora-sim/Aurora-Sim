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
    public class RemoteProfileConnector : IProfileConnector
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private IRegistryCore m_registry;

        public void Initialize(IGenericData unneeded, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            m_registry = simBase;
            if (source.Configs["AuroraConnectors"].GetString("ProfileConnector", "LocalConnector") == "RemoteConnector")
            {
                DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IProfileConnector"; }
        }

        public void Dispose()
        {
        }

        #region IProfileConnector Members

        public IUserProfileInfo GetUserProfile (UUID PrincipalID)
        {
            try
            {
                List<string> serverURIs = m_registry.RequestModuleInterface<IConfigurationService> ().FindValueOf (PrincipalID.ToString (), "RemoteServerURI");
                foreach (string url in serverURIs)
                {
                    OSDMap map = new OSDMap();
                    map["Method"] = "getprofile";
                    map["PrincipalID"] = PrincipalID;
                    OSDMap response = WebUtils.PostToService (url + "osd", map);
                    if (response["_Result"].Type == OSDType.Map)
                    {
                        OSDMap responsemap = (OSDMap)response["_Result"];
                        IUserProfileInfo info = new IUserProfileInfo ();
                        info.FromOSD (responsemap);
                        return info;
                    }
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat ("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.ToString ());
            }

            return null;
        }

        public bool UpdateUserProfile(IUserProfileInfo Profile)
        {
            try
            {
                List<string> serverURIs = m_registry.RequestModuleInterface<IConfigurationService> ().FindValueOf (Profile.PrincipalID.ToString (), "RemoteServerURI");
                foreach (string url in serverURIs)
                {
                    OSDMap map = new OSDMap ();
                    map["Method"] = "updateprofile";
                    map["Profile"] = Profile.ToOSD();
                    WebUtils.PostToService (url + "osd", map);
                }
                return true;
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.ToString());
            }
            return false;
        }

        public void CreateNewProfile(UUID PrincipalID)
        {
            //No user creation from sims
        }

        public bool AddClassified (Classified classified)
        {
            try
            {
                List<string> serverURIs = m_registry.RequestModuleInterface<IConfigurationService> ().FindValueOf (classified.CreatorUUID.ToString (), "RemoteServerURI");
                foreach (string url in serverURIs)
                {
                    OSDMap map = new OSDMap ();
                    map["Method"] = "addclassified";
                    map["Classified"] = classified.ToOSD ();
                    WebUtils.PostToService (url + "osd", map);
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat ("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.ToString ());
            }
            return true;
        }

        public Classified GetClassified (UUID queryClassifiedID)
        {
            try
            {
                List<string> serverURIs = m_registry.RequestModuleInterface<IConfigurationService> ().FindValueOf ("RemoteServerURI");
                foreach (string url in serverURIs)
                {
                    OSDMap map = new OSDMap ();
                    map["Method"] = "getclassified";
                    map["ClassifiedUUID"] = queryClassifiedID;
                    OSDMap response = WebUtils.PostToService (url + "osd", map);
                    if (response["_Result"].Type == OSDType.Map)
                    {
                        OSDMap responsemap = (OSDMap)response["_Result"];
                        Classified info = new Classified ();
                        info.FromOSD (responsemap);
                        return info;
                    }
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat ("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.ToString ());
            }
            return null;
        }

        public List<Classified> GetClassifieds (UUID ownerID)
        {
            try
            {
                List<string> serverURIs = m_registry.RequestModuleInterface<IConfigurationService> ().FindValueOf (ownerID.ToString (), "RemoteServerURI");
                foreach (string url in serverURIs)
                {
                    OSDMap map = new OSDMap ();
                    map["Method"] = "getclassifieds";
                    map["PrincipalID"] = ownerID;
                    OSDMap response = WebUtils.PostToService (url + "osd", map);
                    if (response["_Result"].Type == OSDType.Map)
                    {
                        OSDMap responsemap = (OSDMap)response["_Result"];
                        if (responsemap.ContainsKey ("Result"))
                        {
                            List<Classified> list = new List<Classified> ();
                            OSDArray picks = (OSDArray)responsemap["Result"];
                            foreach (OSD o in picks)
                            {
                                Classified info = new Classified ();
                                info.FromOSD ((OSDMap)o);
                                list.Add (info);
                            }
                            return list;
                        }
                        return new List<Classified> ();
                    }
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat ("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.ToString ());
            }
            return null;
        }

        public void RemoveClassified (UUID queryClassifiedID)
        {
            try
            {
                List<string> serverURIs = m_registry.RequestModuleInterface<IConfigurationService> ().FindValueOf ("RemoteServerURI");
                foreach (string url in serverURIs)
                {
                    OSDMap map = new OSDMap ();
                    map["Method"] = "removeclassified";
                    map["ClassifiedUUID"] = queryClassifiedID;
                    WebUtils.PostToService (url + "osd", map);
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat ("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.ToString ());
            }
        }

        public bool AddPick (ProfilePickInfo pick)
        {
            try
            {
                List<string> serverURIs = m_registry.RequestModuleInterface<IConfigurationService> ().FindValueOf (pick.CreatorUUID.ToString (), "RemoteServerURI");
                foreach (string url in serverURIs)
                {
                    OSDMap map = new OSDMap ();
                    map["Method"] = "addpick";
                    map["Pick"] = pick.ToOSD ();
                    WebUtils.PostToService (url + "osd", map);
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat ("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.ToString ());
            }
            return true;
        }

        public ProfilePickInfo GetPick (UUID queryPickID)
        {
            try
            {
                List<string> serverURIs = m_registry.RequestModuleInterface<IConfigurationService> ().FindValueOf ("RemoteServerURI");
                foreach (string url in serverURIs)
                {
                    OSDMap map = new OSDMap ();
                    map["Method"] = "getpick";
                    map["PickUUID"] = queryPickID;
                    OSDMap response = WebUtils.PostToService (url + "osd", map);
                    if (response["_Result"].Type == OSDType.Map)
                    {
                        OSDMap responsemap = (OSDMap)response["_Result"];
                        ProfilePickInfo info = new ProfilePickInfo ();
                        info.FromOSD (responsemap);
                        return info;
                    }
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat ("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.ToString ());
            }
            return null;
        }

        public List<ProfilePickInfo> GetPicks (UUID ownerID)
        {
            try
            {
                List<string> serverURIs = m_registry.RequestModuleInterface<IConfigurationService> ().FindValueOf (ownerID.ToString (), "RemoteServerURI");
                foreach (string url in serverURIs)
                {
                    OSDMap map = new OSDMap ();
                    map["Method"] = "getpicks";
                    map["PrincipalID"] = ownerID;
                    OSDMap response = WebUtils.PostToService (url + "osd", map);
                    if (response["_Result"].Type == OSDType.Map)
                    {
                        OSDMap responsemap = (OSDMap)response["_Result"];
                        if (responsemap.ContainsKey ("Result"))
                        {
                            List<ProfilePickInfo> list = new List<ProfilePickInfo> ();
                            OSDArray picks = (OSDArray)responsemap["Result"];
                            foreach (OSD o in picks)
                            {
                                ProfilePickInfo info = new ProfilePickInfo ();
                                info.FromOSD ((OSDMap)o);
                                list.Add (info);
                            }
                            return list;
                        }
                        return new List<ProfilePickInfo>();
                    }
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat ("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.ToString ());
            }
            return null;
        }

        public void RemovePick (UUID queryPickID)
        {
            try
            {
                List<string> serverURIs = m_registry.RequestModuleInterface<IConfigurationService> ().FindValueOf ("RemoteServerURI");
                foreach (string url in serverURIs)
                {
                    OSDMap map = new OSDMap ();
                    map["Method"] = "removepick";
                    map["PickUUID"] = queryPickID;
                    WebUtils.PostToService (url + "osd", map);
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat ("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.ToString ());
            }
        }

        #endregion
    }
}
