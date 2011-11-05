using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.DataManager;
using Aurora.Framework;
using OpenSim.Framework;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Region.Framework.Interfaces;
using System.Collections;
using System.Web;

namespace Aurora.Modules
{
    public class OpenRegionSettingsConnector : IOpenRegionSettingsConnector
    {
        #region IAuroraDataPlugin Members

        public string Name
        {
            get { return "IOpenRegionSettingsConnector"; }
        }

        public void Initialize(IGenericData GenericData, Nini.Config.IConfigSource source, IRegistryCore simBase, string DefaultConnectionString)
        {
            Aurora.DataManager.DataManager.RegisterPlugin(Name, this);
        }

        #endregion

        #region IOpenRegionSettingsConnector Members

        public OpenRegionSettings GetSettings(UUID regionID)
        {
            OpenRegionSettings settings = new OpenRegionSettings();
            IGenericsConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();

            if (connector != null)
            {
                settings = connector.GetGeneric<OpenRegionSettings>(regionID, "OpenRegionSettings", "OpenRegionSettings", new OpenRegionSettings());
                if (settings == null)
                    settings = new OpenRegionSettings();
            }
            return settings;
        }

        public void SetSettings(UUID regionID, OpenRegionSettings settings)
        {
            IGenericsConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();

            //Update the database
            if (connector != null)
                connector.AddGeneric(regionID, "OpenRegionSettings", "OpenRegionSettings", settings.ToOSD());
        }

        public string AddOpenRegionSettingsHTMLPage(UUID regionID)
        {
            string secret = Util.RandomClass.Next(0, int.MaxValue).ToString();
            string secret2 = Util.RandomClass.Next(0, int.MaxValue).ToString();
            string navUrl = MainServer.Instance.HostName +
                ":" + MainServer.Instance.Port +
                "/index.php?method=OpenRegionSettings" + secret2;
            string url = MainServer.Instance.HostName +
                ":" + MainServer.Instance.Port +
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
            });
            return navUrl;
        }

        private Hashtable HandleResponse(Hashtable request, UUID regionID)
        {
            Uri myUri = new Uri("http://localhost/index.php?" + request["body"].ToString());
            OpenSim.Region.Framework.Interfaces.IOpenRegionSettingsConnector orsc = Aurora.DataManager.DataManager.RequestPlugin<OpenSim.Region.Framework.Interfaces.IOpenRegionSettingsConnector>();
            if (orsc != null)
            {
                OpenSim.Region.Framework.Interfaces.OpenRegionSettings settings = orsc.GetSettings(regionID);
                settings.DefaultDrawDistance = float.Parse(HttpUtility.ParseQueryString(myUri.Query).Get("DDD"));
                settings.ForceDrawDistance = HttpUtility.ParseQueryString(myUri.Query).Get("FDD") != null;
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

        private Hashtable SetUpWebpage(Hashtable request, string url, UUID regionID)
        {
            Hashtable reply = new Hashtable();
            OpenSim.Region.Framework.Interfaces.IOpenRegionSettingsConnector orsc = Aurora.DataManager.DataManager.RequestPlugin<OpenSim.Region.Framework.Interfaces.IOpenRegionSettingsConnector>();
            if (orsc != null)
            {
                OpenSim.Region.Framework.Interfaces.OpenRegionSettings settings = orsc.GetSettings(regionID);
                string html = "<html> " +
    " <body> " +
    " <form method=\"post\" action=\"" + url + "\">" +
    " Default Draw Distance:<input type=\"text\" name=\"DDD\" value=\"" + settings.DefaultDrawDistance + "\"><br />" +
    " Force Draw Distance:<input type=\"checkbox\" " + (settings.ForceDrawDistance ? "checked" : "") + " value=\"FDD\" name=\"FDD\"><br />" +
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
