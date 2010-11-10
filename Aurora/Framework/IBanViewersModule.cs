using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Framework;
using OpenMetaverse;

namespace Aurora.Framework
{
    public interface IBanViewersModule
    {
        /// <summary>
        /// List of viewer names that are banned
        /// </summary>
        List<string> BannedViewers { get; }

        /// <summary>
        /// Check this user to see if they are using a banned viewer
        /// </summary>
        /// <param name="client">Client of the user</param>
        /// <param name="textureEntry">The textures the viewer is giving the server</param>
        void CheckForBannedViewer(IClientAPI client, Primitive.TextureEntry textureEntry);
    }
}
