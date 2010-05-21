using System;
using System.Collections.Generic;
using System.Text;
using Aurora.Framework;
using OpenSim.Framework;
using OpenSim.Framework.Console;

namespace Aurora.Services.DataService
{
	//This will always be local, as this is only used by the grid server.
	//The region server should not be using this class.
    public class LocalAvatarArchiverConnector : IAvatarArchiverConnector
	{
		private IGenericData GD = null;
		public LocalAvatarArchiverConnector()
		{
			GD = Aurora.DataManager.DataManager.GetDefaultGenericPlugin();
			List<string> Results = GD.Query("Method", "AvatarArchive", "Passwords", "Password");
			if (Results.Count == 0) 
            {
				string newPass = MainConsole.Instance.CmdPrompt("Password to access Avatar Archive");
				GD.Insert("Passwords", new object[] {
					"AvatarArchive",
					Util.Md5Hash(newPass)
				});
			}
		}

		public AvatarArchive GetAvatarArchive(string Name, string Password)
		{
			if (!CheckPassword(Password))
				return null;
			List<string> RetVal = GD.Query("Name", Name, "AvatarArchives", "*");
			if (RetVal.Count == 0)
            {
				return null;
			}
			AvatarArchive Archive = new AvatarArchive();
			Archive.Name = RetVal[0];
			Archive.ArchiveXML = RetVal[1];
			return Archive;
		}

		public void SaveAvatarArchive(AvatarArchive archive, string Password)
		{
			if (!CheckPassword(Password))
				return;
			List<string> Check = GD.Query("Name", archive.Name, "AvatarArchives", "Name");
			if (Check.Count == 0)
            {
				GD.Insert("AvatarArchives", new object[] {
					archive.Name,
					archive.ArchiveXML
				});
			}
            else
            {
				GD.Update("AvatarArchives", new object[] { archive.ArchiveXML }, new string[] { "Archive" }, new string[] { "Name" }, new object[] { archive.Name });
			}
		}

		private bool CheckPassword(string Password)
		{
			List<string> TruePassword = GD.Query("Method", "AvatarArchive", "Passwords", "Password");
            if (TruePassword.Count == 0)
                return false;
            if (Util.Md5Hash(Password) == TruePassword[0])
				return true;
			return false;
		}
	}

}
