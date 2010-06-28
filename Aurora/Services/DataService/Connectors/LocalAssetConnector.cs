using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.DataManager;
using Aurora.Framework;
using OpenMetaverse;
using Nini.Config;

namespace Aurora.Services.DataService
{
    public class LocalAssetConnector : IAssetConnector, IAuroraDataPlugin
	{
		private IGenericData GD = null;

        public void Initialise(IGenericData GenericData, IConfigSource source)
        {
            if (source.Configs["AuroraConnectors"].GetString("AssetConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;
                DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IAssetConnector"; }
        }

        public void Dispose()
        {
        }

		public ObjectMediaURL GetObjectMediaInfo(string objectID, int side)
		{
			ObjectMediaURL info = new ObjectMediaURL();
			List<string> data = GD.Query(new string[] {
				"objectUUID",
				"side"
			}, new string[] {
				objectID,
				side.ToString()
			}, "assetMediaURL", "*");
			if (data.Count == 0)
				return null;
            info.ObjectID = OpenMetaverse.UUID.Parse(data[0]);
            info.OwnerID = OpenMetaverse.UUID.Parse(data[1]);
            info.alt_image_enable = bool.Parse(data[2]);
            info.auto_loop = bool.Parse(data[3]);
            info.auto_play = bool.Parse(data[4]);
            info.auto_scale = bool.Parse(data[5]);
            info.auto_zoom = bool.Parse(data[6]);
			info.controls = Convert.ToInt32(data[7]);
			info.current_url = data[8];
			info.first_click_interact = bool.Parse(data[9]);
			info.height_pixels = Convert.ToInt32(data[10]);
			info.home_url = data[11];
			info.perms_control = Convert.ToInt32(data[12]);
			info.perms_interact = Convert.ToInt32(data[13]);
			info.whitelist = data[14];
			info.whitelist_enable = bool.Parse(data[15]);
			info.width_pixels = Convert.ToInt32(data[16]);
			info.object_media_version = data[17];
            info.Side = Convert.ToInt32(data[18]);
			return info;
		}

        public void UpdateObjectMediaInfo(ObjectMediaURL media, int side, UUID ObjectID)
        {
            try
            {
                GD.Delete("assetMediaURL", new string[] { "ObjectUUID", "side" }, new object[] { ObjectID, side });
            }
            catch(Exception) { }
            if (media != null)
            {
                List<object> Values = new List<object>();
                Values.Add(media.ObjectID);
                Values.Add(media.OwnerID);
                Values.Add(media.alt_image_enable);
                Values.Add(media.auto_loop);
                Values.Add(media.auto_play);
                Values.Add(media.auto_scale);
                Values.Add(media.auto_zoom);
                Values.Add(media.controls);
                Values.Add(media.current_url);
                Values.Add(media.first_click_interact);
                Values.Add(media.height_pixels);
                Values.Add(media.home_url);
                Values.Add(media.perms_control);
                Values.Add(media.perms_interact);
                Values.Add(media.whitelist);
                Values.Add(media.whitelist_enable);
                Values.Add(media.width_pixels);
                Values.Add(media.object_media_version);
                Values.Add(media.Side);
                GD.Insert("assetMediaURL", Values.ToArray());
            }
        }

        public void UpdateLSLData(string token, string key, string value)
        {
            List<string> Test = GD.Query(new string[] { "Token", "Key" }, new string[] { token, key }, "LSLGenericData", "*");
            if (Test.Count == 0)
            {
                GD.Insert("LSLGenericData", new string[] { token, key, value });
            }
            else
            {
                GD.Update("LSLGenericData", new string[] { "Value" }, new string[] { value }, new string[] { "key" }, new string[] { key });
            }
        }

        public List<string> FindLSLData(string token, string key)
        {
            return GD.Query(new string[] { "Token", "Key" }, new string[] { token, key }, "LSLGenericData", "*");
        }
    }
}
