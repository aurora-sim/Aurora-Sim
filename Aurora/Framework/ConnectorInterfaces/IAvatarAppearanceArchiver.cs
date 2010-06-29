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
		void LoadAvatarArchive(string FileName, string First, string Last);
	}
}
