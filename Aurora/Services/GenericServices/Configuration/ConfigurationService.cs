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
using Aurora.Framework.Modules;
using Aurora.Framework.Services;
using Nini.Config;
using OpenMetaverse.StructuredData;

namespace Aurora.Services
{
    /// <summary>
    ///     This is an application plugin so that it loads asap as it is used by many things (IService modules especially)
    /// </summary>
    public class ConfigurationService : IConfigurationService, IApplicationPlugin
    {
        #region Declares

        protected OSDMap m_urls = new OSDMap();
        protected IConfigSource m_config;

        #endregion

        #region IApplicationPlugin Members

        public void PreStartup(ISimulationBase simBase)
        {
        }

        public void Initialize(ISimulationBase openSim)
        {
            m_config = openSim.ConfigSource;

            IConfig handlerConfig = m_config.Configs["Handlers"];
            if (handlerConfig.GetString("ConfigurationHandler", "") != Name)
                return;

            //Register us
            openSim.ApplicationRegistry.RegisterModuleInterface<IConfigurationService>(this);

            FindConfiguration(m_config.Configs["Configuration"]);
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
            IConfig handlerConfig = m_config.Configs["Handlers"];
            if (handlerConfig.GetString("ConfigurationHandler", "") != Name)
                return;

            FindConfiguration(m_config.Configs["Configuration"]);
        }

        public virtual string Name
        {
            get { return GetType().Name; }
        }

        #endregion

        #region IConfigurationService Members

        public string FindValueOf(string key)
        {
            return m_urls[key].AsString();
        }

        #endregion

        public void Dispose()
        {
        }

        protected void FindConfiguration(IConfig autoConfig)
        {
            if (autoConfig == null)
                return;

            OSDMap request = new OSDMap();
            //Get the urls from the config
            GetConfigFor("AssetServerURI", request);
            GetConfigFor("AvatarServerURI", request);
            GetConfigFor("GridServerURI", request);
            GetConfigFor("InventoryServerURI", request);
            GetConfigFor("ServerURI", request);
            GetConfigFor("SyncMessageServerURI", request);
            GetConfigFor("InstantMessageServerURI", request);
            GetConfigFor("UserAccountServerURI", request);
            GetConfigFor("CurrencyServerURI", request);
            m_urls = request;
        }

        private void GetConfigFor(string name, OSDMap request)
        {
            request[name] = m_config.Configs["Configuration"].GetString(name, "");
        }
    }
}