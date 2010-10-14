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
        List<string> BannedViewers { get; }
        void CheckForBannedViewer(IClientAPI SP, Primitive.TextureEntry textureEntry);
    }
}
