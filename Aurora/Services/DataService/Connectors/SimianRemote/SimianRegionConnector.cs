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
    public class SimianRegionConnector : IRegionConnector
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);
        private string m_ServerURI = "";

        public void Initialize(IGenericData unneeded, ISimulationBase simBase, string defaultConnectionString)
        {
            IConfigSource source = simBase.ConfigSource;
            if (source.Configs["AuroraConnectors"].GetString("RegionConnector", "LocalConnector") == "SimianConnector")
            {
                m_ServerURI = simBase.ApplicationRegistry.RequestModuleInterface<IAutoConfigurationService>().FindValueOf("RemoteServerURI", "AuroraData");
                
                //If blank, no connector
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

        #region IRegionConnector Members

        public void AddTelehub(Telehub telehub, UUID SessionID)
        {
            SimianUtils.AddGeneric(telehub.RegionID, "RegionTelehub", SessionID.ToString(), telehub.ToOSD(), m_ServerURI);
        }

        public void RemoveTelehub(UUID regionID, UUID SessionID)
        {
            SimianUtils.RemoveGenericEntry(regionID, "RegionTelehub", SessionID.ToString(), m_ServerURI);
        }

        public Telehub FindTelehub(UUID regionID)
        {
            Dictionary<string, OSDMap> maps = new Dictionary<string,OSDMap>();
            SimianUtils.GetGenericEntries(regionID, "RegionTelehub", m_ServerURI, out maps);
            
            List<OSDMap> listMaps = new List<OSDMap>(maps.Values);
            if (listMaps.Count == 0)
                return null;

            Telehub t = new Telehub();
            t.FromOSD(listMaps[0]);
            return t;
        }

        #endregion
    }
}
