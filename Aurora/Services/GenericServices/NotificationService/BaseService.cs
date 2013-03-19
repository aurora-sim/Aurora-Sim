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
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Repository;
using Nini.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Aurora.Services
{
    public class BaseService : IApplicationPlugin
    {
        protected static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected OpenSimAppender m_consoleAppender;

        #region IApplicationPlugin Members

        public string Name
        {
            get { return "BaseNotificationService"; }
        }

        public void PreStartup(ISimulationBase simBase)
        {
            SetUpConsole(simBase.ConfigSource, simBase.ApplicationRegistry);
        }

        public void Initialize(ISimulationBase simBase)
        {
        }

        private void SetUpConsole(IConfigSource config, IRegistryCore registry)
        {
            List<ICommandConsole> Plugins = AuroraModuleLoader.PickupModules<ICommandConsole>();
            foreach (ICommandConsole plugin in Plugins)
            {
                plugin.Initialize(config, registry.RequestModuleInterface<ISimulationBase>());
            }

            ILoggerRepository repository = LogManager.GetRepository();
            IAppender[] appenders = repository.GetAppenders();
            foreach (IAppender appender in appenders)
            {
                if (appender.Name == "Console")
                {
                    m_consoleAppender = (OpenSimAppender)appender;
                    break;
                }
            }

            if (null != m_consoleAppender)
            {
                m_consoleAppender.Console = MainConsole.Instance;
                // If there is no threshold set then the threshold is effectively everything.
                if (null == m_consoleAppender.Threshold)
                    m_consoleAppender.Threshold = Level.All;
                repository.Threshold = m_consoleAppender.Threshold;
                foreach (ILogger log in repository.GetCurrentLoggers())
                {
                    log.Level = m_consoleAppender.Threshold;
                }
            }
            IAppender logFileAppender = null;
            foreach (IAppender appender in appenders)
            {
                if (appender.Name == "LogFileAppender")
                {
                    logFileAppender = appender;
                }
            }

            if (logFileAppender != null)
            {
                if (logFileAppender is FileAppender)
                {
                    FileAppender appender = (FileAppender)logFileAppender;
                    IConfig startupConfig = config.Configs["Startup"];
                    string fileName = startupConfig.GetString("LogFile", String.Empty);
                    if (fileName != String.Empty)
                    {
                        appender.File = fileName;
                        appender.ActivateOptions();
                    }
                }
            }
            if (MainConsole.Instance == null)
            {
                m_log.Info("[Console]: No Console located");
                return;
            }

            MainConsole.Instance.MaxLogLevel = m_consoleAppender.Threshold;
            if (m_consoleAppender != null)
                MainConsole.Instance.Fatal(String.Format("[Console]: Console log level is {0}", m_consoleAppender.Threshold));

            MainConsole.Instance.Commands.AddCommand("set log level", "set log level [level]", "Set the console logging level", HandleLogLevel);

            MainConsole.Instance.Commands.AddCommand("get log level", "get log level", "Returns the current console logging level", HandleGetLogLevel);
        }

        public void PostInitialise()
        {
        }

        public void Start()
        {
        }

        public void PostStart()
        {
        }

        public void Close()
        {
        }

        public void ReloadConfiguration(IConfigSource m_config)
        {
        }

        #endregion

        #region Console Commands

        private void HandleGetLogLevel(string[] cmd)
        {
            MainConsole.Instance.Fatal(String.Format("Console log level is {0}", m_consoleAppender.Threshold));
        }

        private void HandleLogLevel(string[] cmd)
        {
            if (null == m_consoleAppender)
            {
                MainConsole.Instance.Fatal("No appender named Console found (see the log4net config file for this executable)!");
                return;
            }

            string rawLevel = cmd[3];

            ILoggerRepository repository = LogManager.GetRepository();
            Level consoleLevel = repository.LevelMap[rawLevel];
            if (consoleLevel != null)
            {
                m_consoleAppender.Threshold = consoleLevel;
                repository.Threshold = consoleLevel;
                foreach (ILogger log in repository.GetCurrentLoggers())
                {
                    log.Level = consoleLevel;
                }
            }
            else
            {
                string forms = "";
                for (int i = 0; i < repository.LevelMap.AllLevels.Count; i++)
                {
                    forms += repository.LevelMap.AllLevels[i].Name;
                    if (i + 1 != repository.LevelMap.AllLevels.Count)
                        forms += ", ";
                }
                MainConsole.Instance.Fatal(
                    String.Format(
                        "{0} is not a valid logging level.  Valid logging levels are " + forms,
                        rawLevel));
            }

            MainConsole.Instance.MaxLogLevel = m_consoleAppender.Threshold;
            MainConsole.Instance.Fatal(String.Format("Console log level is {0}", m_consoleAppender.Threshold));
        }

        #endregion
    }
}
