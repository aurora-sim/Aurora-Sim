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
using Aurora.Framework.Servers;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Servers.HttpServer.Implementation;
using Aurora.Framework.Services;
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
        public string GridMapTileURI { get; set; }
        public string AgentAppearanceURI { get; set; }
        public string GridWebProfileURI { get; protected set; }
        public string GridSearchURI { get; protected set; }
        public string GridDestinationURI { get; protected set; }
        public string GridMarketplaceURI { get; protected set; }
        public string GridTutorialURI { get; protected set; }
        public string GridSnapshotConfigURI { get; protected set; }
        protected IConfigSource m_config;
        protected IRegistryCore m_registry;

        /// <summary>
        ///     Instantiate a GridInfoService object.
        /// </summary>
        /// <param name="configSource">path to config path containing grid information</param>
        /// <param name="registry"></param>
        /// <remarks>
        ///     GridInfoService uses the [GridInfo] section of the
        ///     standard Aurora.ini file --- which is not optimal, but
        ///     anything else requires a general redesign of the config
        ///     system.
        /// </remarks>
        public GridInfoHandlers(IConfigSource configSource, IRegistryCore registry)
        {
            m_config = configSource;
            m_registry = registry;
            UpdateGridInfo();
        }

        public void UpdateGridInfo()
        {
            IConfig gridCfg = m_config.Configs["GridInfoService"];
            if (gridCfg == null)
                return;
            _info["platform"] = "Aurora";
            try
            {
                IConfig configCfg = m_config.Configs["Handlers"];
                IWebInterfaceModule webInterface = m_registry.RequestModuleInterface<IWebInterfaceModule>();
                IMoneyModule moneyModule = m_registry.RequestModuleInterface<IMoneyModule>();
                IGridServerInfoService serverInfoService = m_registry.RequestModuleInterface<IGridServerInfoService>();

                GridEconomyURI = GetConfig(m_config, "economy");
                if (GridEconomyURI == "")
                {
                    if (moneyModule != null)
                    {
                        int port = moneyModule.ClientPort;
                        if (port == 0)
                            port = (int)MainServer.Instance.Port;
                        GridEconomyURI = MainServer.Instance.FullHostName + ":" + port + "/";
                    }
                    else
                        GridEconomyURI = MainServer.Instance.FullHostName + ":" + 9000 + "/"; //Fallback... we dunno
                }
                if (GridEconomyURI != "" && !GridEconomyURI.EndsWith("/"))
                    GridEconomyURI += "/";
                _info["economy"] = _info["helperuri"] = GridEconomyURI;

                GridLoginURI = GetConfig(m_config, "login");
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

                _info["welcome"] = GridWelcomeURI = GetConfig(m_config, "welcome");
                if (GridWelcomeURI == "" && webInterface != null)
                    _info["welcome"] = GridWelcomeURI = webInterface.LoginScreenURL;

                _info["register"] = GridRegisterURI = GetConfig(m_config, "register");
                if (GridRegisterURI == "" && webInterface != null)
                    _info["register"] = GridRegisterURI = webInterface.RegistrationScreenURL;

                _info["gridname"] = GridName = GetConfig(m_config, "gridname");
                _info["gridnick"] = GridNick = GetConfig(m_config, "gridnick");

                _info["about"] = GridAboutURI = GetConfig(m_config, "about");
                _info["help"] = GridHelpURI = GetConfig(m_config, "help");
                _info["password"] = GridForgotPasswordURI = GetConfig(m_config, "forgottenpassword");
                GridMapTileURI = GetConfig(m_config, "map");

                if (GridMapTileURI == "" && serverInfoService != null)
                    GridMapTileURI = serverInfoService.GetGridURI("MapService");
                AgentAppearanceURI = GetConfig(m_config, "AgentAppearanceURI");
                if (AgentAppearanceURI == "" && serverInfoService != null)
                    AgentAppearanceURI = serverInfoService.GetGridURI("SSAService");
                GridWebProfileURI = GetConfig(m_config, "webprofile");
                if (GridWebProfileURI == "" && webInterface != null)
                    GridWebProfileURI = webInterface.WebProfileURL;
                GridSearchURI = GetConfig(m_config, "search");
                GridDestinationURI = GetConfig(m_config, "destination");
                GridMarketplaceURI = GetConfig(m_config, "marketplace");
                GridTutorialURI = GetConfig(m_config, "tutorial");
                GridSnapshotConfigURI = GetConfig(m_config, "snapshotconfig");
            }
            catch (Exception)
            {
                MainConsole.Instance.Warn(
                    "[GRID INFO SERVICE]: Cannot get grid info from config source, using minimal defaults");
            }

            MainConsole.Instance.DebugFormat("[GRID INFO SERVICE]: Grid info service initialized with {0} keys",
                                             _info.Count);
        }

        private string GetConfig(IConfigSource config, string p)
        {
            IConfig gridCfg = config.Configs["GridInfoService"];
            return gridCfg.GetString(p, "");
        }

        private void IssueWarning()
        {
            MainConsole.Instance.Warn("[GRID INFO SERVICE]: found no [GridInfo] section in your configuration files");
            MainConsole.Instance.Warn(
                "[GRID INFO SERVICE]: trying to guess sensible defaults, you might want to provide better ones:");

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
            UpdateGridInfo();

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
            UpdateGridInfo();

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