/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

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
                System.Data.IDataReader RetVal = GD.QueryData ("where IsPublic = 1", "avatararchives", "Name, Snapshot, IsPublic");
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
