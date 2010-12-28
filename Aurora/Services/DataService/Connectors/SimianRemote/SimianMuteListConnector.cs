using System;
using System.Collections;
using System.Collections.Specialized;
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
    public class SimianMuteListConnector : IMuteListConnector
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private string m_ServerURI = "";

        public void Initialize(IGenericData unneeded, ISimulationBase simBase, string DefaultConnectionString)
        {
            if (simBase.ConfigSource.Configs["AuroraConnectors"].GetString("MuteListConnector", "LocalConnector") == "SimianConnector")
            {
                m_ServerURI = simBase.ApplicationRegistry.RequestModuleInterface<IAutoConfigurationService>().FindValueOf("RemoteServerURI", "AuroraData");
                DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IMuteListConnector"; }
        }

        public void Dispose()
        {
        }

        #region IMuteListConnector Members

        public MuteList[] GetMuteList(UUID PrincipalID)
        {
            List<MuteList> Mutes = new List<MuteList>();
            Dictionary<string, OSDMap> Map;
            if (SimianUtils.GetGenericEntries(PrincipalID, "MuteList", m_ServerURI, out Map))
            {
                foreach (object OSDMap in Map.Values)
                {
                    MuteList mute = new MuteList();
                    mute.FromOSD((OSDMap)OSDMap);
                    Mutes.Add(mute);
                }

                return Mutes.ToArray();
            }
            return null;
        }

        public void UpdateMute(MuteList mute, UUID PrincipalID)
        {
            SimianUtils.AddGeneric(PrincipalID, "MuteList", mute.MuteID.ToString(), Util.DictionaryToOSD(mute.ToKeyValuePairs()), m_ServerURI);
        }

        public void DeleteMute(UUID muteID, UUID PrincipalID)
        {
            SimianUtils.RemoveGenericEntry(PrincipalID, "MuteList", muteID.ToString(), m_ServerURI);
        }

        public bool IsMuted(UUID PrincipalID, UUID PossibleMuteID)
        {
            OSDMap map = null;
            if (SimianUtils.GetGenericEntry(PrincipalID, "MuteList", PossibleMuteID.ToString(), m_ServerURI, out map))
                return true;
            return false;
        }

        #endregion
    }
}
