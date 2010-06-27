using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using Aurora.Framework;

namespace Aurora.Framework
{
	public interface IAgentConnector
	{
		IAgentInfo GetAgent(UUID agentID);
		void UpdateAgent(IAgentInfo agent);
		void CreateNewAgent(UUID agentID);

        bool CheckMacAndViewer(string Mac, string viewer);
	}
}
