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
using OpenSim;
using OpenSim.Framework.Console;
using Aurora.Modules.RegionLoader;
using Aurora.Framework;

namespace OpenSim.ApplicationPlugins.RegionLoaderPlugin
{
    public class LoadRegionsPlugin : IApplicationPlugin, IRegionCreator
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public event NewRegionCreated OnNewRegionCreated;
        private NewRegionCreated m_newRegionCreatedHandler;

        #region IApplicationPlugin Members

        private string m_name = "LoadRegionsPlugin";
        public string Name
        {
            get { return m_name; }
        }

        protected OpenSimBase m_openSim;

        public void Initialize(IOpenSimBase openSim)
        {
            m_openSim = (OpenSimBase)openSim;
            MainConsole.Instance.Commands.AddCommand("base", false, "open region manager", "open region manager", "Opens the region manager", OpenRegionManager);
            m_openSim.ApplicationRegistry.RegisterInterface<IRegionCreator>(this);
        }

        protected void OpenRegionManager(string module, string[] cmdparams)
        {
            System.Threading.Thread thread = new Thread(StartRegionManagerThread);
            thread.Start();
        }

        protected void StartRegionManagerThread()
        {
            RegionManager manager = new RegionManager(false, m_openSim);
            System.Windows.Forms.Application.Run(manager);
        }

        public void PostInitialise()
        {
            List<IRegionLoader> regionLoaders = AuroraModuleLoader.PickupModules<IRegionLoader>();
            foreach (IRegionLoader loader in regionLoaders)
            {
                loader.Initialise(m_openSim.ConfigSource, this, m_openSim);
                m_log.Info("[LOADREGIONSPLUGIN]: Loading region configurations from " + loader.Name + "...");

                RegionInfo[] regionsToLoad = loader.LoadRegions();
                if (regionsToLoad == null)
                    continue;

                if (!CheckRegionsForSanity(regionsToLoad))
                {
                    m_log.Error("[LOADREGIONS]: Halting startup due to conflicts in region configurations");
                    throw new Exception();
                }

                m_openSim.SceneManager.AllRegions += regionsToLoad.Length;
                Util.NumberofScenes += regionsToLoad.Length;

                for (int i = 0; i < regionsToLoad.Length; i++)
                {
                    IScene scene = null;
                    m_log.Debug("[LOADREGIONS]: Creating Region: " + regionsToLoad[i].RegionName);
                    m_openSim.SceneManager.CreateRegion(regionsToLoad[i], true, out scene);
                    if (scene != null)
                    {
                        m_newRegionCreatedHandler = OnNewRegionCreated;
                        if (m_newRegionCreatedHandler != null)
                        {
                            m_newRegionCreatedHandler(scene);
                        }
                    }
                }
            }
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
        private bool CheckRegionsForSanity(RegionInfo[] regions)
        {
            if (regions.Length <= 1)
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
                        return false;
                    }
                    else if (
                        regions[i].RegionLocX == regions[j].RegionLocX && regions[i].RegionLocY == regions[j].RegionLocY)
                    {
                        m_log.ErrorFormat(
                            "[LOADREGIONS]: Regions {0} and {1} have the same grid location ({2}, {3})",
                            regions[i].RegionName, regions[j].RegionName, regions[i].RegionLocX, regions[i].RegionLocY);
                        return false;
                    }
                    else if (regions[i].InternalEndPoint.Port == regions[j].InternalEndPoint.Port)
                    {
                        m_log.ErrorFormat(
                            "[LOADREGIONS]: Regions {0} and {1} have the same internal IP port {2}",
                            regions[i].RegionName, regions[j].RegionName, regions[i].InternalEndPoint.Port);
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
