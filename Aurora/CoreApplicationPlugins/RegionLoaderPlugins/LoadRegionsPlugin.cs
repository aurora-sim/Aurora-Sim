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

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using log4net;
using OpenSim.Framework;
using OpenSim.Region.CoreModules.Agent.AssetTransaction;
using OpenSim.Region.CoreModules.Avatar.InstantMessage;
using OpenSim.Region.CoreModules.Scripting.DynamicTexture;
using OpenSim.Region.CoreModules.Scripting.LoadImageURL;
using OpenSim.Region.CoreModules.Scripting.XMLRPC;
using Aurora.Modules.RegionLoader;
using Aurora.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using Nini.Config;

namespace OpenSim.CoreApplicationPlugins
{
    public class LoadRegionsPlugin : IApplicationPlugin
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected ISimulationBase m_openSim;
        public bool Enabled = true;

        #region IApplicationPlugin Members

        private string m_name = "LoadRegionsPlugin";
        public string Name
        {
            get { return m_name; }
        }


        public void Initialize(ISimulationBase openSim)
        {
            m_openSim = openSim;
            openSim.ApplicationRegistry.RegisterModuleInterface<LoadRegionsPlugin>(this);
        }

        public void ReloadConfiguration(IConfigSource config)
        {
        }

        public void PostInitialise()
        {
        }

        public void Start()
        {
        }

        public void PostStart()
        {
            IConfig handlerConfig = m_openSim.ConfigSource.Configs["ApplicationPlugins"];
            if (handlerConfig.GetString("LoadRegionsPlugin", "") != Name || !Enabled)
                return;

            List<IRegionLoader> regionLoaders = AuroraModuleLoader.PickupModules<IRegionLoader>();
            List<RegionInfo[]> regions = new List<RegionInfo[]>();
            SceneManager manager = m_openSim.ApplicationRegistry.RequestModuleInterface<SceneManager>();
            foreach (IRegionLoader loader in regionLoaders)
            {
                loader.Initialise(m_openSim.ConfigSource, m_openSim);

                if (!loader.Enabled)
                    continue;

                m_log.Info("[LoadRegionsPlugin]: Checking for region configurations from " + loader.Name + " plugin...");
                RegionInfo[] regionsToLoad = loader.LoadRegions();
                if (regionsToLoad == null)
                    continue; //No regions, end for this module

                string reason;
                if (!CheckRegionsForSanity(regionsToLoad, out reason))
                {
                    m_log.Error("[LoadRegionsPlugin]: Halting startup due to conflicts in region configurations");
                    if (!loader.FailedToStartRegions(reason))
                        throw new Exception(); //If it doesn't fix it, end the program
                }
                else
                {
                    //They are sanitized, load them
                    manager.AllRegions += regionsToLoad.Length;
                    regions.Add(regionsToLoad);
                }
            }
            foreach (RegionInfo[] regionsToLoad in regions)
            {
                for (int i = 0; i < regionsToLoad.Length; i++)
                {
                    m_log.Info("[LoadRegionsPlugin]: Creating Region: " + regionsToLoad[i].RegionName);
                    manager.CreateRegion(regionsToLoad[i]);
                }
            }
        }

        public void Close()
        {
        }

        public void Dispose()
        {
        }

        #endregion

        /// <summary>
        /// Check that region configuration information makes sense.
        /// </summary>
        /// <param name="regions"></param>
        /// <returns>True if we're sane, false if we're insane</returns>
        private bool CheckRegionsForSanity(RegionInfo[] regions, out string reason)
        {
            reason = "";
            if (regions.Length < 1)
                return true;

            for (int i = 0; i < regions.Length - 1; i++)
            {
                for (int j = i + 1; j < regions.Length; j++)
                {
                    if (regions[i].RegionID == regions[j].RegionID)
                    {
                        m_log.ErrorFormat(
                            "[LOADREGIONS]: Regions {0} and {1} have the same UUID {2}",
                            regions[i].RegionName, regions[j].RegionName, regions[i].RegionID);
                        reason = "Same UUID for regions " + regions[i].RegionName + ", " + regions[j].RegionName;
                        return false;
                    }
                    else if (
                        regions[i].RegionLocX == regions[j].RegionLocX && regions[i].RegionLocY == regions[j].RegionLocY)
                    {
                        m_log.ErrorFormat(
                            "[LOADREGIONS]: Regions {0} and {1} have the same grid location ({2}, {3})",
                            regions[i].RegionName, regions[j].RegionName, regions[i].RegionLocX, regions[i].RegionLocY);
                        reason = "Same grid location for regions " + regions[i].RegionName + ", " + regions[j].RegionName;
                        return false;
                    }
                    else if (regions[i].InternalEndPoint.Port == regions[j].InternalEndPoint.Port)
                    {
                        m_log.ErrorFormat(
                            "[LOADREGIONS]: Regions {0} and {1} have the same internal IP port {2}",
                            regions[i].RegionName, regions[j].RegionName, regions[i].InternalEndPoint.Port);
                        reason = "Same internal end point for regions " + regions[i].RegionName + ", " + regions[j].RegionName;
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
