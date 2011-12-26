using System;
using Aurora.Framework;
using OpenSim.Framework;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using System.Collections;
using System.Collections.Generic;
using System.Web;

namespace Aurora.Modules
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
            DataManager.DataManager.RegisterPlugin(Name, this);

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
                settings = connector.GetGeneric(regionID, "OpenRegionSettings", "OpenRegionSettings", new OpenRegionSettings()) ??
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
            /*string secret = Util.RandomClass.Next(0, int.MaxValue).ToString();
            string secret2 = Util.RandomClass.Next(0, int.MaxValue).ToString();
            string navUrl = MainServer.Instance.ServerURI +
                "/index.php?method=OpenRegionSettings" + secret2;
            string url = MainServer.Instance.ServerURI +
                "/index.php?method=OpenRegionSettings" + secret;
            MainServer.Instance.RemoveHTTPHandler(null, "OpenRegionSettings" + secret);
            MainServer.Instance.RemoveHTTPHandler(null, "OpenRegionSettings" + secret2);
            MainServer.Instance.AddHTTPHandler("OpenRegionSettings" + secret2, delegate(Hashtable t)
            {
                MainServer.Instance.RemoveHTTPHandler(null, "OpenRegionSettings" + secret2);
                return SetUpWebpage(t, url, regionID);
            });
            MainServer.Instance.AddHTTPHandler("OpenRegionSettings" + secret, delegate(Hashtable t)
            {
                MainServer.Instance.RemoveHTTPHandler(null, "OpenRegionSettings" + secret);
                return HandleResponse(t, regionID);
            });*=
            return navUrl;*/
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

        private Hashtable HandleResponse(Hashtable request, UUID regionID)
        {
            Uri myUri = new Uri("http://localhost/index.php?" + request["body"]);
            IOpenRegionSettingsConnector orsc = DataManager.DataManager.RequestPlugin<IOpenRegionSettingsConnector>();
            if (orsc != null)
            {
                OpenRegionSettings settings = orsc.GetSettings(regionID);
                settings.DefaultDrawDistance = float.Parse(HttpUtility.ParseQueryString(myUri.Query).Get("Default Draw Distance"));
                settings.ForceDrawDistance = HttpUtility.ParseQueryString(myUri.Query).Get("Force Draw Distance") != null;
                settings.MaxDragDistance = float.Parse(HttpUtility.ParseQueryString(myUri.Query).Get("Max Drag Distance"));
                settings.MaximumPrimScale = float.Parse(HttpUtility.ParseQueryString(myUri.Query).Get("Max Prim Scale"));
                settings.MinimumPrimScale = float.Parse(HttpUtility.ParseQueryString(myUri.Query).Get("Min Prim Scale"));
                settings.MaximumPhysPrimScale = float.Parse(HttpUtility.ParseQueryString(myUri.Query).Get("Max Physical Prim Scale"));
                settings.MaximumHollowSize = float.Parse(HttpUtility.ParseQueryString(myUri.Query).Get("Max Hollow Size"));
                settings.MinimumHoleSize = float.Parse(HttpUtility.ParseQueryString(myUri.Query).Get("Min Hole Size"));
                settings.MaximumLinkCount = int.Parse(HttpUtility.ParseQueryString(myUri.Query).Get("Max Link Count"));
                settings.MaximumLinkCountPhys = int.Parse(HttpUtility.ParseQueryString(myUri.Query).Get("Max Link Count Phys"));
                settings.MaximumInventoryItemsTransfer = int.Parse(HttpUtility.ParseQueryString(myUri.Query).Get("Max Inventory Items To Transfer"));
                settings.TerrainDetailScale = float.Parse(HttpUtility.ParseQueryString(myUri.Query).Get("Terrain Scale"));
                settings.ShowTags = int.Parse(HttpUtility.ParseQueryString(myUri.Query).Get("Show Tags"));
                settings.RenderWater = HttpUtility.ParseQueryString(myUri.Query).Get("Render Water") != null;
                settings.DisplayMinimap = HttpUtility.ParseQueryString(myUri.Query).Get("Allow Minimap") != null;
                settings.AllowPhysicalPrims = HttpUtility.ParseQueryString(myUri.Query).Get("Allow Physical Prims") != null;
                settings.EnableTeenMode = HttpUtility.ParseQueryString(myUri.Query).Get("Enable Teen Mode") != null;
                settings.ClampPrimSizes = HttpUtility.ParseQueryString(myUri.Query).Get("Enforce Max Build Constraints") != null;
                orsc.SetSettings(regionID, settings);
            }
            Hashtable reply = new Hashtable();
            string url = AddOpenRegionSettingsHTMLPage(regionID);
            string html = "<html>" +
"<head>" +
"<meta http-equiv=\"REFRESH\" content=\"0;url=" + url + "\"></HEAD>" +
"</HTML>";
            reply["str_response_string"] = html;
            reply["int_response_code"] = 200;
            reply["content_type"] = "text/html";

            return reply;
        }
        // request is never used
        private Hashtable SetUpWebpage(Hashtable request, string url, UUID regionID)
        {
            Hashtable reply = new Hashtable();
            IOpenRegionSettingsConnector orsc = DataManager.DataManager.RequestPlugin<IOpenRegionSettingsConnector>();
            if (orsc != null)
            {
                OpenRegionSettings settings = orsc.GetSettings(regionID);
                string html = "<html> " +
    " <body> " +
    " <form method=\"post\" action=\"" + url + "\">" +
    " Default Draw Distance:<input type=\"text\" name=\"DDD\" value=\"" + settings.DefaultDrawDistance + "\"><br />" +
    " Force Draw Distance:<input type=\"checkbox\" " + (settings.ForceDrawDistance ? "checked" : "") + " name=\"FDD\"><br />" +
    " Max Drag Distance:<input type=\"text\" size=\"12\" maxlength=\"12\" name=\"Max Drag Distance\" value=\"" + settings.MaxDragDistance + "\"><br />" +
    " Max Prim Scale:<input type=\"text\" size=\"12\" maxlength=\"12\" name=\"Max Prim Scale\" value=\"" + settings.MaximumPrimScale + "\"><br />" +
    " Min Prim Scale:<input type=\"text\" size=\"12\" maxlength=\"12\" name=\"Min Prim Scale\" value=\"" + settings.MinimumPrimScale + "\"><br />" +
    " Max Physical Prim Scale:<input type=\"text\" size=\"12\" maxlength=\"12\" name=\"Max Physical Prim Scale\" value=\"" + settings.MaximumPhysPrimScale + "\"><br />" +
    " Max Hollow Size:<input type=\"text\" size=\"12\" maxlength=\"12\" name=\"Max Hollow Size\" value=\"" + settings.MaximumHollowSize + "\"><br />" +
    " Min Hole Size:<input type=\"text\" size=\"12\" maxlength=\"12\" name=\"Min Hole Size\" value=\"" + settings.MinimumHoleSize + "\"><br />" +
    " Max Link Count:<input type=\"text\" size=\"12\" maxlength=\"12\" name=\"Max Link Count\" value=\"" + settings.MaximumLinkCount + "\"><br />" +
    " Max Link Count Phys:<input type=\"text\" size=\"12\" maxlength=\"12\" name=\"Max Link Count Phys\" value=\"" + settings.MaximumLinkCountPhys + "\"><br />" +
    " Max Inventory Items To Transfer:<input type=\"text\" size=\"12\" maxlength=\"12\" name=\"Max Inventory Items To Transfer\" value=\"" + settings.MaximumInventoryItemsTransfer + "\"><br />" +
    " Terrain Scale:<input type=\"text\" size=\"12\" maxlength=\"12\" name=\"Terrain Scale\" value=\"" + settings.TerrainDetailScale + "\"><br />" +
    " Show Tags:<input type=\"text\" size=\"12\" maxlength=\"12\" name=\"Show Tags\" value=\"" + settings.ShowTags + "\"><br />" +
    " Render Water:<input type=\"checkbox\" value=\"Render Water\" " + (settings.RenderWater ? "checked" : "") + " name=\"Render Water\"><br />" +
    " Allow Minimap:<input type=\"checkbox\" value=\"Allow Minimap\" " + (settings.DisplayMinimap ? "checked" : "") + " name=\"Allow Minimap\"><br />" +
    " Allow Physical Prims:<input type=\"checkbox\" value=\"Allow Physical Prims\" " + (settings.AllowPhysicalPrims ? "checked" : "") + " name=\"Allow Physical Prims\"><br />" +
    " Enable Teen Mode:<input type=\"checkbox\" value=\"Enable Teen Mode\" " + (settings.EnableTeenMode ? "checked" : "") + " name=\"Enable Teen Mode\"><br />" +
    " Enforce Max Build Constraints:<input type=\"checkbox\" value=\"Enforce Max Build Constraints\" " + (settings.ClampPrimSizes ? "checked" : "") + " name=\"Enforce Max Build Constraints\"><br />" +
    " <input type=\"submit\" value=\"submit\" name=\"submit\"><br />" +
    " </form><br />" +
    " </body>" +
    "</html>";
                reply["str_response_string"] = html;
            }
            reply["int_response_code"] = 200;
            reply["content_type"] = "text/html";

            return reply;
        }

        #endregion
    }
}
