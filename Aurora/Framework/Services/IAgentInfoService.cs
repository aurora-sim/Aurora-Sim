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
using System.Collections.Generic;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework
{
    public class UserInfo : IDataTransferable
    {
        public Vector3 CurrentLookAt;
        public Vector3 CurrentPosition;

        /// <summary>
        ///     The region the user is currently active in
        /// </summary>
        public UUID CurrentRegionID;

        public string CurrentRegionURI;

        public Vector3 HomeLookAt;
        public Vector3 HomePosition;

        /// <summary>
        ///     The home region of this user
        /// </summary>
        public UUID HomeRegionID;

        /// <summary>
        ///     Any other assorted into about this user
        /// </summary>
        public OSDMap Info = new OSDMap();

        /// <summary>
        ///     Whether this agent is currently online
        /// </summary>
        public bool IsOnline;

        /// <summary>
        ///     The last login of the user
        /// </summary>
        public DateTime LastLogin;

        /// <summary>
        ///     The last logout of the user
        /// </summary>
        public DateTime LastLogout;

        /// <summary>
        ///     The user that this info is for
        /// </summary>
        public string UserID;

        public override OSDMap ToOSD()
        {
            OSDMap retVal = new OSDMap();
            retVal["UserID"] = UserID;
            retVal["CurrentRegionID"] = CurrentRegionID;
            retVal["CurrentRegionURI"] = CurrentRegionURI;
            retVal["CurrentPosition"] = CurrentPosition;
            retVal["CurrentLookAt"] = CurrentLookAt;
            retVal["HomeRegionID"] = HomeRegionID;
            retVal["HomePosition"] = HomePosition;
            retVal["HomeLookAt"] = HomeLookAt;
            retVal["IsOnline"] = IsOnline;
            retVal["LastLogin"] = LastLogin;
            retVal["LastLogout"] = LastLogout;
            retVal["Info"] = Info;
            return retVal;
        }

        public override void FromOSD(OSDMap retVal)
        {
            UserID = retVal["UserID"].AsString();
            CurrentRegionID = retVal["CurrentRegionID"].AsUUID();
            CurrentRegionURI = retVal["CurrentRegionURI"].AsString();
            CurrentPosition = retVal["CurrentPosition"].AsVector3();
            CurrentLookAt = retVal["CurrentLookAt"].AsVector3();
            HomeRegionID = retVal["HomeRegionID"].AsUUID();
            HomePosition = retVal["HomePosition"].AsVector3();
            HomeLookAt = retVal["HomeLookAt"].AsVector3();
            IsOnline = retVal["IsOnline"].AsBoolean();
            LastLogin = retVal["LastLogin"].AsDate();
            LastLogout = retVal["LastLogout"].AsDate();
            if (retVal["Info"].Type == OSDType.Map)
                Info = (OSDMap) retVal["Info"];
        }
    }

    public interface IAgentInfoService
    {
        /// <summary>
        ///     The local service (if one exists)
        /// </summary>
        IAgentInfoService InnerService { get; }

        /// <summary>
        ///     Get the user infos for the given user
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="regionID"></param>
        /// <returns></returns>
        UserInfo GetUserInfo(string userID);

        /// <summary>
        ///     Get the user infos for the given users
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="regionID"></param>
        /// <returns></returns>
        List<UserInfo> GetUserInfos(List<string> userIDs);

        /// <summary>
        ///     Gets a list of userinfos that are logged into the given region
        /// </summary>
        /// <param name="regionID"></param>
        /// <returns></returns>
        List<UserInfo> GetUserInfos(UUID regionID);

        /// <summary>
        ///     Get the HTTP URLs for all root agents of the given users
        /// </summary>
        /// <param name="requestor"></param>
        /// <param name="userIDs"></param>
        /// <returns></returns>
        List<string> GetAgentsLocations(string requestor, List<string> userIDs);

        /// <summary>
        ///     Set the home position of the given user
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="homeID"></param>
        /// <param name="homePosition"></param>
        /// <param name="homeLookAt"></param>
        /// <returns></returns>
        bool SetHomePosition(string userID, UUID homeID, Vector3 homePosition, Vector3 homeLookAt);

        /// <summary>
        ///     Set the last known position of the given user
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="regionID"></param>
        /// <param name="lastPosition"></param>
        /// <param name="lastLookAt"></param>
        void SetLastPosition(string userID, UUID regionID, Vector3 lastPosition, Vector3 lastLookAt, string regionURI);

        /// <summary>
        ///     Log the agent in or out
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="loggingIn">Whether the user is logging in or out</param>
        /// <param name="fireLoggedInEvent">Fire the event to log a user in</param>
        /// <param name="enteringRegion">The region the user is entering (if logging in)</param>
        /// <param name="enteringRegion">The regionURI the user is entering (if logging in)</param>
        void SetLoggedIn(string userID, bool loggingIn, UUID enteringRegion, string enteringRegionURI);

        /// <summary>
        ///     Fire the status changed event for this user
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="loggingIn"></param>
        /// <param name="enteringRegion"></param>
        void FireUserStatusChangeEvent(string userID, bool loggingIn, UUID enteringRegion);

        void Start(IConfigSource config, IRegistryCore registry);

        void FinishedStartup();

        void Initialize(IConfigSource config, IRegistryCore registry);
    }

    public interface IAgentInfoConnector : IAuroraDataPlugin
    {
        bool Set(UserInfo info);
        void Update(string userID, Dictionary<string, object> values);
        void SetLastPosition(string userID, UUID regionID, string regionURI, Vector3 Position, Vector3 LookAt);
        void SetHomePosition(string userID, UUID regionID, Vector3 Position, Vector3 LookAt);
        UserInfo Get(string userID, bool checkOnlineStatus, out bool onlineStatusChanged);

        uint RecentlyOnline(uint secondsAgo, bool stillOnline);

        List<UserInfo> RecentlyOnline(uint secondsAgo, bool stillOnline, Dictionary<string, bool> sort, uint start,
                                      uint count);

        List<UserInfo> GetByCurrentRegion(string regionID);
    }
}