using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;

namespace Aurora.Framework
{
    public class StateSave
    {
        public string State;
        public UUID ItemID;
        public string Source;
        public bool Running;
        public bool Disabled;
        public object Variables;
        public object Plugins;
        public string Permissions;
        public double MinEventDelay;
        public string AssemblyName;
        public UUID UserInventoryID;
    }
}
