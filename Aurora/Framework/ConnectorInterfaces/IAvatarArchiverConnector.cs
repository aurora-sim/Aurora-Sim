using System;
using System.Collections.Generic;
using System.Text;
using Aurora.Framework;
using OpenSim.Framework;

namespace Aurora.Framework
{
	public interface IAvatarArchiverConnector
	{
		AvatarArchive GetAvatarArchive(string Name, string Password);
		void SaveAvatarArchive(AvatarArchive archive, string Password);
	}
}
