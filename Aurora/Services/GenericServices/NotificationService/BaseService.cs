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
using Aurora.Framework.ModuleLoader;
using Aurora.Framework.Modules;
using Aurora.Framework.Services;
using Nini.Config;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Aurora.Services
{
    public class BaseService : IApplicationPlugin
    {
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

            if (MainConsole.Instance == null)
            {
                Console.WriteLine("[Console]: No Console located");
                return;
            }

            MainConsole.Instance.Threshold = Level.Info;
            
            MainConsole.Instance.Fatal(String.Format("[Console]: Console log level is {0}",
                                                         MainConsole.Instance.Threshold));

            MainConsole.Instance.Commands.AddCommand("set log level", "set log level [level]",
                                                     "Set the console logging level", HandleLogLevel);

            MainConsole.Instance.Commands.AddCommand("get log level", "get log level",
                                                     "Returns the current console logging level", HandleGetLogLevel);
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
            MainConsole.Instance.Fatal(String.Format("Console log level is {0}", MainConsole.Instance.Threshold));
        }

        private void HandleLogLevel(string[] cmd)
        {
            string rawLevel = cmd[3];

            MainConsole.Instance.Threshold = (Level)Enum.Parse(typeof(Level), rawLevel);

            MainConsole.Instance.Fatal(String.Format("Console log level is {0}", MainConsole.Instance.Threshold));
        }

        #endregion
    }
}