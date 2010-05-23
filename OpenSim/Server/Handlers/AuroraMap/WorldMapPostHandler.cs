using Nini.Config;
using log4net;
using System;
using System.Reflection;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using OpenSim.Server.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Services.DataService;
using OpenSim.Server.Handlers.Grid;

namespace OpenSim.Server.Handlers.AuroraMap
{
    public class WorldMapPostHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private SimMapConnector SimMapConnector = null;
        private GridServerPostHandler GSHandler;
        private IGridService GridService;

        public WorldMapPostHandler(GridServerPostHandler handler, IGridService GS) :
            base("POST", "/SIMMAP")
        {
            m_log.Debug("[AuroraSimMapConnector]: Starting...");
            GSHandler = handler;
            GridService = GS;
            SimMapConnector = new SimMapConnector(GridService);

            handler.OnDeregisterRegion += SimMapConnector.RemoveSimMap;
            handler.OnRegisterRegion += handler_OnRegisterRegion;
        }

        public override byte[] Handle(string path, Stream requestData,
                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            StreamReader sr = new StreamReader(requestData);
            string body = sr.ReadToEnd();
            sr.Close();
            body = body.Trim();

            string method = "";
            try
            {
                Dictionary<string, object> request =
                        ServerUtils.ParseQueryString(body);

                if (!request.ContainsKey("METHOD"))
                    return FailureResult();

                method = request["METHOD"].ToString();

                switch (method)
                {
                    case "getsimmap":
                        return GetSimMap(request);
                    case "getsimmaprange":
                        return GetSimMapRange(request);
                    case "updatesimmap":
                        return UpdateSimMap(request);
                    case "fullupdatesimmap":
                        return FullUpdateSimMap(request);
                    case "addagent":
                        return AddAgent(request);
                    case "removeagent":
                        return RemoveAgent(request);
                    case "getmapitems":
                        return GetMapItems(request);
                }
                m_log.DebugFormat("[AuroraDataServerPostHandler]: unknown method {0} request {1}", method.Length, method);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraDataServerPostHandler]: Exception {0} in " + method, e);
            }

            return FailureResult();
        }

        private byte[] AddAgent(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID regionID = UUID.Parse(request["REGIONID"].ToString());
            UUID agentID = UUID.Parse(request["AGENTID"].ToString());
            int X = int.Parse(request["X"].ToString());
            int Y = int.Parse(request["Y"].ToString());
            int Z = int.Parse(request["Z"].ToString());
            Vector3 Position = new Vector3(X, Y, Z);

            SimMapConnector.AddAgent(regionID,agentID,Position);

            return SuccessResult();
        }

        private byte[] RemoveAgent(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID regionID = UUID.Parse(request["REGIONID"].ToString());
            UUID agentID = UUID.Parse(request["AGENTID"].ToString());
            
            SimMapConnector.RemoveAgent(regionID, agentID);

            return SuccessResult();
        }

        private byte[] GetMapItems(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            List<SimMap> Sims = new List<SimMap>();

            ulong regionHandle = ulong.Parse(request["REGIONHANDLE"].ToString());
            GridItemType gridItemType = (GridItemType)int.Parse(request["GRIDITEMTYPE"].ToString());
            
            List<mapItemReply> items = SimMapConnector.GetMapItems(regionHandle, gridItemType);
            
            int i = 0;
            foreach (mapItemReply item in items)
            {
                result["MapItem" + i.ToString()] = (Dictionary<string, object>)item.ToKeyValuePairs();
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] GetSimMapRange(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            List<SimMap> Sims = new List<SimMap>();

            UUID agentID = UUID.Parse(request["AGENTID"].ToString());
            int regionXMin = int.Parse(request["REGIONLOCXMIN"].ToString());
            int regionYMin = int.Parse(request["REGIONLOCYMIN"].ToString());
            int regionXMax = int.Parse(request["REGIONLOCXMAX"].ToString());
            int regionYMax = int.Parse(request["REGIONLOCYMAX"].ToString());

            List<Services.Interfaces.GridRegion> Regions = GridService.GetRegionRange(UUID.Zero,
                    regionXMin,
                    regionXMax,
                    regionYMin,
                    regionYMax);

            foreach (Services.Interfaces.GridRegion region in Regions)
            {
                SimMap map = SimMapConnector.GetSimMap(region.RegionID, agentID);
                if (map != null)
                    Sims.Add(map);
            }

            if (Sims.Count == 0)
            {
                int X = (regionXMax + regionXMin) / 2;
                int Y = (regionYMax + regionYMin) / 2;
                Sims.Add(SimMapConnector.NotFound(X, Y));
            }
            int i = 0;
            foreach (SimMap map in Sims)
            {
                result["SimMap" + i.ToString()] = (Dictionary<string, object>)map.ToKeyValuePairs();
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] UpdateSimMap(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            
            UUID regionID = UUID.Parse(request["REGIONID"].ToString());

            SimMapConnector.UpdateSimMap(regionID);

            return SuccessResult();
        }

        private byte[] FullUpdateSimMap(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            SimMap simMap = new SimMap(request);

            SimMapConnector.UpdateSimMap(simMap);

            return SuccessResult();
        }

        private byte[] GetSimMap(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            List<SimMap> Sims = new List<SimMap>();

            UUID agentID = UUID.Parse(request["AGENTID"].ToString());

            if (request.ContainsKey("REGIONID"))
            {
                UUID regionID = UUID.Parse(request["REGIONID"].ToString());
                SimMap map = SimMapConnector.GetSimMap(regionID, agentID);
                if(map != null)
                    Sims.Add(map);
            }
            else if (request.ContainsKey("REGIONNAME"))
            {
                string RegionName = request["REGIONNAME"].ToString();
                List<Services.Interfaces.GridRegion> Regions = GridService.GetRegionsByName(UUID.Zero, RegionName, 20);

                foreach (Services.Interfaces.GridRegion region in Regions)
                {
                    SimMap map = SimMapConnector.GetSimMap(region.RegionID, agentID);
                    if (map != null)
                        Sims.Add(map);
                }
            }
            int i = 0;
            foreach (SimMap map in Sims)
            {
                result["SimMap" + i.ToString()] = (Dictionary<string, object>)map.ToKeyValuePairs();
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        void handler_OnRegisterRegion(OpenSim.Services.Interfaces.GridRegion region, out string result)
        {
            SimMap map = SimMapConnector.TryAddSimMap(region, out result);
        }

        #region Misc

        private byte[] SuccessResult()
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                    "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                    "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "Result", "");
            result.AppendChild(doc.CreateTextNode("Success"));

            rootElement.AppendChild(result);

            return DocToBytes(doc);
        }

        private byte[] FailureResult()
        {
            return FailureResult(String.Empty);
        }

        private byte[] FailureResult(string msg)
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                    "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                    "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "Result", "");
            result.AppendChild(doc.CreateTextNode("Failure"));

            rootElement.AppendChild(result);

            XmlElement message = doc.CreateElement("", "Message", "");
            message.AppendChild(doc.CreateTextNode(msg));

            rootElement.AppendChild(message);

            return DocToBytes(doc);
        }

        private byte[] DocToBytes(XmlDocument doc)
        {
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xw = new XmlTextWriter(ms, null);
            xw.Formatting = Formatting.Indented;
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }

        #endregion
    }
}
