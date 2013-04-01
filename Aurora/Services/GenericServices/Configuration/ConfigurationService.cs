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
using System.Collections.Generic;
using System.Linq;

namespace Aurora.Services
{
    /// <summary>
    ///     This is an application plugin so that it loads asap as it is used by many things (IService modules especially)
    /// </summary>
    public class ConfigurationService : IConfigurationService, IService
    {
        #region Declares

        protected Dictionary<string, string> m_urls = new Dictionary<string, string>();
        protected IConfigSource m_config;

        #endregion

        public virtual string Name
        {
            get { return GetType().Name; }
        }

        #region IConfigurationService Members

        public string FindValueOf(string key)
        {
            return m_urls[key];
        }

        public Dictionary<string, string> GetURIs()
        {
            return new Dictionary<string, string>(m_urls);
        }

        public void SetURIs(Dictionary<string, string> uris)
        {
            m_urls = uris;
        }

        #endregion

        public void Dispose()
        {
        }

        protected void FindConfiguration(IConfig autoConfig)
        {
            if (autoConfig == null)
                return;

            //Get the urls from the config
            foreach (string key in m_config.Configs["Configuration"].GetKeys().Where((k) => k.EndsWith("URI")))
                m_urls[key] = m_config.Configs["Configuration"].GetString(key);
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_config = config;

            IConfig handlerConfig = m_config.Configs["Handlers"];
            if (handlerConfig.GetString("ConfigurationHandler", "") != Name)
                return;

            //Register us
            registry.RegisterModuleInterface<IConfigurationService>(this);

            FindConfiguration(m_config.Configs["Configuration"]);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
        }
    }
}