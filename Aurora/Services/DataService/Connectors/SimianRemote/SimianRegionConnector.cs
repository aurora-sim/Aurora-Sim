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

using System.Collections.Generic;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Services.Interfaces;

namespace Aurora.Services.DataService
{
    public class SimianRegionConnector : IRegionConnector
    {
        private List<string> m_ServerURIs = new List<string>();

        #region IRegionConnector Members

        public void Initialize(IGenericData unneeded, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("RegionConnector", "LocalConnector") == "SimianConnector")
            {
                m_ServerURIs = simBase.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");

                //If blank, no connector
                if (m_ServerURIs.Count != 0)
                    DataManager.DataManager.RegisterPlugin(this);
            }
        }

        public string Name
        {
            get { return "IRegionConnector"; }
        }

        public void AddTelehub(Telehub telehub, ulong RegionHandle)
        {
            foreach (string m_ServerURI in m_ServerURIs)
            {
                SimianUtils.AddGeneric(telehub.RegionID, "RegionTelehub", UUID.Zero.ToString(), telehub.ToOSD(),
                                       m_ServerURI);
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

        public void Dispose()
        {
        }
    }
}