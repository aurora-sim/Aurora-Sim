using System;
using Aurora.Framework;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using System.Collections;
using System.Collections.Generic;
using System.Web;

namespace Aurora.Modules.OpenRegionSettingsModule
{
    public class OpenRegionSettingsConnector : IOpenRegionSettingsConnector
    {
        private string HTMLPage = "";
        #region IAuroraDataPlugin Members

        public string Name
        {
            get { return "IOpenRegionSettingsConnector"; }
        }

        public void Initialize(IGenericData GenericData, Nini.Config.IConfigSource source, IRegistryCore simBase, string DefaultConnectionString)
        {
            DataManager.DataManager.RegisterPlugin(this);

            string path = Util.BasePathCombine(System.IO.Path.Combine("data", "OpenRegionSettingsPage.html"));
            if(System.IO.File.Exists(path))
                HTMLPage = System.IO.File.ReadAllText(path);
        }

        #endregion

        #region IOpenRegionSettingsConnector Members

        public OpenRegionSettings GetSettings(UUID regionID)
        {
            OpenRegionSettings settings = new OpenRegionSettings();
            IGenericsConnector connector = DataManager.DataManager.RequestPlugin<IGenericsConnector>();

            if (connector != null)
            {
                settings = connector.GetGeneric<OpenRegionSettings>(regionID, "OpenRegionSettings", "OpenRegionSettings") ??
                           new OpenRegionSettings();
            }
            return settings;
        }

        public void SetSettings(UUID regionID, OpenRegionSettings settings)
        {
            IGenericsConnector connector = DataManager.DataManager.RequestPlugin<IGenericsConnector>();

            //Update the database
            if (connector != null)
                connector.AddGeneric(regionID, "OpenRegionSettings", "OpenRegionSettings", settings.ToOSD());
        }

        public string AddOpenRegionSettingsHTMLPage(UUID regionID)
        {
            Dictionary<string, string> vars = new Dictionary<string,string>();
            IOpenRegionSettingsConnector orsc = DataManager.DataManager.RequestPlugin<IOpenRegionSettingsConnector>();
            if (orsc != null)
            {
                OpenRegionSettings settings = orsc.GetSettings(regionID);
                vars.Add("Default Draw Distance", settings.DefaultDrawDistance.ToString());
                vars.Add("Force Draw Distance", settings.ForceDrawDistance ? "checked" : "");
                vars.Add("Max Drag Distance", settings.MaxDragDistance.ToString());
                vars.Add("Max Prim Scale", settings.MaximumPrimScale.ToString());
                vars.Add("Min Prim Scale", settings.MinimumPrimScale.ToString());
                vars.Add("Max Physical Prim Scale", settings.MaximumPhysPrimScale.ToString());
                vars.Add("Max Hollow Size", settings.MaximumHollowSize.ToString());
                vars.Add("Min Hole Size", settings.MinimumHoleSize.ToString());
                vars.Add("Max Link Count", settings.MaximumLinkCount.ToString());
                vars.Add("Max Link Count Phys", settings.MaximumLinkCountPhys.ToString());
                vars.Add("Max Inventory Items To Transfer", settings.MaximumInventoryItemsTransfer.ToString());
                vars.Add("Terrain Scale", settings.TerrainDetailScale.ToString());
                vars.Add("Show Tags", settings.ShowTags.ToString());
                vars.Add("Render Water", settings.RenderWater ? "checked" : "");
                vars.Add("Allow Minimap", settings.DisplayMinimap ? "checked" : "");
                vars.Add("Allow Physical Prims", settings.AllowPhysicalPrims ? "checked" : "");
                vars.Add("Enable Teen Mode", settings.EnableTeenMode ? "checked" : "");
                vars.Add("Enforce Max Build Constraints", settings.ClampPrimSizes ? "checked" : "");
                return CSHTMLCreator.AddHTMLPage(HTMLPage, "", "OpenRegionSettings", vars, (newVars) => 
                {
                    ParseUpdatedList(regionID, newVars);
                    return AddOpenRegionSettingsHTMLPage(regionID); 
                });
            }
            return "";
        }

        private void ParseUpdatedList(UUID regionID, Dictionary<string, string> vars)
        {
            IOpenRegionSettingsConnector orsc = DataManager.DataManager.RequestPlugin<IOpenRegionSettingsConnector>();
            if (orsc != null)
            {
                OpenRegionSettings settings = orsc.GetSettings(regionID);
                settings.DefaultDrawDistance = float.Parse(vars["Default Draw Distance"]);
                settings.ForceDrawDistance = vars["Force Draw Distance"] != null;
                settings.MaxDragDistance = float.Parse(vars["Max Drag Distance"]);
                settings.MaximumPrimScale = float.Parse(vars["Max Prim Scale"]);
                settings.MinimumPrimScale = float.Parse(vars["Min Prim Scale"]);
                settings.MaximumPhysPrimScale = float.Parse(vars["Max Physical Prim Scale"]);
                settings.MaximumHollowSize = float.Parse(vars["Max Hollow Size"]);
                settings.MinimumHoleSize = float.Parse(vars["Min Hole Size"]);
                settings.MaximumLinkCount = int.Parse(vars["Max Link Count"]);
                settings.MaximumLinkCountPhys = int.Parse(vars["Max Link Count Phys"]);
                settings.MaximumInventoryItemsTransfer = int.Parse(vars["Max Inventory Items To Transfer"]);
                settings.TerrainDetailScale = float.Parse(vars["Terrain Scale"]);
                settings.ShowTags = int.Parse(vars["Show Tags"]);
                settings.RenderWater = vars["Render Water"] != null;
                settings.DisplayMinimap = vars["Allow Minimap"] != null;
                settings.AllowPhysicalPrims = vars["Allow Physical Prims"] != null;
                settings.EnableTeenMode = vars["Enable Teen Mode"] != null;
                settings.ClampPrimSizes = vars["Enforce Max Build Constraints"] != null;
                orsc.SetSettings(regionID, settings);
            }
        }

        #endregion
    }
}
