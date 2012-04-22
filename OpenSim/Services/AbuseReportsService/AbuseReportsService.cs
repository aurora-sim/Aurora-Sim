/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services.AbuseReports
{
    public class AbuseReportsService : ConnectorBase, IAbuseReports, IService
    {
        public string Name
        {
            get { return GetType().Name; }
        }

        #region IAbuseReports Members

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public void AddAbuseReport(AbuseReport abuse_report)
        {
            object remoteValue = DoRemote(abuse_report);
            if (remoteValue != null || m_doRemoteOnly)
                return;

            IAbuseReportsConnector conn = DataManager.RequestPlugin<IAbuseReportsConnector>();
            if (conn != null)
                conn.AddAbuseReport(abuse_report);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Full)]
        public AbuseReport GetAbuseReport(int Number, string Password)
        {
            object remoteValue = DoRemote(Number, Password);
            if (remoteValue != null || m_doRemoteOnly)
                return (AbuseReport)remoteValue;

            IAbuseReportsConnector conn = DataManager.RequestPlugin<IAbuseReportsConnector>();
            if (conn != null)
                return conn.GetAbuseReport(Number, Password);
            else
                return null;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Full)]
        public void UpdateAbuseReport(AbuseReport report, string Password)
        {
            object remoteValue = DoRemote(report, Password);
            if (remoteValue != null || m_doRemoteOnly)
                return;

            IAbuseReportsConnector conn = DataManager.RequestPlugin<IAbuseReportsConnector>();
            if (conn != null)
                conn.UpdateAbuseReport(report, Password);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Full)]
        public List<AbuseReport> GetAbuseReports(int start, int count, bool active)
        {
            object remoteValue = DoRemote(start, count, active);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<AbuseReport>)remoteValue;

            IAbuseReportsConnector conn = DataManager.RequestPlugin<IAbuseReportsConnector>();
            if (conn != null)
                return conn.GetAbuseReports(start, count, active);
            else
                return null;
        }

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            registry.RegisterModuleInterface<IAbuseReports>(this);
            Init(registry, Name);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
        }

        #endregion
    }
}