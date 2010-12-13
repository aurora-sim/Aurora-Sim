/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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

using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.CoreModules.Framework.Monitoring.Monitors
{
    public class SimFrameMonitor : IMonitor, ISimFrameStats
    {
        #region Declares

        private readonly Scene m_scene;
        // saved last reported value so there is something available for llGetRegionFPS 
        private volatile float lastReportedSimFPS = 0;

        private volatile float timeDilation;
        private volatile float simFps;
        private volatile float physicsFps;
        private volatile float agentUpdates;
        private volatile float totalFrameTime;
        private volatile float netFrameTime;
        private volatile float physicsFrameTime;
        private volatile float physicsFrameTimeOther;
        private volatile float physicsStep = 0;
        private volatile float otherFrameTime;
        private volatile float imageFrameTime;
        private volatile float sleepFrameTime;
        private volatile float inPacketsPerSecond;
        private volatile float outPacketsPerSecond;
        private volatile float unackedBytes;
        private volatile float agentFrameTime;
        private volatile float pendingDownloads;
        private volatile float pendingUploads;

        #endregion

        #region Public properties

        /// <summary>
        /// This is for llGetRegionFPS
        /// </summary>
        public float LastReportedSimFPS
        {
            get { return lastReportedSimFPS; }
            set { lastReportedSimFPS = value; }
        }

        public float TimeDilation { get { return timeDilation; } }
        public float SimFPS { get { return simFps; } }
        public float AgentUpdates { get { return agentUpdates; } }
        public float TotalFrameTime { get { return totalFrameTime; } }
        public float NetFrameTime { get { return netFrameTime; } }
        public float PhysicsFrameTime { get { return physicsFrameTime; } }
        public float PhysicsFrameTimeOther { get { return physicsFrameTimeOther; } }
        public float PhysicsStep { get { return physicsStep; } }
        public float OtherFrameTime { get { return otherFrameTime; } }
        public float ImageFrameTime { get { return imageFrameTime; } }
        public float SleepFrameTime { get { return sleepFrameTime; } }
        public float InPacketsPerSecond { get { return inPacketsPerSecond; } }
        public float OutPacketsPerSecond { get { return outPacketsPerSecond; } }
        public float UnackedBytes { get { return unackedBytes; } }
        public float AgentFrameTime { get { return agentFrameTime; } }
        public float PendingDownloads { get { return pendingDownloads; } }
        public float PendingUploads { get { return pendingUploads; } }

        #endregion

        #region Constructor

        public SimFrameMonitor(Scene scene)
        {
            m_scene = scene;
        }

        #endregion

        #region Implementation of IMonitor

        public double GetValue()
        {
            return 0;
        }

        public string GetName()
        {
            return "SimFrameMonitor";
        }

        public string GetFriendlyValue()
        {
            string Value = "";
            Value += "FRAME STATISTICS" + "\n";
            Value += "Dilatn  SimFPS  PhyFPS  AgntUp  RootAg  ChldAg  Prims   AtvPrm  AtvScr  ScrLPS" + "\n";
            Value += 
                string.Format(
                    "{0,6:0.00}  {1,6:0}  {2,6:0.0}  {3,6:0.0}  {4,6:0}  {5,6:0}  {6,6:0}  {7,6:0}  {8,6:0}  {9,6:0}",
                    timeDilation, simFps, physicsFps, agentUpdates, m_scene.SceneGraph.GetRootAgentCount(),
                    m_scene.SceneGraph.GetChildAgentCount(), m_scene.SceneGraph.GetTotalObjectsCount(), m_scene.SceneGraph.GetActiveObjectsCount(), m_scene.SceneGraph.GetActiveScriptsCount(), m_scene.SceneGraph.GetScriptEPS());

            Value += "\n";
            Value += "\n";
            // There is no script frame time currently because we don't yet collect it
            Value += "PktsIn  PktOut  PendDl  PendUl  UnackB  TotlFt  NetFt   PhysFt  OthrFt  AgntFt  ImgsFt" + "\n";
            Value += string.Format(
                    "{0,6:0}  {1,6:0}  {2,6:0}  {3,6:0}  {4,6:0}  {5,6:0.0}  {6,6:0.0}  {7,6:0.0}  {8,6:0.0}  {9,6:0.0}  {10,6:0.0}",
                    inPacketsPerSecond, outPacketsPerSecond, pendingDownloads, pendingUploads, unackedBytes, totalFrameTime,
                    netFrameTime, physicsFrameTime, otherFrameTime, agentFrameTime, imageFrameTime) + "\n";
            return Value;
        }

        #endregion

        #region Other Methods

        public void AddPacketsStats(int inPackets, int outPackets, int unAckedBytes)
        {
            AddInPackets(inPackets);
            AddOutPackets(outPackets);
            AddUnackedBytes(unAckedBytes);
        }

        public void AddAgentTime(int ms)
        {
            AddFrameMS(ms);
            AddAgentMS(ms);
        }

        public void AddTimeDilation(float td)
        {
            //float tdsetting = td;
            //if (tdsetting > 1.0f)
            //tdsetting = (tdsetting - (tdsetting - 0.91f));

            //if (tdsetting < 0)
            //tdsetting = 0.0f;
            timeDilation = td;
        }

        public void AddFPS(int frames)
        {
            simFps += frames;
        }

        public void AddPhysicsFPS(float frames)
        {
            physicsFps += frames;
        }

        public void AddAgentUpdates(int numUpdates)
        {
            agentUpdates += numUpdates;
        }

        public void AddInPackets(int numPackets)
        {
            inPacketsPerSecond += numPackets;
        }

        public void AddOutPackets(int numPackets)
        {
            outPacketsPerSecond += numPackets;
        }

        public void AddUnackedBytes(int numBytes)
        {
            unackedBytes += numBytes;
        }

        public void AddFrameMS(int ms)
        {
            simFps += ms;
        }

        public void AddNetMS(int ms)
        {
            netFrameTime += ms;
        }

        public void AddAgentMS(int ms)
        {
            agentFrameTime += ms;
        }

        public void AddPhysicsMS(int ms)
        {
            physicsFrameTime += ms;
        }

        public void AddPhysicsStep(int ms)
        {
            physicsStep += ms;
        }

        public void AddImageMS(int ms)
        {
            imageFrameTime += ms;
        }

        public void AddOtherMS(int ms)
        {
            otherFrameTime += ms;
        }

        public void AddSleepMS(int ms)
        {
            sleepFrameTime += ms;
        }

        public void AddPhysicsOther(int ms)
        {
            physicsFrameTimeOther += ms;
        }

        public void AddPendingDownloads(int count)
        {
            pendingDownloads += count;
        }

        public void ResetStats()
        {
            timeDilation = 0;
            simFps = 0;
            physicsFps = 0;
            agentUpdates = 0;
            totalFrameTime = 0;
            netFrameTime = 0;
            physicsFrameTime = 0;
            physicsFrameTimeOther = 0;
            physicsStep = 0;
            otherFrameTime = 0;
            imageFrameTime = 0;
            sleepFrameTime = 0;
            inPacketsPerSecond = 0;
            outPacketsPerSecond = 0;
            unackedBytes = 0;
            agentFrameTime = 0;
            pendingDownloads = 0;
            pendingUploads = 0;
        }

        #endregion
    }
}
