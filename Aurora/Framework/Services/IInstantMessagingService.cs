using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurora.Framework
{
    public interface IInstantMessagingService
    {
        /// <summary>
        /// The client calls the ChatSessionRequest CAP, which in turn runs this
        /// This creates conference sessions, does admin functions for sessions, adds users to conferences, and deals with voice calling
        /// </summary>
        /// <param name="caps"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        string ChatSessionRequest(IRegionClientCapsService caps, OSDMap req);

        /// <summary>
        /// Sends a chat message to a session
        /// </summary>
        /// <param name="agentID"></param>
        /// <param name="im"></param>
        void SendChatToSession(UUID agentID, GridInstantMessage im);

        /// <summary>
        /// Removes a member from a session
        /// </summary>
        /// <param name="agentID"></param>
        /// <param name="im"></param>
        void DropMemberFromSession(UUID agentID, GridInstantMessage im);

        /// <summary>
        /// Creates a new group conference session by the given user (will not replace an existing session)
        /// </summary>
        /// <param name="uUID"></param>
        /// <param name="im"></param>
        void CreateGroupChat(UUID agentID, GridInstantMessage im);

        /// <summary>
        /// Checks to make sure a group conference session exsits for the given group
        /// </summary>
        /// <param name="sessionID"></param>
        void EnsureSessionIsStarted(UUID groupID);
    }
}
