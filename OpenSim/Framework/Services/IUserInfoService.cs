using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using Aurora.Framework;

namespace OpenSim.Services.Interfaces
{
    public class UserInfo : IDataTransferable
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

        public override OSDMap ToOSD()
        {
             OSDMap retVal = new OSDMap();
             retVal["UserID"] = UserID;
             retVal["SessionID"] = SessionID;
             retVal["CurrentRegionID"] = CurrentRegionID;
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
             SessionID = retVal["SessionID"].AsUUID();
             CurrentRegionID = retVal["CurrentRegionID"].AsUUID();
             CurrentPosition = retVal["CurrentPosition"].AsVector3();
             CurrentLookAt = retVal["CurrentLookAt"].AsVector3();
             HomeRegionID = retVal["HomeRegionID"].AsUUID();
             HomePosition = retVal["HomePosition"].AsVector3();
             HomeLookAt = retVal["HomeLookAt"].AsVector3();
             IsOnline = retVal["IsOnline"].AsBoolean();
             LastLogin = retVal["LastLogin"].AsDate();
             LastLogout = retVal["LastLogout"].AsDate();
             Info = (OSDMap)retVal["Info"];
        }

        public override Dictionary<string, object> ToKeyValuePairs()
        {
            return Util.OSDToDictionary(ToOSD());
        }

        public override void FromKVP(Dictionary<string, object> KVP)
        {
            FromOSD(Util.DictionaryToOSD(KVP));
        }

        public override IDataTransferable Duplicate()
        {
            UserInfo m = new UserInfo();
            m.FromOSD(ToOSD());
            return m;
        }
    }

    public interface IAgentInfoService
    {
        /// <summary>
        /// Get the user infos for the given user
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="regionID"></param>
        /// <returns></returns>
        UserInfo GetUserInfo(string userID);

        /// <summary>
        /// Get the user infos for the given users
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="regionID"></param>
        /// <returns></returns>
        UserInfo[] GetUserInfos(string[] userIDs);

        /// <summary>
        /// Get the HTTP URLs for all root agents of the given users
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="regionID"></param>
        /// <returns></returns>
        string[] GetAgentsLocations(string[] userIDs);

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

        /// <summary>
        /// Log the agent in or out
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="loggingIn">Whether the user is logging in or out</param>
        void SetLoggedIn(string userID, bool loggingIn);
    }
}
