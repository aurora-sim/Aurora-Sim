using System;
using System.Collections.Generic;
using System.Text;
using Aurora.Framework;
using OpenSim.Framework;
using Nini.Config;
using System.Data;

namespace Aurora.Services.DataService
{
	//This will always be local, as this is only used by the grid server.
	//The region server should not be using this class.
    public class LocalAvatarArchiverConnector : IAvatarArchiverConnector
	{
		private IGenericData GD = null;

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("AvatarArchiverConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                if (source.Configs[Name] != null)
                    defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                GD.ConnectToDatabase(defaultConnectionString, "AvatarArchive", source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));
                DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IAvatarArchiverConnector"; }
        }

        public void Dispose()
        {
        }

		public AvatarArchive GetAvatarArchive(string Name)
		{
			List<string> RetVal = GD.Query("Name", Name, "avatararchives", "*");
			if (RetVal.Count == 0)
				return null;

			AvatarArchive Archive = new AvatarArchive();
            Archive.Name = RetVal[0];
            Archive.ArchiveXML = RetVal[1];
			return Archive;
		}

        /// <summary>
        /// Returns a list object of AvatarArchives. This is being used for wiredux
        /// </summary>
        /// <param name="Public"></param>
        /// <returns></returns>
        public List<AvatarArchive> GetAvatarArchives(bool isPublic)
        {
            List<AvatarArchive> returnValue = new List<AvatarArchive>();
            try
            {
                System.Data.IDbCommand cmd = GD.QueryData ("where IsPublic = 1", "avatararchives", "Name, Snapshot, IsPublic");
                System.Data.IDataReader RetVal = cmd.ExecuteReader ();
                while (RetVal.Read ())
                {
                    AvatarArchive Archive = new AvatarArchive ();
                    Archive.Name = RetVal["Name"].ToString ();
                    Archive.Snapshot = RetVal["Snapshot"].ToString ();
                    Archive.IsPublic = int.Parse (RetVal["IsPublic"].ToString ());
                    returnValue.Add (Archive);
                }
                RetVal.Close ();
                RetVal.Dispose ();
                cmd.Dispose ();
            }
            catch
            {
            }
            GD.CloseDatabase ();
            return returnValue;
        }
		public void SaveAvatarArchive(AvatarArchive archive)
		{
			List<string> Check = GD.Query("Name", archive.Name, "avatararchives", "Name");
			if (Check.Count == 0)
            {
                GD.Insert("avatararchives", new object[] {
					archive.Name,
					archive.ArchiveXML,
                    archive.Snapshot,
                    archive.IsPublic
				});
			}
            else
            {
				GD.Update("avatararchives", new object[] { archive.ArchiveXML }, new string[] { "Archive" }, new string[] { "Name" }, new object[] { archive.Name });
			}
		}
	}

}
