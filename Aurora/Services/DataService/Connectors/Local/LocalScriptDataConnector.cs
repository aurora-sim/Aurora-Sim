using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;
using OpenSim.Framework;
using Nini.Config;

namespace Aurora.Services.DataService
{
    public class LocalScriptDataConnector : IScriptDataConnector, IAuroraDataPlugin
	{
        private IGenericData GD = null;

        public void Initialize(IGenericData GenericData, IConfigSource source, string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("IScriptDataConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                if (source.Configs[Name] != null)
                    defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                GD.ConnectToDatabase(defaultConnectionString);

                DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IScriptDataConnector"; }
        }

        public void Dispose()
        {
        }

		public StateSave GetStateSave(UUID itemID, UUID UserInventoryItemID)
		{
            try
            {
                StateSave StateSave = new StateSave();
                List<string> StateSaveRetVals = new List<string>();
                if (UserInventoryItemID != UUID.Zero)
                {
                    StateSaveRetVals = GD.Query("UserInventoryItemID", UserInventoryItemID.ToString(), "auroradotnetstatesaves", "*");
                }
                else
                {
                    StateSaveRetVals = GD.Query("ItemID", itemID.ToString(), "auroradotnetstatesaves", "*");
                }
                if (StateSaveRetVals.Count == 0)
                    return null;
                Dictionary<string, object> vars = new Dictionary<string, object>();
                StateSave.State = StateSaveRetVals[0];
                StateSave.ItemID = new UUID(StateSaveRetVals[1]);
                StateSave.Source = StateSaveRetVals[2];
                StateSave.Running = int.Parse(StateSaveRetVals[3]) == 1;

                string varsmap = StateSaveRetVals[4];
                if (varsmap != " " && varsmap != "")
                {
                    varsmap = varsmap.Replace('\n', ';');
                    foreach (string var in varsmap.Split(';'))
                    {
                        if (var == "")
                            continue;
                        string[] values = var.Split(',');
                        string value = "";
                        int i = 0;
                        foreach (string val in values)
                        {
                            if (i != 0)
                            {
                                value += val + ",";
                            }
                            i++;
                        }
                        value = value.Remove(value.Length - 1, 1);
                        vars.Add(var.Split(',')[0], (object)value);
                    }
                }
                StateSave.Variables = vars;

                List<object> plugins = new List<object>();
                object[] pluginsSaved = StateSaveRetVals[5].Split(',');
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
                StateSave.Permissions = StateSaveRetVals[6];
                double minEventDelay = 0.0;
                double.TryParse(StateSaveRetVals[7], NumberStyles.Float, Culture.NumberFormatInfo, out minEventDelay);
                StateSave.MinEventDelay = (long)minEventDelay;
                StateSave.AssemblyName = StateSaveRetVals[8];
                StateSave.Disabled = int.Parse(StateSaveRetVals[9]) == 1;
                StateSave.UserInventoryID = UUID.Parse(StateSaveRetVals[10]);

                return StateSave;
            }
            catch
            {
                return null;
            }
		}

		public void SaveStateSave(StateSave state)
		{
            List<string> Keys = new List<string>();
            Keys.Add("State");
            Keys.Add("ItemID");
            Keys.Add("Source");
            Keys.Add("Running");
            Keys.Add("Variables");
            Keys.Add("Plugins");
            Keys.Add("Permissions");
            Keys.Add("MinEventDelay");
            Keys.Add("AssemblyName");
            Keys.Add("Disabled");
            Keys.Add("UserInventoryItemID");

            List<object> Insert = new List<object>();
            Insert.Add(state.State);
            Insert.Add(state.ItemID);
            Insert.Add(state.Source);
            Insert.Add(state.Running ? 1 : 0);
            Insert.Add(state.Variables);
            Insert.Add(state.Plugins);
            Insert.Add(state.Permissions);
            Insert.Add(state.MinEventDelay);
            Insert.Add(state.AssemblyName);
            Insert.Add(state.Disabled ? 1 : 0);
            Insert.Add(state.UserInventoryID);
            GD.Replace("auroradotnetstatesaves", Keys.ToArray(), Insert.ToArray());
		}

        public void DeleteStateSave(UUID itemID)
        {
            GD.Delete("auroradotnetstatesaves", new string[] { "ItemID" }, new object[] { itemID });
        }

        public void DeleteStateSave(string assemblyName)
        {
            GD.Delete("auroradotnetstatesaves", new string[] { "AssemblyName" }, new object[] { assemblyName });
        }
	}
}
