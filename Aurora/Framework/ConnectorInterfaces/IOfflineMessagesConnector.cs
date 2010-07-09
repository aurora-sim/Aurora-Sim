using System;
using System.Collections.Generic;
using System.Text;
using Aurora.Framework;
using OpenMetaverse;
using OpenSim.Framework;

namespace Aurora.Framework
{
	public interface IOfflineMessagesConnector
	{
        GridInstantMessage[] GetOfflineMessages(UUID agentID);
        void AddOfflineMessage(GridInstantMessage message);
	}
}
