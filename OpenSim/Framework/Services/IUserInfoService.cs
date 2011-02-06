using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.Interfaces
{
    public class UserInfo
    {
        /// <summary>
        /// The user that this info is for
        /// </summary>
        public string UserID;

        public UUID SessionID;

        /// <summary>
        /// The region the user is currently active in
        /// NOTE: In a grid situation, the agent can be active in more than one region
        ///   as they can be logged in more than once
        /// </summary>
        public UUID CurrentRegionID;
        public Vector3 CurrentPosition;
        public Vector3 CurrentLookAt;

        /// <summary>
        /// The home region of this user
        /// </summary>
        public UUID HomeRegionID;
        public Vector3 HomePosition;
        public Vector3 HomeLookAt;

        /// <summary>
        /// Whether this agent is currently online
        /// </summary>
        public bool IsOnline;

        /// <summary>
        /// The last login of the user
        /// </summary>
        public DateTime LastLogin;

        /// <summary>
        /// The last logout of the user
        /// </summary>
        public DateTime LastLogout;

        /// <summary>
        /// Any other assorted into about this user
        /// </summary>
        public OSDMap Info = new OSDMap();
    }

    public interface IAgentInfoService
    {
        /// <summary>
        /// Add the given user to the given region
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="regionID"></param>
        /// <returns></returns>
        bool AddPresence(string userID, UUID regionID);

        /// <summary>
        /// Get the user infos for the given user (all regions)
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="regionID"></param>
        /// <returns></returns>
        UserInfo[] GetUserInfo(string userID);

        /// <summary>
        /// Set the home position of the given user
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="homeID"></param>
        /// <param name="homePosition"></param>
        /// <param name="homeLookAt"></param>
        /// <returns></returns>
        bool SetHomePosition(string userID, UUID homeID, Vector3 homePosition, Vector3 homeLookAt);

        /// <summary>
        /// Set the last known position of the given user
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="regionID"></param>
        /// <param name="lastPosition"></param>
        /// <param name="lastLookAt"></param>
        void SetLastPosition(string userID, UUID regionID, Vector3 lastPosition, Vector3 lastLookAt);
    }
}
