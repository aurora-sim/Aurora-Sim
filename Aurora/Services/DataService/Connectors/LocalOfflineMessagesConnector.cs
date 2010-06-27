using System;
using System.Collections.Generic;
using System.Text;
using Aurora.DataManager;
using Aurora.Framework;
using OpenMetaverse;

namespace Aurora.Services.DataService
{
	public class LocalOfflineMessagesConnector : IOfflineMessagesConnector
	{
		private IGenericData GD = null;
        public LocalOfflineMessagesConnector(IGenericData GenericData)
        {
            GD = GenericData;
		}

		public OfflineMessage[] GetOfflineMessages(UUID agentID)
		{
			List<OfflineMessage> messages = new List<OfflineMessage>();
			List<string> Messages = GD.Query("ToUUID", agentID, "offlinemessages", "*");
			GD.Delete("offlinemessages", new string[] { "ToUUID" }, new object[] { agentID });
            if (Messages.Count == 0)
                return messages.ToArray();
            int i = 0;
			OfflineMessage Message = new OfflineMessage();
            foreach (string part in Messages) {
				if (i == 0)
					Message.FromUUID = new UUID(part);
				if (i == 1)
					Message.FromName = part;
				if (i == 2)
					Message.ToUUID = new UUID(part);
				if (i == 3)
					Message.Message = part;
				i++;
				if (i == 4) {
					i = 0;
					messages.Add(Message);
					Message = new OfflineMessage();
				}
			}
			return messages.ToArray();
		}

		public void AddOfflineMessage(OfflineMessage message)
		{
			GD.Insert("offlinemessages", new object[] {
				message.FromUUID,
				message.FromName,
				message.ToUUID,
				message.Message
			});
		}
	}
}
