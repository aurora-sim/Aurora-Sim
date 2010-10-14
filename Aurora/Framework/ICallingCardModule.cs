using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenSim.Framework;

namespace Aurora.Framework
{
    public interface ICallingCardModule
    {
        void CreateCallingCard(IClientAPI client, UUID creator, UUID folder, string name);
    }
}
