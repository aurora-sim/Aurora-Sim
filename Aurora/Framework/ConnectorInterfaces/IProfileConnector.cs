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
        /// <summary>
        /// Gets the profile for an agent
        /// </summary>
        /// <param name="agentID"></param>
        /// <returns></returns>
        IUserProfileInfo GetUserProfile(UUID agentID);

        /// <summary>
        /// Updates the user's profile
        /// </summary>
        /// <param name="Profile"></param>
        /// <returns></returns>
        bool UpdateUserProfile(IUserProfileInfo Profile);

        /// <summary>
        /// Creates an new profile for the user
        /// </summary>
        /// <param name="UUID"></param>
		void CreateNewProfile(UUID UUID);
    }
}
