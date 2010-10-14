using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using Aurora.Framework;

namespace Aurora.Framework
{
	public interface IAgentConnector
	{
        /// <summary>
        /// Gets the info about the agent (TOS data, maturity info, language, etc)
        /// </summary>
        /// <param name="agentID"></param>
        /// <returns></returns>
		IAgentInfo GetAgent(UUID agentID);

        /// <summary>
        /// Updates the language and maturity params of the agent.
        /// Note: we only allow for this on the grid side
        /// </summary>
        /// <param name="agent"></param>
		void UpdateAgent(IAgentInfo agent);

        /// <summary>
        /// Creates a new database entry for the agent.
        /// Note: we only allow for this on the grid side
        /// </summary>
        /// <param name="agentID"></param>
		void CreateNewAgent(UUID agentID);

        /// <summary>
        /// Checks whether the mac address and viewer are allowed to connect to this grid.
        /// Note: we only allow for this on the grid side
        /// </summary>
        /// <param name="Mac"></param>
        /// <param name="viewer"></param>
        /// <returns></returns>
        bool CheckMacAndViewer(string Mac, string viewer);
	}
}
