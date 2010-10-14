using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;

namespace Aurora.Framework
{
    public interface IMuteListModule
    {
        MuteList[] GetMutes(UUID AgentID, out bool Cached);
        void UpdateMuteList(UUID MuteID, string Name, int Flags, UUID AgentID);
        void RemoveMute(UUID MuteID, string Name, UUID AgentID);
    }
}
