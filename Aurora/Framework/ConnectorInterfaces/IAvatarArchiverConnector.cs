using System;
using System.Collections.Generic;
using System.Text;
using Aurora.Framework;
using OpenSim.Framework;

namespace Aurora.Framework
{
    public interface IAvatarArchiverConnector : IAuroraDataPlugin
	{
        /// <summary>
        /// Gets avatar archives from the database
        /// </summary>
        /// <param name="Name">Name of the avatar archive</param>
        /// <param name="Password">Password required to access the database</param>
        /// <returns></returns>
		AvatarArchive GetAvatarArchive(string Name);

        /// <summary>
        /// Save an avatar archive to the database
        /// </summary>
        /// <param name="archive">Archive</param>
        /// <param name="Password">Password that will be required to access this archive</param>
		void SaveAvatarArchive(AvatarArchive archive);
	}
}
