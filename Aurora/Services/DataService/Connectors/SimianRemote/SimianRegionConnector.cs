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
        private List<string> m_ServerURIs = new List<string>();

        public void Initialize(IGenericData unneeded, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("RegionConnector", "LocalConnector") == "SimianConnector")
            {
                m_ServerURIs = simBase.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                
                //If blank, no connector
                if (m_ServerURIs.Count != 0)
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

        public void AddTelehub(Telehub telehub, ulong RegionHandle)
        {
            foreach (string m_ServerURI in m_ServerURIs)
            {
                SimianUtils.AddGeneric(telehub.RegionID, "RegionTelehub", UUID.Zero.ToString(), telehub.ToOSD(), m_ServerURI);
            }
        }

        public void RemoveTelehub(UUID regionID, ulong regionHandle)
        {
            foreach (string m_ServerURI in m_ServerURIs)
            {
                SimianUtils.RemoveGenericEntry(regionID, "RegionTelehub", UUID.Zero.ToString(), m_ServerURI);
            }
        }

        public Telehub FindTelehub(UUID regionID, ulong Regionhandle)
        {
            foreach (string m_ServerURI in m_ServerURIs)
            {
                Dictionary<string, OSDMap> maps = new Dictionary<string, OSDMap>();
                SimianUtils.GetGenericEntries(regionID, "RegionTelehub", m_ServerURI, out maps);

                List<OSDMap> listMaps = new List<OSDMap>(maps.Values);
                if (listMaps.Count == 0)
                    continue;

                Telehub t = new Telehub();
                t.FromOSD(listMaps[0]);
                return t;
            }
            return null;
        }

        #endregion
    }
}
