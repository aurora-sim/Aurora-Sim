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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Timers;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository;
using log4net.Config;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Framework.Statistics;
using OpenSim.Region.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Physics.Manager;
using OpenSim.Server.Handlers.Base;
using Aurora.Framework;
using Aurora.Simulation.Base;

namespace Aurora.Server
{
    public class AuroraBase : SimulationBase
    {
        protected Dictionary<uint, BaseHttpServer> m_Servers =
            new Dictionary<uint, BaseHttpServer>();

        public IHttpServer GetHttpServer(uint port)
        {
            m_log.InfoFormat("[SERVER]: Requested port {0}", port);
            if (port == m_Port)
                return HttpServer;

            if (m_Servers.ContainsKey(port))
                return m_Servers[port];

            m_Servers[port] = new BaseHttpServer(port);

            m_log.InfoFormat("[SERVER]: Starting new HTTP server on port {0}", port);
            m_Servers[port].Start();

            return m_Servers[port];
        }

        public override void SetUpHTTPServer()
        {
            base.SetUpHTTPServer();
            m_Servers.Add(m_Port, m_BaseHTTPServer);
        }

        public override void Configuration(IConfigSource configSource)
        {
            IConfig startupConfig = m_config.Configs["Startup"];

            if (startupConfig != null)
            {
                m_startupCommandsFile = startupConfig.GetString("startup_console_commands_file", "startup_commands.txt");
                m_shutdownCommandsFile = startupConfig.GetString("shutdown_console_commands_file", "shutdown_commands.txt");

                m_TimerScriptFileName = startupConfig.GetString("timer_Script", "disabled");
                m_TimerScriptTime = startupConfig.GetInt("timer_time", m_TimerScriptTime);
                if (m_TimerScriptTime < 5) //Limit for things like backup and etc...
                    m_TimerScriptTime = 5;

                string pidFile = startupConfig.GetString("PIDFile", String.Empty);
                if (pidFile != String.Empty)
                    CreatePIDFile(pidFile);
            }
        }

        public override void SetUpConsole()
        {
            base.SetUpConsole();
            m_console.DefaultPrompt = "Aurora.Server ";
        }

        public override void StartModules()
        {
            IConfig startupConfig = m_config.Configs["Startup"];

            string connList = startupConfig.GetString("ServiceConnectors", String.Empty);
            string[] conns = connList.Split(new char[] { ',', ' ' });

            foreach (string c in conns)
            {
                if (c == String.Empty)
                    continue;

                string configName = String.Empty;
                string conn = c;
                uint port = 0;

                string[] split1 = conn.Split(new char[] { '/' });
                if (split1.Length > 1)
                {
                    conn = split1[1];

                    string[] split2 = split1[0].Split(new char[] { '@' });
                    if (split2.Length > 1)
                    {
                        configName = split2[0];
                        port = Convert.ToUInt32(split2[1]);
                    }
                    else
                    {
                        port = Convert.ToUInt32(split1[0]);
                    }
                }
                string[] parts = conn.Split(new char[] { ':' });
                string friendlyName = parts[0];
                if (parts.Length > 1)
                    friendlyName = parts[1];

                IHttpServer server = HttpServer;
                if (port != 0)
                    server = GetHttpServer(port);

                if (port != DefaultPort && port != 0)
                    m_log.InfoFormat("[SERVER]: Loading {0} on port {1}", friendlyName, port);
                else
                    m_log.InfoFormat("[SERVER]: Loading {0}", friendlyName);

                IServiceConnector connector = null;

                Object[] modargs = new Object[] { m_config, server,
                    configName };
                connector = ServerUtils.LoadPlugin<IServiceConnector>(conn,
                        modargs);
                if (connector == null)
                {
                    modargs = new Object[] { m_config, server };
                    connector =
                            ServerUtils.LoadPlugin<IServiceConnector>(conn,
                            modargs);
                }

                if (connector != null)
                {
                    //m_ServiceConnectors.Add(connector);
                    m_log.InfoFormat("[SERVER]: {0} loaded successfully", friendlyName);
                }
                else
                {
                    m_log.InfoFormat("[SERVER]: Failed to load {0}", conn);
                }
            }
        }

        /// <summary>
        /// Performs initialisation of the scene, such as loading configuration from disk.
        /// </summary>
        public override void Startup()
        {
            base.Startup();
            m_log.Info("[AURORASTARTUP]: Startup completed in " + (DateTime.Now - this.StartupTime).TotalSeconds);
		}
    }
}
