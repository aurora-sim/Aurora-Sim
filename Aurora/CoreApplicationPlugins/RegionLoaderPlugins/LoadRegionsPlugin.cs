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
using System.Linq;
using System.Reflection;
using Aurora.Framework;
using Nini.Config;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.CoreApplicationPlugins
{
    public class LoadRegionsPlugin : IApplicationPlugin
    {
        public bool Enabled = true;

        private const string m_name = "LoadRegionsPlugin";
        protected ISimulationBase m_openSim;

        #region IApplicationPlugin Members

        public string Name
        {
            get { return m_name; }
        }

        public void PreStartup(ISimulationBase simBase)
        {
        }

        public void Initialize(ISimulationBase openSim)
        {
            m_openSim = openSim;
            openSim.ApplicationRegistry.RegisterModuleInterface(this);
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
            ISceneManager manager = m_openSim.ApplicationRegistry.RequestModuleInterface<ISceneManager>();
            MainConsole.Instance.DefaultPrompt = "Region (root)";//Set this up
        reload:
            List<RegionInfo[]> regions = new List<RegionInfo[]>();
            foreach (IRegionLoader loader in regionLoaders)
            {
                loader.Initialise(m_openSim.ConfigSource, m_openSim);

                if (!loader.Enabled)
                    continue;

                MainConsole.Instance.Info("[LoadRegions]: Checking for regions from " + loader.Name + "");
                RegionInfo[] regionsToLoad = loader.LoadRegions();
                if (regionsToLoad == null)
                    continue; //No regions, end for this module

                string reason;
                if (!CheckRegionsForSanity(regionsToLoad, out reason))
                {
                    MainConsole.Instance.Error("[LoadRegions]: Halting startup due to conflicts in region configurations");
                    if (!loader.FailedToStartRegions(reason))
                    {
                        bool foundSomeRegions = false;
                        foreach (IRegionLoader l in regionLoaders)
                        {
                            l.Initialise(m_openSim.ConfigSource, m_openSim);

                            if (!loader.Enabled)
                                continue;

                            RegionInfo[] rs = loader.LoadRegions();
                            if (rs == null)
                                continue; //No regions, end for this module
                            if (CheckRegionsForSanity(regionsToLoad, out reason))
                            {
                                foundSomeRegions = true;
                                break;
                            }
                        }
                        if(!foundSomeRegions)
                            throw new Exception(); //If it doesn't fix it, and we don't have regions now, end the program
                    }
                    goto reload;
                }
                else
                {
                    //They are sanitized, load them
                    manager.AllRegions += regionsToLoad.Length;
                    regions.Add(regionsToLoad);
                }
            }
            while (regions.Count == 0)
            {
                foreach (IRegionLoader loader in regionLoaders)
                {
                    if (loader.Default && loader.Enabled)
                    {
                        loader.AddRegion(new string[0]);
                        goto reload;
                    }
                }
            }
#if (!ISWIN)
            foreach (RegionInfo[] regionsToLoad in regions)
                foreach (RegionInfo r in regionsToLoad)
                {
                    RegionInfo reg = r;
                    //System.Threading.Thread t = new System.Threading.Thread(delegate()
                    //    {
                            manager.StartNewRegion(reg);
                    //    });
                    //t.Start();
                }
#else
            foreach (RegionInfo t in regions.SelectMany(regionsToLoad => regionsToLoad))
            {
                manager.StartNewRegion(t);
            }
#endif
        }

        public void Close()
        {
        }

        #endregion

        public void Dispose()
        {
        }

        /// <summary>
        ///   Check that region configuration information makes sense.
        /// </summary>
        /// <param name = "regions"></param>
        /// <param name="reason"></param>
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
                        MainConsole.Instance.ErrorFormat(
                            "[LoadRegion]: Regions {0} and {1} have the same UUID {2}",
                            regions[i].RegionName, regions[j].RegionName, regions[i].RegionID);
                        reason = "Same UUID for regions " + regions[i].RegionName + ", " + regions[j].RegionName;
                        return false;
                    }
                    if (
                        regions[i].RegionLocX == regions[j].RegionLocX && regions[i].RegionLocY == regions[j].RegionLocY)
                    {
                        MainConsole.Instance.ErrorFormat(
                            "[LoadRegion]: Regions {0} and {1} have the same grid location ({2}, {3})",
                            regions[i].RegionName, regions[j].RegionName, regions[i].RegionLocX, regions[i].RegionLocY);
                        reason = "Same grid location for regions " + regions[i].RegionName + ", " +
                                 regions[j].RegionName;
                        return false;
                    }
                    if (regions[i].InternalEndPoint.Port == regions[j].InternalEndPoint.Port)
                    {
                        MainConsole.Instance.ErrorFormat(
                            "[LoadRegion]: Regions {0} and {1} have the same internal IP port {2}",
                            regions[i].RegionName, regions[j].RegionName, regions[i].InternalEndPoint.Port);
                        reason = "Same internal end point for regions " + regions[i].RegionName + ", " +
                                 regions[j].RegionName;
                        return false;
                    }
                }
            }

            return true;
        }
    }
}