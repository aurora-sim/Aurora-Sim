using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;
using OpenSim.Framework;

namespace Aurora.Services.DataService
{
	public class LocalScriptDataConnector : IScriptDataConnector
	{
		private IGenericData GenericData = null;
		public LocalScriptDataConnector(IGenericData connector)
		{
            GenericData = connector;
		}

		public StateSave GetStateSave(UUID itemID, UUID UserInventoryItemID)
		{
            try
            {
                StateSave StateSave = new StateSave();
                List<string> StateSaveRetVals = new List<string>();
                if (UserInventoryItemID != UUID.Zero)
                {
                    StateSaveRetVals = GenericData.Query("UserInventoryItemID", UserInventoryItemID.ToString(), "auroradotnetstatesaves", "*");
                }
                else
                {
                    StateSaveRetVals = GenericData.Query("ItemID", itemID.ToString(), "auroradotnetstatesaves", "*");
                }
                if (StateSaveRetVals.Count == 0)
                    return null;
                Dictionary<string, object> vars = new Dictionary<string, object>();
                StateSave.State = StateSaveRetVals[0];
                StateSave.ItemID = new UUID(StateSaveRetVals[1]);
                StateSave.Source = StateSaveRetVals[2];
                StateSave.Running = bool.Parse(StateSaveRetVals[4]);

                string varsmap = StateSaveRetVals[5];

                foreach (string var in varsmap.Split(';'))
                {
                    if (var == "")
                        continue;
                    string value = var.Split(',')[1].Replace("\n", "");
                    vars.Add(var.Split(',')[0], (object)value);
                }
                StateSave.Variables = vars;

                List<object> plugins = new List<object>();
                object[] pluginsSaved = StateSaveRetVals[6].Split(',');
                if (pluginsSaved.Length != 1)
                {
                    foreach (object plugin in pluginsSaved)
                    {
                        if (plugin == null)
                            continue;
                        plugins.Add(plugin);
                    }
                }
                StateSave.Plugins = plugins.ToArray();
                StateSave.Permissions = StateSaveRetVals[9];
                double minEventDelay = 0.0;
                double.TryParse(StateSaveRetVals[10], NumberStyles.Float, Culture.NumberFormatInfo, out minEventDelay);
                StateSave.MinEventDelay = (long)minEventDelay;
                StateSave.AssemblyName = StateSaveRetVals[11];
                StateSave.Disabled = Convert.ToBoolean(StateSaveRetVals[12]);
                StateSave.UserInventoryID = UUID.Parse(StateSaveRetVals[13]);
                StateSave.Queue = StateSaveRetVals[8];

                return StateSave;
            }
            catch
            {
                return null;
            }
		}

		public void SaveStateSave(StateSave state)
		{
			List<object> Insert = new List<object>();
			Insert.Add(state.State);
			Insert.Add(state.ItemID);
			Insert.Add(state.Source);
			Insert.Add(state.Running);
			Insert.Add(state.Variables);
			Insert.Add(state.Plugins);
			Insert.Add(state.Queue);
			Insert.Add(state.Permissions);
			Insert.Add(state.MinEventDelay);
			Insert.Add(state.AssemblyName);
			Insert.Add(state.Disabled);
			Insert.Add(state.UserInventoryID);
			try {
				GenericData.Insert("auroraDotNetStateSaves", Insert.ToArray());
			} catch (Exception) {
				//Needs to be updated then
				List<string> Keys = new List<string>();
				Keys.Add("State");
				Keys.Add("ItemID");
				Keys.Add("Source");
				Keys.Add("LineMap");
				Keys.Add("Running");
				Keys.Add("Variables");
				Keys.Add("Plugins");
				Keys.Add("ClassID");
				Keys.Add("Queue");
				Keys.Add("Permissions");
				Keys.Add("MinEventDelay");
				Keys.Add("AssemblyName");
				Keys.Add("Disabled");
				Keys.Add("UserInventoryItemID");
				try {
					GenericData.Update("auroraDotNetStateSaves", Insert.ToArray(), Keys.ToArray(), new string[] { "ItemID" }, new object[] { state.ItemID });
				} catch (Exception ex) {
					//Throw this one... Something is very wrong.
					throw ex;
				}
			}
		}

        public void DeleteStateSave(UUID itemID)
        {
            GenericData.Delete("auroraDotNetStateSaves", new string[] { "ItemID" }, new object[] { itemID });
        }

        public void DeleteStateSave(string assemblyName)
        {
            GenericData.Delete("auroraDotNetStateSaves", new string[] { "AssemblyName" }, new object[] { assemblyName });
        }
	}
}
