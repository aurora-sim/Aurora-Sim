using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;

namespace Aurora.Framework
{
    public interface IMuteListConnector
    {
        MuteList[] GetMuteList(UUID AgentID);
        void UpdateMute(MuteList mute, UUID AgentID);
        void DeleteMute(UUID muteID, UUID AgentID);
        bool IsMuted(UUID AgentID, UUID PossibleMuteID);
    }
}
