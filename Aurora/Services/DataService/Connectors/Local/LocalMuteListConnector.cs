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
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;

namespace Aurora.Services.DataService
{
    public class LocalMuteListConnector : ConnectorBase, IMuteListConnector
    {
        private IGenericData GD;

        #region IMuteListConnector Members

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            GD = GenericData;

            if (source.Configs[Name] != null)
                defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

            GD.ConnectToDatabase(defaultConnectionString, "Generics",
                                 source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

            DataManager.DataManager.RegisterPlugin(Name + "Local", this);

            if (source.Configs["AuroraConnectors"].GetString("MuteListConnector", "LocalConnector") == "LocalConnector")
            {
                DataManager.DataManager.RegisterPlugin(this);
            }
            Init(simBase, Name);
        }

        public string Name
        {
            get { return "IMuteListConnector"; }
        }

        /// <summary>
        ///   Gets the full mute list for the given agent.
        /// </summary>
        /// <param name = "AgentID"></param>
        /// <returns></returns>
        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<MuteList> GetMuteList(UUID AgentID)
        {
            object remoteValue = DoRemote(AgentID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<MuteList>)remoteValue;

            return GenericUtils.GetGenerics<MuteList>(AgentID, "MuteList", GD);
        }

        /// <summary>
        ///   Updates or adds a mute for the given agent
        /// </summary>
        /// <param name = "mute"></param>
        /// <param name = "AgentID"></param>
        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public void UpdateMute(MuteList mute, UUID AgentID)
        {
            object remoteValue = DoRemote(mute, AgentID);
            if (remoteValue != null || m_doRemoteOnly)
                return;

            GenericUtils.AddGeneric(AgentID, "MuteList", mute.MuteID.ToString(), mute.ToOSD(), GD);
        }

        /// <summary>
        ///   Deletes a mute for the given agent
        /// </summary>
        /// <param name = "muteID"></param>
        /// <param name = "AgentID"></param>
        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public void DeleteMute(UUID muteID, UUID AgentID)
        {
            object remoteValue = DoRemote(muteID, AgentID);
            if (remoteValue != null || m_doRemoteOnly)
                return;

            GenericUtils.RemoveGenericByKeyAndType(AgentID, "MuteList", muteID.ToString(), GD);
        }

        /// <summary>
        ///   Checks to see if PossibleMuteID is muted by AgentID
        /// </summary>
        /// <param name = "AgentID"></param>
        /// <param name = "PossibleMuteID"></param>
        /// <returns></returns>
        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public bool IsMuted(UUID AgentID, UUID PossibleMuteID)
        {
            object remoteValue = DoRemote(AgentID, PossibleMuteID);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue != null && (bool)remoteValue;

            return GenericUtils.GetGeneric<MuteList>(AgentID, "MuteList", PossibleMuteID.ToString(), GD) != null;
        }

        #endregion

        public void Dispose()
        {
        }
    }
}