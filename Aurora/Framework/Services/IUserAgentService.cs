using System.Collections.Generic;
using System.Net;
using OpenMetaverse;
using Aurora.Framework;

namespace OpenSim.Services.Interfaces
{
    /// <summary>
    ///   HG1.5 only
    /// </summary>
    public interface IUserAgentService
    {
        // called by login service only
        bool LoginAgentToGrid(AgentCircuitData agent, GridRegion gatekeeper, GridRegion finalDestination,
                              IPEndPoint clientIP, out string reason);

        // called by simulators
        bool LoginAgentToGrid(AgentCircuitData agent, GridRegion gatekeeper, GridRegion finalDestination,
                              out string reason);

        void LogoutAgent(UUID userID, UUID sessionID);
        GridRegion GetHomeRegion(UUID userID, out Vector3 position, out Vector3 lookAt);
        GridRegion GetHomeRegion(AgentCircuitData circuit, out Vector3 position, out Vector3 lookAt);
        Dictionary<string, object> GetServerURLs(UUID userID);

        string LocateUser(UUID userID);
        // Tries to get the universal user identifier for the targetUserId
        // on behalf of the userID
        string GetUUI(UUID userID, UUID targetUserID);

        // Returns the local friends online
        List<UUID> StatusNotification(List<string> friends, UUID userID, bool online);
        bool RemoteStatusNotification(FriendInfo friend, UUID userID, bool online);
        //List<UUID> GetOnlineFriends(UUID userID, List<string> friends);
        Dictionary<string, object> GetUserInfo(UUID userID);

        bool AgentIsComingHome(UUID sessionID, string thisGridExternalName);
        bool VerifyAgent(UUID sessionID, string token);
        bool VerifyAgent(AgentCircuitData circuit);
        bool VerifyClient(UUID sessionID, string reportedIP);
    }
}