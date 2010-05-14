using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.DataManager;
using Aurora.Framework;

namespace Aurora.Services.DataService
{
    public class LocalAssetConnector
    {
        private IGenericData GD = null;
        public LocalAssetConnector()
		{
			GD = Aurora.DataManager.DataManager.GetDefaultGenericPlugin();
		}

        public ObjectMediaURL getObjectMediaInfo(string objectID, int side)
        {
            ObjectMediaURL info = new ObjectMediaURL();
            List<string> data = GD.Query(new string[] { "objectUUID", "side" }, new string[] { objectID, side.ToString() }, "assetMediaURL", "*");
            if (data.Count == 1)
                return null;
            info.alt_image_enable = data[2];
            info.auto_loop = Convert.ToInt32(data[3]) == 1;
            info.auto_play = Convert.ToInt32(data[4]) == 1;
            info.auto_scale = Convert.ToInt32(data[5]) == 1;
            info.auto_zoom = Convert.ToInt32(data[6]) == 1;
            info.controls = Convert.ToInt32(data[7]);
            info.current_url = data[8];
            info.first_click_interact = Convert.ToInt32(data[9]) == 1;
            info.height_pixels = Convert.ToInt32(data[10]);
            info.home_url = data[11];
            info.perms_control = Convert.ToInt32(data[12]);
            info.perms_interact = Convert.ToInt32(data[13]);
            info.whitelist = data[14];
            info.whitelist_enable = Convert.ToInt32(data[15]) == 1;
            info.width_pixels = Convert.ToInt32(data[16]);
            info.object_media_version = data[17];
            return info;
        }
    }
}
