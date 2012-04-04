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

namespace Aurora.Modules
{
    public class IWCProfileConnector : ConnectorBase, IProfileConnector
    {
        protected LocalProfileConnector m_localService;

        #region IProfileConnector Members

        public void Initialize(IGenericData unneeded, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("ProfileConnector", "LocalConnector") == "IWCConnector")
            {
                m_localService = new LocalProfileConnector();
                m_localService.Initialize(unneeded, source, simBase, defaultConnectionString);
                m_registry = simBase;
                DataManager.DataManager.RegisterPlugin(this);
                Init(simBase, Name);
            }
        }

        public string Name
        {
            get { return "IProfileConnector"; }
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public IUserProfileInfo GetUserProfile(UUID agentID)
        {
            IUserProfileInfo profile = m_localService.GetUserProfile(agentID);
            if (profile == null)
                profile = (IUserProfileInfo)DoRemoteForced(agentID);
            return profile;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public bool UpdateUserProfile(IUserProfileInfo Profile)
        {
            bool success = m_localService.UpdateUserProfile(Profile);
            if (!success)
                success = (bool)DoRemoteForced(Profile);
            return success;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Full)]
        public void CreateNewProfile(UUID UUID)
        {
            m_localService.CreateNewProfile(UUID);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public bool AddClassified(Classified classified)
        {
            bool success = m_localService.AddClassified(classified);
            if (!success)
                success = (bool)DoRemoteForced(classified);
            return success;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public Classified GetClassified(UUID queryClassifiedID)
        {
            Classified Classified = m_localService.GetClassified(queryClassifiedID);
            if (Classified == null)
                Classified = (Classified)DoRemoteForced(queryClassifiedID);
            return Classified;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<Classified> GetClassifieds(UUID ownerID)
        {
            List<Classified> Classifieds = m_localService.GetClassifieds(ownerID);
            if (Classifieds == null)
                Classifieds = (List<Classified>)DoRemoteForced(ownerID);
            return Classifieds;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public void RemoveClassified(UUID queryClassifiedID)
        {
            m_localService.RemoveClassified(queryClassifiedID);
            DoRemoteForced(queryClassifiedID);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public bool AddPick(ProfilePickInfo pick)
        {
            bool success = m_localService.AddPick(pick);
            if (!success)
                success = (bool)DoRemoteForced(pick);
            return success;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public ProfilePickInfo GetPick(UUID queryPickID)
        {
            ProfilePickInfo pick = m_localService.GetPick(queryPickID);
            if (pick == null)
                pick = (ProfilePickInfo)DoRemoteForced(queryPickID);
            return pick;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<ProfilePickInfo> GetPicks(UUID ownerID)
        {
            List<ProfilePickInfo> picks = m_localService.GetPicks(ownerID);
            if (picks == null)
                picks = (List<ProfilePickInfo>)DoRemoteForced(ownerID);
            return picks;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public void RemovePick(UUID queryPickID)
        {
            m_localService.RemovePick(queryPickID);
            DoRemoteForced(queryPickID);
        }

        #endregion

        public void Dispose()
        {
        }
    }
}