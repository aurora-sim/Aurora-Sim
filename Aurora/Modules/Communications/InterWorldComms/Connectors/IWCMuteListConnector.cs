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
using Aurora.Services.DataService;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules
{
    public class IWCMuteListConnector : IMuteListConnector
    {
        protected LocalMuteListConnector m_localService;

        private IRegistryCore m_registry;
        protected RemoteMuteListConnector m_remoteService;

        #region IMuteListConnector Members

        public void Initialize(IGenericData unneeded, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("MuteListConnector", "LocalConnector") == "IWCConnector")
            {
                m_localService = new LocalMuteListConnector();
                m_localService.Initialize(unneeded, source, simBase, defaultConnectionString);
                m_remoteService = new RemoteMuteListConnector();
                m_remoteService.Initialize(unneeded, source, simBase, defaultConnectionString);
                m_registry = simBase;
                DataManager.DataManager.RegisterPlugin(this);
            }
        }

        public string Name
        {
            get { return "IMuteListConnector"; }
        }

        public MuteList[] GetMuteList(UUID AgentID)
        {
            List<MuteList> list = new List<MuteList>();
            List<string> serverURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(AgentID.ToString(),
                                                                                       "FriendsServerURI");
            if (serverURIs.Count > 0) //Remote user... or should be
                return m_remoteService.GetMuteList(AgentID);
            return m_localService.GetMuteList(AgentID);
        }

        public void UpdateMute(MuteList mute, UUID AgentID)
        {
            List<string> serverURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(AgentID.ToString(),
                                                                                       "FriendsServerURI");
            if (serverURIs.Count > 0) //Remote user... or should be
                m_remoteService.UpdateMute(mute, AgentID);
            else
                m_localService.UpdateMute(mute, AgentID);
        }

        public void DeleteMute(UUID muteID, UUID AgentID)
        {
            List<string> serverURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(AgentID.ToString(),
                                                                                       "FriendsServerURI");
            if (serverURIs.Count > 0) //Remote user... or should be
                m_remoteService.DeleteMute(muteID, AgentID);
            else
                m_localService.DeleteMute(muteID, AgentID);
        }

        public bool IsMuted(UUID AgentID, UUID PossibleMuteID)
        {
            List<string> serverURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(AgentID.ToString(),
                                                                                       "FriendsServerURI");
            if (serverURIs.Count > 0) //Remote user... or should be
                if (!m_remoteService.IsMuted(AgentID, PossibleMuteID))
                    return false;
                else
                    return true;
            else if (!m_localService.IsMuted(AgentID, PossibleMuteID))
                return false;
            else
                return true;
        }

        #endregion

        public void Dispose()
        {
        }
    }
}