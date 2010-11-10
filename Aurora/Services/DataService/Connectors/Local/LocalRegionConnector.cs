using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using Aurora.Framework;
using Aurora.DataManager;
using Nini.Config;

namespace Aurora.Services.DataService
{
    public class LocalRegionConnector : IRegionConnector
    {
        private IGenericData GD = null;

        public void Initialize(IGenericData GenericData, IConfigSource source, string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("RegionConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                if (source.Configs[Name] != null)
                    defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                GD.ConnectToDatabase(defaultConnectionString);

                DataManager.DataManager.RegisterPlugin(Name, this);
            }
            else
            {
                //Check to make sure that something else exists
                string m_ServerURI = source.Configs["AuroraData"].GetString("RemoteServerURI", "");
                if (m_ServerURI == "") //Blank, not set up
                {
                    OpenSim.Framework.Console.MainConsole.Instance.Output("[AuroraDataService]: Falling back on local connector for " + "RegionConnector", "None");
                    GD = GenericData;

                    if (source.Configs[Name] != null)
                        defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                    GD.ConnectToDatabase(defaultConnectionString);

                    DataManager.DataManager.RegisterPlugin(Name, this);
                }
            }
        }

        public string Name
        {
            get { return "IRegionConnector"; }
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// Adds a new telehub in the region. Replaces an old one automatically.
        /// </summary>
        /// <param name="regionID"></param>
        /// <param name="position">Telehub position</param>
        /// <param name="regionPosX">Region Position in meters</param>
        /// <param name="regionPosY">Region Position in meters</param>
        public void AddTelehub(Telehub telehub, UUID SessionID)
        {
            //Look for a telehub first.
            if (FindTelehub(new UUID(telehub.RegionID)) != null)
            {
                //Found one, time to update it.
                GD.Update("telehubs", new object[] {
					telehub.TelehubLocX,
					telehub.TelehubLocY,
					telehub.TelehubLocZ,
                    telehub.TelehubRotX,
					telehub.TelehubRotY,
					telehub.TelehubRotZ,
					telehub.BuildFromList(telehub.SpawnPos),
					telehub.ObjectUUID,
					telehub.Name
				}, new string[] {
					"TelehubLocX",
					"TelehubLocY",
					"TelehubLocZ",
                    "TelehubRotX",
					"TelehubRotY",
					"TelehubRotZ",
                    "Spawns",
					"ObjectUUID",
					"Name"
				}, new string[] { "RegionID" }, new object[] { telehub.RegionID });
            }
            else
            {
                //Make a new one
                List<object> values = new List<object>();
                values.Add(telehub.RegionID);
                values.Add(telehub.RegionLocX);
                values.Add(telehub.RegionLocY);
                values.Add(telehub.TelehubLocX);
                values.Add(telehub.TelehubLocY);
                values.Add(telehub.TelehubLocZ);
                values.Add(telehub.TelehubRotX);
                values.Add(telehub.TelehubRotY);
                values.Add(telehub.TelehubRotZ);
                values.Add(telehub.BuildFromList(telehub.SpawnPos));
                values.Add(telehub.ObjectUUID);
                values.Add(telehub.Name);
                GD.Insert("telehubs", values.ToArray());
            }
        }

        /// <summary>
        /// Removes the telehub if it exists.
        /// </summary>
        /// <param name="regionID"></param>
        public void RemoveTelehub(UUID regionID, UUID SessionID)
        {
            //Look for a telehub first.
            Vector3 oldPos = Vector3.Zero;
            if (FindTelehub(regionID) != null)
            {
                GD.Delete("telehubs", new string[] { "RegionID" }, new object[] { regionID });
            }
        }

        /// <summary>
        /// Attempts to find a telehub in the region; if one is not found, returns false.
        /// </summary>
        /// <param name="regionID">Region ID</param>
        /// <param name="position">The position of the telehub</param>
        /// <returns></returns>
        public Telehub FindTelehub(UUID regionID)
        {
            Telehub telehub = new Telehub();
            List<string> telehubposition = GD.Query("RegionID", regionID, "telehubs", "RegionLocX,RegionLocY,TelehubLocX,TelehubLocY,TelehubLocZ,TelehubRotX,TelehubRotY,TelehubRotZ,Spawns,ObjectUUID,Name");
            //Not the right number of values, so its not there.
            if (telehubposition.Count != 11)
                return null;

            telehub.RegionID = regionID;
            telehub.RegionLocX = float.Parse(telehubposition[0]);
            telehub.RegionLocY = float.Parse(telehubposition[1]);
            telehub.TelehubLocX = float.Parse(telehubposition[2]);
            telehub.TelehubLocY = float.Parse(telehubposition[3]);
            telehub.TelehubLocZ = float.Parse(telehubposition[4]);
            telehub.TelehubRotX = float.Parse(telehubposition[5]);
            telehub.TelehubRotY = float.Parse(telehubposition[6]);
            telehub.TelehubRotZ = float.Parse(telehubposition[7]);
            telehub.SpawnPos = telehub.BuildToList(telehubposition[8]);
            telehub.ObjectUUID = UUID.Parse(telehubposition[9]);
            telehub.Name = telehubposition[10];

            return telehub;
        }
    }
}
