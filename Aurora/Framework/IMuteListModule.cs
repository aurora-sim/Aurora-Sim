using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;

namespace Aurora.Framework
{
    public interface IMuteListModule
    {
        /// <summary>
        /// Get all of a users notes
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="Cached">Are these notes already cached?</param>
        /// <returns></returns>
        MuteList[] GetMutes(UUID AgentID, out bool Cached);
        /// <summary>
        /// Update the mute list
        /// </summary>
        /// <param name="MuteID"></param>
        /// <param name="Name"></param>
        /// <param name="Flags"></param>
        /// <param name="AgentID"></param>
        void UpdateMuteList(UUID MuteID, string Name, int Flags, UUID AgentID);
        /// <summary>
        /// Remove a mute from the mute list
        /// </summary>
        /// <param name="MuteID"></param>
        /// <param name="Name"></param>
        /// <param name="AgentID"></param>
        void RemoveMute(UUID MuteID, string Name, UUID AgentID);
    }
}
