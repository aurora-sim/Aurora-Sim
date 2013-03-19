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

using System;
using Aurora.Framework.ClientInterfaces;
using Aurora.Framework.Services.ClassHelpers.Assets;
using OpenMetaverse;

namespace Aurora.Framework.Modules
{
    /// <summary>
    ///     Implemented by classes which collect up non-viewer statistical information
    /// </summary>
    public interface IStatsCollector
    {
        /// <summary>
        ///     Report back collected statistical information.
        /// </summary>
        /// <returns></returns>
        string Report();
    }

    public delegate void SendStatResult(SimStats stats);

    public class MonitorModuleHelper
    {
        public static string SimFrameStats = "SimFrameStats";
        public static string TimeDilation = "Time Dilation";
        public static string TotalFrameTime = "Total Frame Time";
        public static string SleepFrameTime = "Sleep Frame Time";
        public static string OtherFrameTime = "Other Frame Time";
        public static string TotalPhysicsFrameTime = "Total Physics Frame Time";
        public static string PhysicsSyncFrameTime = "Physics Sync Frame Time";
        public static string PhysicsUpdateFrameTime = "Physics Update Frame Time";
        public static string AgentUpdateCount = "Agent Update Count";
        public static string NetworkMonitor = "Network Monitor";
        public static string ImagesFrameTime = "Images Frame Time";
        public static string ScriptFrameTime = "Script Frame Time";
        public static string TotalScriptCount = "Total Script Count";
        public static string LoginMonitor = "LoginMonitor";
        public static string AssetMonitor = "AssetMonitor";
        public static string LastCompletedFrameAt = "Last Completed Frame At";
        public static string PrimUpdates = "PrimUpdates";
    }

    public interface IMonitorModule
    {
        /// <summary>
        ///     Event that gives others the SimStats class that is being sent out to the client
        /// </summary>
        event SendStatResult OnSendStatsResult;

        /// <summary>
        ///     Get a monitor module by the RegionID (key parameter, can be "" to get the base monitors) and Name of the monitor
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Name"></param>
        /// <returns></returns>
        IMonitor GetMonitor(string Key, string Name);

        /// <summary>
        ///     Get the latest stats
        /// </summary>
        /// <param name="key">The RegionID of the region</param>
        /// <returns></returns>
        float[] GetRegionStats(string key);
    }

    public interface IMonitor
    {
        /// <summary>
        ///     Get the value of this monitor
        /// </summary>
        /// <returns></returns>
        double GetValue();

        /// <summary>
        ///     Get the name of this monitor
        /// </summary>
        /// <returns></returns>
        string GetName();

        /// <summary>
        ///     Get the nice looking value of GetValue()
        /// </summary>
        /// <returns></returns>
        string GetFriendlyValue();

        /// <summary>
        ///     Resets any per stats beat stats that may need done
        /// </summary>
        void ResetStats();
    }

    public delegate void Alert(Type reporter, string reason, bool fatal);

    public interface IAlert
    {
        /// <summary>
        ///     The name of the alert
        /// </summary>
        /// <returns></returns>
        string GetName();

        /// <summary>
        ///     Test the alert
        /// </summary>
        void Test();

        /// <summary>
        ///     What will happen when the alert is triggered
        /// </summary>
        event Alert OnTriggerAlert;
    }

    public interface ITimeMonitor : IMonitor
    {
        void AddTime(int time);
    }

    public interface IAssetMonitor
    {
        /// <summary>
        ///     Add a failure to ask the asset service
        /// </summary>
        void AddAssetServiceRequestFailure();

        /// <summary>
        ///     The time that it took to request the asset after it was not found in the cache
        /// </summary>
        /// <param name="ts"></param>
        void AddAssetRequestTimeAfterCacheMiss(TimeSpan ts);

        /// <summary>
        ///     Add the asset's memory to the memory count
        /// </summary>
        /// <param name="asset"></param>
        void AddAsset(AssetBase asset);

        /// <summary>
        ///     This asset was removed, take it out of the asset list
        /// </summary>
        /// <param name="uuid"></param>
        void RemoveAsset(UUID uuid);

        /// <summary>
        ///     Clear the cache for assets
        /// </summary>
        void ClearAssetCacheStatistics();

        /// <summary>
        ///     Add a missing texture request
        /// </summary>
        void AddBlockedMissingTextureRequest();
    }

    public interface INetworkMonitor
    {
        /// <summary>
        ///     The number of packets coming in per second
        /// </summary>
        float InPacketsPerSecond { get; }

        /// <summary>
        ///     The number of packets going out per second
        /// </summary>
        float OutPacketsPerSecond { get; }

        /// <summary>
        ///     The number of bytes that we have not acked yet (see LLUDPClient for more info)
        /// </summary>
        float UnackedBytes { get; }

        /// <summary>
        ///     The number of downloads that the client has requested, but has not recieved at this time
        /// </summary>
        float PendingDownloads { get; }

        /// <summary>
        ///     The number of updates that the client has started, but not finished
        /// </summary>
        float PendingUploads { get; }

        /// <summary>
        ///     Add the number of packets that are incoming
        /// </summary>
        /// <param name="numPackets"></param>
        void AddInPackets(int numPackets);

        /// <summary>
        ///     Add the number of outgoing packets
        /// </summary>
        /// <param name="numPackets"></param>
        void AddOutPackets(int numPackets);

        /// <summary>
        ///     Add the current bytes that are not acked
        /// </summary>
        /// <param name="numBytes"></param>
        void AddUnackedBytes(int numBytes);

        /// <summary>
        ///     Add new pending downloads
        /// </summary>
        /// <param name="count"></param>
        void AddPendingDownloads(int count);

        /// <summary>
        ///     Add new pending upload
        /// </summary>
        /// <param name="count"></param>
        void AddPendingUploads(int count);
    }

    public interface IScriptCountMonitor : IMonitor
    {
        /// <summary>
        ///     The number of active scripts in the region
        /// </summary>
        int ActiveScripts { get; }

        /// <summary>
        ///     The number of events firing per second in the script engine
        /// </summary>
        int ScriptEPS { get; }
    }

    public interface ISetMonitor : IMonitor
    {
        /// <summary>
        ///     Set the Value for the monitor
        /// </summary>
        /// <param name="value"></param>
        void SetValue(int value);
    }

    public interface ITimeDilationMonitor : IMonitor
    {
        /// <summary>
        ///     Set the Value for the monitor
        /// </summary>
        /// <param name="value"></param>
        void SetPhysicsFPS(float value);
    }

    public interface IPhysicsFrameMonitor
    {
        /// <summary>
        ///     The last reported PhysicsSim FPS
        /// </summary>
        float LastReportedPhysicsFPS { get; set; }

        /// <summary>
        ///     The 'current' Physics FPS (NOTE: This will NOT be what you expect, you will have to divide by the time since the last check to get the correct average Physics FPS)
        /// </summary>
        float PhysicsFPS { get; }

        /// <summary>
        ///     Add X frames to the stats
        /// </summary>
        /// <param name="frames"></param>
        void AddFPS(int frames);
    }

    public interface ISimFrameMonitor
    {
        /// <summary>
        ///     The last reported Sim FPS (for llGetRegionFPS())
        /// </summary>
        float LastReportedSimFPS { get; set; }

        /// <summary>
        ///     The 'current' Sim FPS (NOTE: This will NOT be what you expect, you will have to divide by the time since the last check to get the correct average Sim FPS)
        /// </summary>
        float SimFPS { get; }

        /// <summary>
        ///     Add X frames to the stats
        /// </summary>
        /// <param name="frames"></param>
        void AddFPS(int frames);
    }

    public interface IImageFrameTimeMonitor
    {
        /// <summary>
        ///     Add the time it took to process sending of images to the client
        /// </summary>
        /// <param name="time">time in milliseconds</param>
        void AddImageTime(int time);
    }

    public interface ITotalFrameTimeMonitor : IMonitor
    {
        /// <summary>
        ///     Add the time it took to process sending of images to the client
        /// </summary>
        /// <param name="time">time in milliseconds</param>
        void AddFrameTime(int time);
    }

    public interface IObjectUpdateMonitor
    {
        /// <summary>
        ///     The current number of prims that were not sent to the client
        /// </summary>
        float PrimsLimited { get; }

        /// <summary>
        ///     Add X prims updates that were limited to the stats
        /// </summary>
        /// <param name="prims"></param>
        void AddLimitedPrims(int prims);
    }

    public interface IAgentUpdateMonitor
    {
        /// <summary>
        ///     The time it takes to update the agent with info
        /// </summary>
        int AgentFrameTime { get; }

        /// <summary>
        ///     The number of updates sent to the agent
        /// </summary>
        int AgentUpdates { get; }

        /// <summary>
        ///     Add the agent updates
        /// </summary>
        /// <param name="value"></param>
        void AddAgentUpdates(int value);

        /// <summary>
        ///     Add the amount of time it took to update the client
        /// </summary>
        /// <param name="value"></param>
        void AddAgentTime(int value);
    }

    public interface ILoginMonitor
    {
        /// <summary>
        ///     Add a successful login to the stats
        /// </summary>
        void AddSuccessfulLogin();

        /// <summary>
        ///     Add a successful logout to the stats
        /// </summary>
        void AddLogout();

        /// <summary>
        ///     Add a terminated client thread to the stats
        /// </summary>
        void AddAbnormalClientThreadTermination();
    }
}