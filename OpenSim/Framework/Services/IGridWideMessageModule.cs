using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;

namespace OpenSim.Services.Interfaces
{
    public interface IGridWideMessageModule
    {
        /// <summary>
        /// Kicks the given avatarID from the grid with the given message
        /// </summary>
        /// <param name="avatarID"></param>
        void KickUser(UUID avatarID, string message);

        /// <summary>
        /// Sends a message to the given avatarID with the given message
        /// </summary>
        /// <param name="avatarID"></param>
        /// <param name="message"></param>
        void MessageUser(UUID avatarID, string message);

        /// <summary>
        /// Sends a message to all users currently in the grid (will take ~30 seconds for all messages to be sent)
        /// </summary>
        /// <param name="message"></param>
        void SendAlert(string message);
    }
}
