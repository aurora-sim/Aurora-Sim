using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Aurora.Framework;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;

namespace Aurora.Framework
{
	public interface IProfileConnector
	{
		Classified ReadClassifiedInfoRow(string classifiedID);
		ProfilePickInfo ReadPickInfoRow(string pickID);
		void UpdateUserNotes(UUID agentID, UUID targetAgentID, string notes, IUserProfileInfo UPI);
		IUserProfileInfo GetUserProfile(UUID agentID);
		bool UpdateUserProfile(IUserProfileInfo Profile);
		void CreateNewProfile(UUID UUID, string firstName, string lastName);
		void RemoveFromCache(UUID ID);
	}
}
