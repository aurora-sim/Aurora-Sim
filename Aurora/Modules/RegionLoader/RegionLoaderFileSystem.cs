/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using Nini.Config;
using OpenSim.Framework.Console;
using log4net;
using OpenMetaverse;
using System.Reflection;
using System.Net;

namespace OpenSim.Framework.RegionLoader.Filesystem
{
    public class RegionLoaderFileSystem : IRegionLoader
    {
        private static readonly ILog m_log
            = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public string Name
        {
            get
            {
                return "RegionLoaderDataBaseSystem";
            }
        }

        private IConfigSource m_configSource;

        public void SetIniConfigSource(IConfigSource configSource)
        {
            m_configSource = configSource;
        }

        public RegionInfo[] LoadRegions()
        {
            RegionInfo[] infos = Aurora.DataManager.DataManager.IRegionInfoConnector.GetRegionInfos();
            if (infos.Length == 0)
            {
                CreateNewRegion();
                return LoadRegions();
            }
            else
                return infos;
        }

        public void CreateNewRegion()
        {
            RegionInfo region = new RegionInfo();
            MainConsole.Instance.Output("=====================================\n");
            MainConsole.Instance.Output("We are now going to ask a couple of questions about your region.\n");
            MainConsole.Instance.Output("You can press 'enter' without typing anything to use the default\n");
            MainConsole.Instance.Output("the default is displayed between [ ] brackets.\n");
            MainConsole.Instance.Output("=====================================\n");
            region.RegionName = MainConsole.Instance.CmdPrompt("New region name", region.RegionName);
            region.RegionID = UUID.Random();
            while (true)
            {
                try
                {
                    region.RegionLocX = uint.Parse(MainConsole.Instance.CmdPrompt("Region Location X", "1000"));
                    region.RegionLocY = uint.Parse(MainConsole.Instance.CmdPrompt("Region Location Y", "1000"));
                    break;
                }
                catch
                {
                    m_log.Warn("Cannot parse region Location! Please try again.");
                }
            }
            IPAddress address = IPAddress.Parse("0.0.0.0");
            int port = port = Convert.ToInt32(MainConsole.Instance.CmdPrompt("Region Port", "9000"));
            region.InternalEndPoint = new IPEndPoint(address, port);
            string externalName = MainConsole.Instance.CmdPrompt("External host name (Use DEFAULT if you are not sure, as this will find your IP automatically)", "DEFAULT");
            if (externalName == "DEFAULT")
            {
                externalName = Aurora.Framework.Utils.GetExternalIp();
                region.FindExternalAutomatically = true;
            }
            else
                region.FindExternalAutomatically = false;
            region.ExternalHostName = externalName;
            region.RegionType = MainConsole.Instance.CmdPrompt("Region Type", "Mainland");
            region.NonphysPrimMax = int.Parse(MainConsole.Instance.CmdPrompt("Maximum Non-physical Prim size", "256"));
            region.PhysPrimMax = int.Parse(MainConsole.Instance.CmdPrompt("Maximum Physical Prim size", "50"));
            region.ClampPrimSize = true;
            region.ObjectCapacity = int.Parse(MainConsole.Instance.CmdPrompt("Maximum objects in this region", "65536"));
            region.AccessLevel = Util.ConvertMaturityToAccessLevel(uint.Parse(MainConsole.Instance.CmdPrompt("Region Maturity (0 - PG, 1 - Mature, 2 - Adult)", "0")));
            Aurora.DataManager.DataManager.IRegionInfoConnector.UpdateRegionInfo(region, false);
        }
    }
}
