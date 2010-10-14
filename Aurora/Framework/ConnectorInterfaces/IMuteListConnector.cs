using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;

namespace Aurora.Framework
{
    public interface IMuteListConnector
    {
        /// <summary>
        /// Gets the full mute list for the given agent.
        /// </summary>
        /// <param name="AgentID"></param>
        /// <returns></returns>
        MuteList[] GetMuteList(UUID AgentID);

        /// <summary>
        /// Updates or adds a mute for the given agent
        /// </summary>
        /// <param name="mute"></param>
        /// <param name="AgentID"></param>
        void UpdateMute(MuteList mute, UUID AgentID);

        /// <summary>
        /// Deletes a mute for the given agent
        /// </summary>
        /// <param name="muteID"></param>
        /// <param name="AgentID"></param>
        void DeleteMute(UUID muteID, UUID AgentID);

        /// <summary>
        /// Checks to see if PossibleMuteID is muted by AgentID
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="PossibleMuteID"></param>
        /// <returns></returns>
        bool IsMuted(UUID AgentID, UUID PossibleMuteID);
    }
}
