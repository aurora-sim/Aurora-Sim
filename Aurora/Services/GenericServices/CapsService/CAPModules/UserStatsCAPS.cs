/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
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

using Aurora.Framework;
using Aurora.Framework.Modules;
using Aurora.Framework.Servers;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Servers.HttpServer.Implementation;
using Aurora.Framework.Services;
using Nini.Config;
using OpenMetaverse.StructuredData;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Aurora.Services
{
    public class StatsModule : ICapsServiceConnector
    {
        private IRegionClientCapsService m_service;

        /// <summary>
        ///     Callback for a viewerstats cap
        /// </summary>
        /// <param name="request"></param>
        /// <param name="path"></param>
        /// <param name="httpRequest"></param>
        /// <param name="httpResponse"></param>
        /// <returns></returns>
        public byte[] ViewerStatsReport(string path, Stream request, OSHttpRequest httpRequest,
                                        OSHttpResponse httpResponse)
        {
            IUserStatsDataConnector dataConnector =
                Framework.Utilities.DataManager.RequestPlugin<IUserStatsDataConnector>();

            OpenMetaverse.Messages.Linden.ViewerStatsMessage vsm =
                new OpenMetaverse.Messages.Linden.ViewerStatsMessage();
            vsm.Deserialize((OSDMap) OSDParser.DeserializeLLSDXml(request));
            dataConnector.UpdateUserStats(vsm, m_service.AgentID, m_service.Region.RegionID);

            return MainServer.BlankResponse;
        }

        public void RegisterCaps(IRegionClientCapsService service)
        {
            m_service = service;
            service.AddStreamHandler("ViewerStats",
                                     new GenericStreamHandler("POST", service.CreateCAPS("ViewerStats", ""),
                                                              ViewerStatsReport));
        }

        public void DeregisterCaps()
        {
            m_service.RemoveStreamHandler("ViewerStats", "POST");
        }

        public void EnteringRegion()
        {
        }
    }

    public class StatMetrics : IService
    {
        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            MainConsole.Instance.Commands.AddCommand("user metrics", "user metrics", "Gives metrics on users", Metrics);
            MainConsole.Instance.Commands.AddCommand("clear user metrics", "clear user metrics",
                                                     "Clear all saved user metrics", ClearMetrics);
        }

        public void FinishedStartup()
        {
        }

        public void ClearMetrics(string[] cmd)
        {
            IUserStatsDataConnector dc = Framework.Utilities.DataManager.RequestPlugin<IUserStatsDataConnector>();
            if (dc != null)
                dc.RemoveAllSessions();
        }

        public void Metrics(string[] cmd)
        {
            IUserStatsDataConnector dc = Framework.Utilities.DataManager.RequestPlugin<IUserStatsDataConnector>();
            if (dc != null)
            {
                MainConsole.Instance.Info(
                    string.Format(
                        "Graphic cards: {0} logins have used ATI, {1} logins have used NVIDIA, {2} logins have used Intel graphics",
                        dc.GetCount("s_gpuvendor", new KeyValuePair<string, object>("s_gpuvendor", "ATI")),
                        dc.GetCount("s_gpuvendor", new KeyValuePair<string, object>("s_gpuvendor", "NVIDIA")),
                        dc.GetCount("s_gpuvendor", new KeyValuePair<string, object>("s_gpuvendor", "Intel"))));

                List<float> fps = dc.Get("fps").ConvertAll<float>((s) => float.Parse(s));
                if (fps.Count > 0)
                    MainConsole.Instance.Info(string.Format("Average fps: {0}", fps.Average()));

                List<float> run_time = dc.Get("run_time").ConvertAll<float>((s) => float.Parse(s));
                if (run_time.Count > 0)
                    MainConsole.Instance.Info(string.Format("Average viewer run time: {0}", run_time.Average()));

                List<int> regions_visited = dc.Get("regions_visited").ConvertAll<int>((s) => int.Parse(s));
                if (regions_visited.Count > 0)
                    MainConsole.Instance.Info(string.Format("Average regions visited: {0}", regions_visited.Average()));

                List<int> mem_use = dc.Get("mem_use").ConvertAll<int>((s) => int.Parse(s));
                if (mem_use.Count > 0)
                    MainConsole.Instance.Info(string.Format("Average viewer memory use: {0} mb", mem_use.Average()/1000));

                List<float> ping = dc.Get("ping").ConvertAll<float>((s) => float.Parse(s));
                if (ping.Count > 0)
                    MainConsole.Instance.Info(string.Format("Average ping: {0}", ping.Average()));

                List<int> agents_in_view = dc.Get("agents_in_view").ConvertAll<int>((s) => int.Parse(s));
                if (agents_in_view.Count > 0)
                    MainConsole.Instance.Info(string.Format("Average agents in view: {0}", agents_in_view.Average()));
            }
        }
    }
}