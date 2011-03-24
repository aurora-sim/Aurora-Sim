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
            if (source.Configs["AuroraConnectors"].GetString("ProfileConnector", "LocalConnector") == "RemoteConnector")
            {
                m_registry = simBase;
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
            Dictionary<string, object> sendData = Profile.ToKeyValuePairs();

            sendData["PRINCIPALID"] = Profile.PrincipalID.ToString();
            sendData["METHOD"] = "updateprofile";

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                List<string> serverURIs = m_registry.RequestModuleInterface<IConfigurationService> ().FindValueOf (Profile.PrincipalID.ToString (), "RemoteServerURI");
                foreach (string url in serverURIs)
                {
                    OSDMap map = new OSDMap ();
                    map["Method"] = "updateprofile";
                    map["Profile"] = Profile.ToOSD();
                    OSDMap response = WebUtils.PostToService (url + "osd", map);
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

        public void AddClassified (Classified classified)
        {
            throw new NotImplementedException ();
        }

        public Classified GetClassified (UUID queryClassifiedID)
        {
            throw new NotImplementedException ();
        }

        public List<Classified> GetClassifieds (UUID ownerID)
        {
            throw new NotImplementedException ();
        }

        public void RemoveClassified (UUID queryClassifiedID)
        {
            throw new NotImplementedException ();
        }

        public void AddPick (ProfilePickInfo pick)
        {
            throw new NotImplementedException ();
        }

        public ProfilePickInfo GetPick (UUID queryPickID)
        {
            throw new NotImplementedException ();
        }

        public List<ProfilePickInfo> GetPicks (UUID ownerID)
        {
            throw new NotImplementedException ();
        }

        public void RemovePick (UUID queryPickID)
        {
            throw new NotImplementedException ();
        }

        #endregion
    }
}
