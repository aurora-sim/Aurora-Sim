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
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Modules;
using Aurora.Framework.SceneInfo.Entities;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Servers.HttpServer.Implementation;
using Aurora.Framework.Services;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using GridRegion = Aurora.Framework.Services.GridRegion;

namespace Aurora.Services
{
    public class ObjectHandler
    {
        private readonly ISimulationService m_SimulationService;
        private readonly bool m_allowForeignIncomingObjects;

        public ObjectHandler()
        {
        }

        public ObjectHandler(ISimulationService sim, IConfigSource source)
        {
            IConfig simulationConfig = source.Configs["Handlers"];
            if (simulationConfig != null)
                m_allowForeignIncomingObjects = simulationConfig.GetBoolean("AllowIncomingForeignObjects",
                                                                            m_allowForeignIncomingObjects);
            m_SimulationService = sim.InnerService;
        }

        public byte[] Handler(string path, Stream request, OSHttpRequest httpRequest,
                                        OSHttpResponse httpResponse)
        {
            //MainConsole.Instance.Debug("[CONNECTION DEBUGGING]: ObjectHandler Called");

            //MainConsole.Instance.Debug("---------------------------");
            //MainConsole.Instance.Debug(" >> uri=" + request["uri"]);
            //MainConsole.Instance.Debug(" >> content-type=" + request["content-type"]);
            //MainConsole.Instance.Debug(" >> http-method=" + request["http-method"]);
            //MainConsole.Instance.Debug("---------------------------\n");

            httpResponse.ContentType = "text/html";

            UUID objectID;
            UUID regionID;
            string action;
            if (!WebUtils.GetParams(httpRequest.RawUrl, out objectID, out regionID, out action) ||
                m_allowForeignIncomingObjects)
            {
                //MainConsole.Instance.InfoFormat("[OBJECT HANDLER]: Invalid parameters for object message {0}", request["uri"]);
                httpResponse.StatusCode = 404;
                return Encoding.UTF8.GetBytes("false");
            }

            // Next, let's parse the verb
            string method = httpRequest.HttpMethod;
            if (method.Equals("POST"))
            {
                return DoObjectPost(request, httpRequest, httpResponse, regionID);
            }
            else if (method.Equals("PUT"))
            {
                return DoObjectPut(request, httpRequest, httpResponse, regionID);
            }
            else
            {
                MainConsole.Instance.InfoFormat("[OBJECT HANDLER]: method {0} not supported in object message", method);
                httpResponse.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                return Encoding.UTF8.GetBytes("Method not allowed");
            }
        }

        protected virtual byte[] DoObjectPost(Stream request, OSHttpRequest httpRequest,
                                        OSHttpResponse httpResponse, UUID regionID)
        {
            OSDMap args = WebUtils.GetOSDMap(request.ReadUntilEnd());
            if (args == null)
            {
                httpResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                return Encoding.UTF8.GetBytes("false");
            }
            // retrieve the input arguments
            int x = 0, y = 0;
            UUID uuid = UUID.Zero;
            string regionname = string.Empty;
            if (args.ContainsKey("destination_x") && args["destination_x"] != null)
                Int32.TryParse(args["destination_x"].AsString(), out x);
            if (args.ContainsKey("destination_y") && args["destination_y"] != null)
                Int32.TryParse(args["destination_y"].AsString(), out y);
            if (args.ContainsKey("destination_uuid") && args["destination_uuid"] != null)
                UUID.TryParse(args["destination_uuid"].AsString(), out uuid);
            if (args.ContainsKey("destination_name") && args["destination_name"] != null)
                regionname = args["destination_name"].ToString();

            GridRegion destination = new GridRegion
                                         {RegionID = uuid, RegionLocX = x, RegionLocY = y, RegionName = regionname};

            string sogXmlStr = "";
            if (args.ContainsKey("sog") && args["sog"] != null)
                sogXmlStr = args["sog"].AsString();

            ISceneEntity sog = null;
            try
            {
                //MainConsole.Instance.DebugFormat("[OBJECT HANDLER]: received {0}", sogXmlStr);
                IRegionSerialiserModule mod =
                    m_SimulationService.Scene.RequestModuleInterface<IRegionSerialiserModule>();
                if (mod != null)
                    sog = mod.DeserializeGroupFromXml2(sogXmlStr, m_SimulationService.Scene);
            }
            catch (Exception ex)
            {
                MainConsole.Instance.InfoFormat("[OBJECT HANDLER]: exception on deserializing scene object {0}", ex);
                httpResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                return Encoding.UTF8.GetBytes("Bad request");
            }

            bool result = false;

            if (sog == null)
            {
                MainConsole.Instance.ErrorFormat(
                    "[OBJECT HANDLER]: error on deserializing scene object as the object was null!");

                httpResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                return Encoding.UTF8.GetBytes(result.ToString());
            }

            try
            {
                // This is the meaning of POST object
                result = m_SimulationService.CreateObject(destination, sog);
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[OBJECT HANDLER]: Exception in CreateObject: {0}", e.StackTrace);
            }

            httpResponse.StatusCode = (int)HttpStatusCode.OK;
            return Encoding.UTF8.GetBytes(result.ToString());
        }

        protected virtual byte[] DoObjectPut(Stream request, OSHttpRequest httpRequest,
                                        OSHttpResponse httpResponse, UUID regionID)
        {
            OSDMap args = WebUtils.GetOSDMap(request.ReadUntilEnd());
            if (args == null)
            {
                httpResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                return Encoding.UTF8.GetBytes("false");
            }

            // retrieve the input arguments
            int x = 0, y = 0;
            UUID uuid = UUID.Zero;
            string regionname = string.Empty;
            if (args.ContainsKey("destination_x") && args["destination_x"] != null)
                Int32.TryParse(args["destination_x"].AsString(), out x);
            if (args.ContainsKey("destination_y") && args["destination_y"] != null)
                Int32.TryParse(args["destination_y"].AsString(), out y);
            if (args.ContainsKey("destination_uuid") && args["destination_uuid"] != null)
                UUID.TryParse(args["destination_uuid"].AsString(), out uuid);
            if (args.ContainsKey("destination_name") && args["destination_name"] != null)
                regionname = args["destination_name"].ToString();

            GridRegion destination = new GridRegion
                                         {RegionID = uuid, RegionLocX = x, RegionLocY = y, RegionName = regionname};

            UUID userID = UUID.Zero, itemID = UUID.Zero;
            if (args.ContainsKey("userid") && args["userid"] != null)
                userID = args["userid"].AsUUID();
            if (args.ContainsKey("itemid") && args["itemid"] != null)
                itemID = args["itemid"].AsUUID();

            // This is the meaning of PUT object
            bool result = m_SimulationService.CreateObject(destination, userID, itemID);

            httpResponse.StatusCode = (int)HttpStatusCode.OK;
            return Encoding.UTF8.GetBytes(result.ToString());
        }
    }
}