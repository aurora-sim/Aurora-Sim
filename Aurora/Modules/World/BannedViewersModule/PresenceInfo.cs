/*
 *  Copyright 2011 Matthew Beardmore
 *
 *  This file is part of Aurora.Addon.Protection.
 *  Aurora.Addon.Protection is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 *  Aurora.Addon.Protection is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 *  You should have received a copy of the GNU General Public License along with Aurora.Addon.Protection. If not, see http://www.gnu.org/licenses/.
 */

using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using Aurora.Framework;

namespace Aurora.Modules
{
    public class PresenceInfo
    {
        public UUID AgentID;

        public string LastKnownIP = "";
        public string LastKnownViewer = "";
        public string LastKnownID0 = "";
        public string LastKnownMac = "";
        public string Platform = "";

        public List<string> KnownIPs = new List<string>();
        public List<string> KnownViewers = new List<string>();
        public List<string> KnownMacs = new List<string>();
        public List<string> KnownID0s = new List<string>();
        public List<string> KnownAlts = new List<string>();

        public PresenceInfoFlags Flags;
        public enum PresenceInfoFlags : int
        {
            Clean = 1 << 1,
            Suspected = 1 << 2,
            Known = 1 << 3,
            SuspectedAltAccount = 1 << 4,
            SuspectedAltAccountOfKnown = 1 << 5,
            SuspectedAltAccountOfSuspected = 1 << 6,
            KnownAltAccountOfKnown = 1 << 7,
            KnownAltAccountOfSuspected = 1 << 8,
            Banned = 1 << 9
        }
    }

    public interface IPresenceInfo : IAuroraDataPlugin
    {
        PresenceInfo GetPresenceInfo(UUID agentID);
        void UpdatePresenceInfo(PresenceInfo agent);
        void Check(PresenceInfo info, List<string> viewers, bool includeList);
        void Check(List<string> viewers, bool includeList);
    }
}
