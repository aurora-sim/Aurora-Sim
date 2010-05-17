using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;

namespace Aurora.Framework
{
    public class ObjectMediaURL
    {
        public int Side;
        public UUID ObjectID;
        public UUID OwnerID;
        public bool alt_image_enable = true;
        public bool auto_loop = true;
        public bool auto_play = true;
        public bool auto_scale = true;
        public bool auto_zoom = false;
        public int controls = 0;
        public string current_url = "http://www.google.com/";
        public bool first_click_interact = false;
        public int height_pixels = 0;
        public string home_url = "http://www.google.com/";
        public int perms_control = 7;
        public int perms_interact = 7;
        public string whitelist = "";
        public bool whitelist_enable = false;
        public int width_pixels = 0;
        public string object_media_version;
    }
}
