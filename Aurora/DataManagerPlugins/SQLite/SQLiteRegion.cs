using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;
using System.Threading;
using Aurora.DataManager;
using Mono.Data.SqliteClient;
using Aurora.Framework;

namespace Aurora.DataManager.SQLite
{
    public class SQLiteRegion : SQLiteLoader, IRegionData
    {
        public ObjectMediaURLInfo getObjectMediaInfo(string objectID, int side)
        {
            ObjectMediaURLInfo info = new ObjectMediaURLInfo();
            List<string> data = Query(new string[] { "objectUUID", "side" }, new string[] { objectID, side.ToString() }, "assetMediaURL", "*");
            if (data.Count == 1)
                return null;
            for (int i = 0; i < data.Count; ++i)
            {
                if (i == 2)
                    info.alt_image_enable = data[i];
                if (i == 3)
                    info.auto_loop = Convert.ToInt32(data[i]) == 1;
                if (i == 4)
                    info.auto_play = Convert.ToInt32(data[i]) == 1;
                if (i == 5)
                    info.auto_scale = Convert.ToInt32(data[i]) == 1;
                if (i == 6)
                    info.auto_zoom = Convert.ToInt32(data[i]) == 1;
                if (i == 7)
                    info.controls = Convert.ToInt32(data[i]);
                if (i == 8)
                    info.current_url = data[i];
                if (i == 9)
                    info.first_click_interact = Convert.ToInt32(data[i]) == 1;
                if (i == 10)
                    info.height_pixels = Convert.ToInt32(data[i]);
                if (i == 11)
                    info.home_url = data[i];
                if (i == 12)
                    info.perms_control = Convert.ToInt32(data[i]);
                if (i == 13)
                    info.perms_interact = Convert.ToInt32(data[i]);
                if (i == 14)
                    info.whitelist = data[i];
                if (i == 15)
                    info.whitelist_enable = Convert.ToInt32(data[i]) == 1;
                if (i == 16)
                    info.width_pixels = Convert.ToInt32(data[i]);
                if (i == 17)
                    info.object_media_version = data[i];
            }
            return info;
        }
    }
}
