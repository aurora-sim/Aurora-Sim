using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Aurora.Framework;
using Aurora.Simulation.Base;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Repository;
using Nini.Config;
using log4net;

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
