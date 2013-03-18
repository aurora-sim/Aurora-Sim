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
using Aurora.Framework.Servers.HttpServer;
using Nini.Config;
using Nwc.XmlRpc;
using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;

namespace Aurora.Services
{
    public class GridInfoHandlers : IGridInfo
    {
        private readonly Hashtable _info = new Hashtable();

        public string GridName { get; protected set; }
        public string GridNick { get; protected set; }
        public string GridLoginURI { get; protected set; }
        public string GridWelcomeURI { get; protected set; }
        public string GridEconomyURI { get; protected set; }
        public string GridAboutURI { get; protected set; }
        public string GridHelpURI { get; protected set; }
        public string GridRegisterURI { get; protected set; }
        public string GridForgotPasswordURI { get; protected set; }
        public string GridMapTileURI { get; protected set; }
        public string GridWebProfileURI { get; protected set; }
        public string GridSearchURI { get; protected set; }
        public string GridDestinationURI { get; protected set; }
        public string GridMarketplaceURI { get; protected set; }
        public string GridTutorialURI { get; protected set; }
        public string GridSnapshotConfigURI { get; protected set; }

        /// <summary>
        ///   Instantiate a GridInfoService object.
        /// </summary>
        /// <param name = "configSource">path to config path containing grid information</param>
        /// <param name = "registry"></param>
        /// <remarks>
        ///   GridInfoService uses the [GridInfo] section of the
        ///   standard Aurora.ini file --- which is not optimal, but
        ///   anything else requires a general redesign of the config
        ///   system.
        /// </remarks>
        public GridInfoHandlers(IConfigSource configSource, IRegistryCore registry)
        {
            IConfig gridCfg = configSource.Configs["GridInfoService"];
            if (gridCfg == null)
                return;
            _info["platform"] = "Aurora";
            try
            {
                IConfig configCfg = configSource.Configs["Handlers"];
                IWebInterfaceModule webInterface = registry.RequestModuleInterface<IWebInterfaceModule>();
                IMoneyModule moneyModule = registry.RequestModuleInterface<IMoneyModule>();

                GridEconomyURI = GetConfig(configSource, "economy");
                if (GridEconomyURI == "")
                {
                    if (moneyModule != null)
                        GridEconomyURI = MainServer.Instance.FullHostName + ":" + moneyModule.ClientPort + "/";
                    else
                        GridEconomyURI = MainServer.Instance.FullHostName + ":" + 9000 + "/";//Fallback... we dunno
                }
                if (GridEconomyURI != "" && !GridEconomyURI.EndsWith("/"))
                    GridEconomyURI += "/";
                _info["economy"] = _info["helperuri"] = GridEconomyURI;

                GridLoginURI = GetConfig(configSource, "login");
                if (GridLoginURI == "")
                {
                    if (configCfg != null && configCfg.GetString("LLLoginHandlerPort", "") != "")
                    {
                        var port = configCfg.GetString("LLLoginHandlerPort", "");
                        if (port == "" || port == "0")
                            port = MainServer.Instance.Port.ToString();
                        GridLoginURI = MainServer.Instance.FullHostName +
                            ":" + port + "/";
                    }
                    else
                    {
                        GridLoginURI = MainServer.Instance.ServerURI + "/";
                    }
                }
                else if (!GridLoginURI.EndsWith("/"))
                    GridLoginURI += "/";
                _info["login"] = GridLoginURI;

                _info["welcome"] = GridWelcomeURI = GetConfig(configSource, "welcome");
                if (GridWelcomeURI == "" && webInterface != null)
                    _info["welcome"] = GridWelcomeURI = webInterface.LoginScreenURL;

                _info["register"] = GridRegisterURI = GetConfig(configSource, "register");
                if (GridRegisterURI == "" && webInterface != null)
                    _info["register"] = GridRegisterURI = webInterface.RegistrationScreenURL;

                _info["gridname"] = GridName = GetConfig(configSource, "gridname");
                _info["gridnick"] = GridNick = GetConfig(configSource, "gridnick");

                _info["about"] = GridAboutURI = GetConfig(configSource, "about");
                _info["help"] = GridHelpURI = GetConfig(configSource, "help");
                _info["password"] = GridForgotPasswordURI = GetConfig(configSource, "forgottenpassword");
                GridMapTileURI = GetConfig(configSource, "map");
                IMapService mapService = registry.RequestModuleInterface<IMapService>();
                if (GridMapTileURI == "" && mapService != null)
                    GridMapTileURI = mapService.MapServiceURL;
                GridWebProfileURI = GetConfig(configSource, "webprofile");
                if (GridWebProfileURI == "" && webInterface != null)
                    GridWebProfileURI = webInterface.WebProfileURL;
                GridSearchURI = GetConfig(configSource, "search");
                GridDestinationURI = GetConfig(configSource, "destination");
                GridMarketplaceURI = GetConfig(configSource, "marketplace");
                GridTutorialURI = GetConfig(configSource, "tutorial");
                GridSnapshotConfigURI = GetConfig(configSource, "snapshotconfig");
            }
            catch (Exception)
            {
                MainConsole.Instance.Warn("[GRID INFO SERVICE]: Cannot get grid info from config source, using minimal defaults");
            }

            MainConsole.Instance.DebugFormat("[GRID INFO SERVICE]: Grid info service initialized with {0} keys", _info.Count);
        }

        private string GetConfig(IConfigSource config, string p)
        {
            IConfig gridCfg = config.Configs["GridInfoService"];
            return gridCfg.GetString(p, "");
        }

        private void IssueWarning()
        {
            MainConsole.Instance.Warn("[GRID INFO SERVICE]: found no [GridInfo] section in your configuration files");
            MainConsole.Instance.Warn("[GRID INFO SERVICE]: trying to guess sensible defaults, you might want to provide better ones:");

            foreach (string k in _info.Keys)
            {
                MainConsole.Instance.WarnFormat("[GRID INFO SERVICE]: {0}: {1}", k, _info[k]);
            }
        }

        public XmlRpcResponse XmlRpcGridInfoMethod(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            MainConsole.Instance.Debug("[GRID INFO SERVICE]: Request for grid info");

            foreach (string k in _info.Keys)
            {
                responseData[k] = _info[k];
            }
            response.Value = responseData;

            return response;
        }

        public byte[] RestGetGridInfoMethod(string path, Stream request, OSHttpRequest httpRequest,
                                                            OSHttpResponse httpResponse)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("<gridinfo>\n");
            foreach (string k in _info.Keys)
            {
                sb.AppendFormat("<{0}>{1}</{0}>\n", k, _info[k]);
            }
            sb.Append("</gridinfo>\n");

            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}