using System;
using System.Collections.Generic;
using System.Text;
using Aurora.Framework;
using OpenMetaverse;

namespace Aurora.Framework
{
	public interface IOfflineMessagesConnector
	{
		OfflineMessage[] GetOfflineMessages(UUID agentID);
		void AddOfflineMessage(OfflineMessage message);
	}
}
