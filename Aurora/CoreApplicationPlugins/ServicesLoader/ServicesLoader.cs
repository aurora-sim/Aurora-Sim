/*
 * Copyright (c) Contributors, http://aurora-sim.org/
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
using Nini.Config;
using System.Collections.Generic;

namespace Aurora.CoreApplicationPlugins
{
    public class ServicesLoader : IApplicationPlugin
    {
        private ISimulationBase m_openSim;

        #region IApplicationPlugin Members

        public void PreStartup(ISimulationBase simBase)
        {
        }

        public void Initialize(ISimulationBase openSim)
        {
            m_openSim = openSim;
        }

        public void ReloadConfiguration(IConfigSource config)
        {
        }

        public void PostInitialise()
        {
        }

        public void Start()
        {
            IConfig handlerConfig = m_openSim.ConfigSource.Configs["ApplicationPlugins"];
            if (handlerConfig.GetString("ServicesLoader", "") != Name)
                return;

            List<IService> serviceConnectors = AuroraModuleLoader.PickupModules<IService>();
            foreach (IService connector in serviceConnectors)
            {
                try
                {
                    connector.Initialize(m_openSim.ConfigSource, m_openSim.ApplicationRegistry);
                }
                catch
                {
                }
            }
            foreach (IService connector in serviceConnectors)
            {
                try
                {
                    connector.Start(m_openSim.ConfigSource, m_openSim.ApplicationRegistry);
                }
                catch
                {
                }
            }
            foreach (IService connector in serviceConnectors)
            {
                try
                {
                    connector.FinishedStartup();
                }
                catch
                {
                }
            }
        }

        public void PostStart()
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "ServicesLoader"; }
        }

        #endregion

        public void Dispose()
        {
        }
    }
}