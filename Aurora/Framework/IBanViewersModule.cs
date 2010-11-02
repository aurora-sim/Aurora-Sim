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
        /// <param name="SP"></param>
        /// <param name="textureEntry"></param>
        void CheckForBannedViewer(IClientAPI SP, Primitive.TextureEntry textureEntry);
    }
}
