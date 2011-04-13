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
        /// Check this user to see if they are using a banned viewer
        /// </summary>
        /// <param name="client">Client of the user</param>
        /// <param name="textureEntry">The textures the viewer is giving the server</param>
        void CheckForBannedViewer(IClientAPI client, Primitive.TextureEntry textureEntry);

        /// <summary>
        /// Check whether the given viewer name is banned
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isEvil"></param>
        /// <returns></returns>
        bool IsViewerBanned(string name, bool isEvil);
    }
}
