using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Aurora.Framework
{
	public interface IAvatarAppearanceArchiver
	{
        /// <summary>
        /// Updates an avatars appearance from the saved AvatarArchive in the database.
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="First"></param>
        /// <param name="Last"></param>
		void LoadAvatarArchive(string FileName, string First, string Last);
	}
}
