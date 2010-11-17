using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;

namespace OpenSim.Framework
{
    public interface IScenePresence
    {
        /// <summary>
        /// UUID of the client
        /// </summary>
        UUID UUID { get; set; }

        /// <summary>
        /// First name of the client
        /// </summary>
        string Firstname { get; }

        /// <summary>
        /// Last name of the client
        /// </summary>
        string Lastname { get; }

        /// <summary>
        /// The actual client base (it sends and recieves packets)
        /// </summary>
        IClientAPI ControllingClient { get; }

        /// <summary>
        /// Is this client really in this region?
        /// </summary>
        bool IsChildAgent { get; set; }

        /// <summary>
        /// The position of this client
        /// </summary>
        Vector3 AbsolutePosition { get; set; }

        /// <summary>
        /// Where this client is looking
        /// </summary>
        Vector3 Lookat { get; }
    }
}
