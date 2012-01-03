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
using System.Linq;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Services.Interfaces;

namespace Aurora.Services.DataService
{
    public class SimianMuteListConnector : IMuteListConnector
    {
        private List<string> m_ServerURIs = new List<string>();

        #region IMuteListConnector Members

        public void Initialize(IGenericData unneeded, IConfigSource source, IRegistryCore simBase,
                               string DefaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("MuteListConnector", "LocalConnector") == "SimianConnector")
            {
                m_ServerURIs = simBase.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                DataManager.DataManager.RegisterPlugin(this);
            }
        }

        public string Name
        {
            get { return "IMuteListConnector"; }
        }

        public MuteList[] GetMuteList(UUID PrincipalID)
        {
            List<MuteList> Mutes = new List<MuteList>();
            Dictionary<string, OSDMap> Map = null;
#if (!ISWIN)
            foreach (string mServerUri in m_ServerURIs)
            {
                if (SimianUtils.GetGenericEntries(PrincipalID, "MuteList", mServerUri, out Map))
                {
                    foreach (object OSDMap in Map.Values)
                    {
                        MuteList mute = new MuteList();
                        mute.FromOSD((OSDMap) OSDMap);
                        Mutes.Add(mute);
                    }

                    return Mutes.ToArray();
                }
            }
#else
            if (m_ServerURIs.Any(m_ServerURI => SimianUtils.GetGenericEntries(PrincipalID, "MuteList", m_ServerURI, out Map)))
            {
                foreach (object OSDMap in Map.Values)
                {
                    MuteList mute = new MuteList();
                    mute.FromOSD((OSDMap) OSDMap);
                    Mutes.Add(mute);
                }

                return Mutes.ToArray();
            }
#endif
            return null;
        }

        public void UpdateMute(MuteList mute, UUID PrincipalID)
        {
            foreach (string m_ServerURI in m_ServerURIs)
            {
                SimianUtils.AddGeneric(PrincipalID, "MuteList", mute.MuteID.ToString(), mute.ToOSD(), m_ServerURI);
            }
        }

        public void DeleteMute(UUID muteID, UUID PrincipalID)
        {
            foreach (string m_ServerURI in m_ServerURIs)
            {
                SimianUtils.RemoveGenericEntry(PrincipalID, "MuteList", muteID.ToString(), m_ServerURI);
            }
        }

        public bool IsMuted(UUID PrincipalID, UUID PossibleMuteID)
        {
            foreach (string m_ServerURI in m_ServerURIs)
            {
                OSDMap map = null;
                if (SimianUtils.GetGenericEntry(PrincipalID, "MuteList", PossibleMuteID.ToString(), m_ServerURI, out map))
                    return true;
            }
            return false;
        }

        #endregion

        public void Dispose()
        {
        }
    }
}