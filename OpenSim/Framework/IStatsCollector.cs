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

using System;
using OpenMetaverse;

namespace OpenSim.Framework
{
    /// <summary>
    /// Implemented by classes which collect up non-viewer statistical information
    /// </summary>
    public interface IStatsCollector
    {
        /// <summary>
        /// Report back collected statistical information.
        /// </summary>
        /// <returns></returns>
        string Report();
    }

    public delegate void SendStatResult(SimStats stats);
    public delegate void YourStatsAreWrong();

    public interface IMonitorModule
    {
        event SendStatResult OnSendStatsResult;

        event YourStatsAreWrong OnStatsIncorrect;

        /// <summary>
        /// Get a monitor module by the RegionID (key parameter, can be "" to get the base monitors) and Name of the monitor
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Name"></param>
        /// <returns></returns>
        IMonitor GetMonitor(string Key, string Name);

        /// <summary>
        /// Get the latest stats
        /// </summary>
        /// <param name="p">The RegionID of the region</param>
        /// <returns></returns>
        float[] GetRegionStats(string Key);
    }

    public interface IMonitor
    {
        double GetValue();
        string GetName();
        string GetFriendlyValue(); // Convert to readable numbers
    }

    public delegate void Alert(Type reporter, string reason, bool fatal);

    public interface IAlert
    {
        string GetName();
        void Test();
        event Alert OnTriggerAlert;
    }

    public interface IAssetMonitor
    {
        /// <summary>
        /// Add a failure to ask the asset service
        /// </summary>
        void AddAssetServiceRequestFailure();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ts"></param>
        void AddAssetRequestTimeAfterCacheMiss(TimeSpan ts);

        /// <summary>
        /// Add the asset's memory to the memory count
        /// </summary>
        /// <param name="asset"></param>
        void AddAsset(AssetBase asset);

        /// <summary>
        /// This asset was removed, take it out of the asset list
        /// </summary>
        /// <param name="uuid"></param>
        void RemoveAsset(UUID uuid);

        /// <summary>
        /// Clear the cache for assets
        /// </summary>
        void ClearAssetCacheStatistics();

        /// <summary>
        /// Add a missing texture request
        /// </summary>
        void AddBlockedMissingTextureRequest();
    }

    public interface ISimFrameStats
    {
        void AddAgentTime(int ms);
        void AddPacketsStats(int inPackets, int outPackets, int unAckedBytes);
        void AddTimeDilation(float td);
        void AddFPS(int frames);
        void AddPhysicsFPS(float frames);
        void AddAgentUpdates(int numUpdates);
        void AddInPackets(int numPackets);
        void AddOutPackets(int numPackets);
        void AddUnackedBytes(int numBytes);
        void AddFrameMS(int ms);
        void AddNetMS(int ms);
        void AddAgentMS(int ms);
        void AddPhysicsMS(int ms);
        void AddPhysicsStep(int ms);
        void AddImageMS(int ms);
        void AddOtherMS(int ms);
        void AddSleepMS(int ms);
        void AddPhysicsOther(int ms);
        void AddPendingDownloads(int count);
        void ResetStats();

        float LastReportedSimFPS { get; set; }
        float TimeDilation { get; }
        float SimFPS { get; }
        float AgentUpdates { get; }
        float TotalFrameTime { get; }
        float NetFrameTime { get; }
        float PhysicsFrameTime { get; }
        float PhysicsFrameTimeOther { get; }
        float PhysicsStep { get; }
        float OtherFrameTime { get; }
        float ImageFrameTime { get; }
        float SleepFrameTime { get; }
        float InPacketsPerSecond { get; }
        float OutPacketsPerSecond { get; }
        float UnackedBytes { get; }
        float AgentFrameTime { get; }
        float PendingDownloads { get; }
        float PendingUploads { get; }
    }

    public interface ILoginMonitor
    {
        /// <summary>
        /// Add a successful login to the stats
        /// </summary>
        void AddSuccessfulLogin();

        /// <summary>
        /// Add a successful logout to the stats
        /// </summary>
        void AddLogout();

        /// <summary>
        /// Add a terminated client thread to the stats
        /// </summary>
        void AddAbnormalClientThreadTermination();
    }
}
