using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using Aurora.Framework.Capabilities;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Simulation.Base;

namespace Aurora.Services
{
    public class StatsModule : ICapsServiceConnector
    {
        private IRegionClientCapsService m_service;

        /// <summary>
        /// Callback for a viewerstats cap
        /// </summary>
        /// <param name="request"></param>
        /// <param name="path"></param>
        /// <param name="param"></param>
        /// <param name="agentID"></param>
        /// <param name="caps"></param>
        /// <returns></returns>
        public byte[] ViewerStatsReport(string path, Stream request, OSHttpRequest httpRequest,
                                                            OSHttpResponse httpResponse)
        {
            IUserStatsDataConnector dataConnector = Aurora.DataManager.DataManager.RequestPlugin<IUserStatsDataConnector>();

            OpenMetaverse.Messages.Linden.ViewerStatsMessage vsm = new OpenMetaverse.Messages.Linden.ViewerStatsMessage();
            vsm.Deserialize((OSDMap)OSDParser.DeserializeLLSDXml(request));
            dataConnector.UpdateUserStats(vsm, m_service.AgentID, m_service.Region.RegionID);

            return MainServer.BlankResponse;
        }

        public void RegisterCaps(IRegionClientCapsService service)
        {
            m_service = service;
            service.AddStreamHandler("ViewerStats", new GenericStreamHandler("POST", service.CreateCAPS("ViewerStats", ""), ViewerStatsReport));
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
            MainConsole.Instance.Commands.AddCommand("clear user metrics", "clear user metrics", "Clear all saved user metrics", ClearMetrics);
        }

        public void FinishedStartup()
        {
        }

        public void ClearMetrics(string[] cmd)
        {
            IUserStatsDataConnector dc = Aurora.DataManager.DataManager.RequestPlugin<IUserStatsDataConnector>();
            if (dc != null)
                dc.RemoveAllSessions();
        }

        public void Metrics(string[] cmd)
        {
            IUserStatsDataConnector dc = Aurora.DataManager.DataManager.RequestPlugin<IUserStatsDataConnector>();
            if (dc != null)
            {
                MainConsole.Instance.Info(string.Format("Graphic cards: {0} logins have used ATI, {1} logins have used NVIDIA, {2} logins have used Intel graphics",
                    dc.GetCount("s_gpuvendor", new KeyValuePair<string, object>("s_gpuvendor", "ATI")),
                    dc.GetCount("s_gpuvendor", new KeyValuePair<string, object>("s_gpuvendor", "NVIDIA")),
                    dc.GetCount("s_gpuvendor", new KeyValuePair<string, object>("s_gpuvendor", "Intel"))));

                List<float> fps = dc.Get("fps").ConvertAll<float>((s) => float.Parse(s));
                if(fps.Count > 0)
                    MainConsole.Instance.Info(string.Format("Average fps: {0}", fps.Average()));

                List<float> run_time = dc.Get("run_time").ConvertAll<float>((s) => float.Parse(s));
                if (run_time.Count > 0)
                    MainConsole.Instance.Info(string.Format("Average viewer run time: {0}", run_time.Average()));

                List<int> regions_visited = dc.Get("regions_visited").ConvertAll<int>((s) => int.Parse(s));
                if (regions_visited.Count > 0)
                    MainConsole.Instance.Info(string.Format("Average regions visited: {0}", regions_visited.Average()));

                List<int> mem_use = dc.Get("mem_use").ConvertAll<int>((s) => int.Parse(s));
                if (mem_use.Count > 0)
                    MainConsole.Instance.Info(string.Format("Average viewer memory use: {0} mb", mem_use.Average() / 1000));

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
