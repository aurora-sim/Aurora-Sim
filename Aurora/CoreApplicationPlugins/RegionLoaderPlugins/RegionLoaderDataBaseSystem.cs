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
using System.Threading;
using System.Windows.Forms;
using Aurora.Modules.RegionLoader;
using Nini.Config;
using Aurora.DataManager;
using Aurora.Framework;

namespace OpenSim.ApplicationPlugins.RegionLoaderPlugin
{
    public class RegionLoaderDataBaseSystem : IRegionLoader
    {
        private ISimulationBase m_openSim;
        private IConfigSource m_configSource;
        private bool m_enabled = false;
        private bool m_default = false;
        private bool m_noGUI = false;

        public bool Enabled
        {
            get { return m_enabled; }
        }

        public bool Default
        {
            get { return m_default; }
        }

        public void Initialise(IConfigSource configSource, ISimulationBase openSim)
        {
            m_configSource = configSource;
            m_openSim = openSim;
            
            IConfig config = configSource.Configs["RegionStartup"];
            if (config != null)
            {
                m_enabled = config.GetBoolean(GetType().Name + "_Enabled", m_enabled);
                if (!m_enabled)
                    return;
                if (MainConsole.Instance != null)
                    MainConsole.Instance.Commands.AddCommand("open region manager", "open region manager", "Opens the region manager", OpenRegionManager);
                m_default = config.GetString("Default") == GetType().Name;

                //Add the console command if it is the default
                if (m_default)
                    if (MainConsole.Instance != null)
                        MainConsole.Instance.Commands.AddCommand ("create region", "create region", "Create a new region.", AddRegion);
            }
            IConfig startupconfig = configSource.Configs["Startup"];
            if (startupconfig != null)
                m_noGUI = startupconfig.GetBoolean("NoGUI", false);

            m_openSim.ApplicationRegistry.StackModuleInterface<IRegionLoader>(this);
        }

        public void Close()
        {
        }

        public string Name
        {
            get
            {
                return "Database Plugin";
            }
        }

        public RegionInfo[] LoadRegions()
        {
            //Grab old region files
            if(m_default)
                FindOldRegionFiles();

            IRegionInfoConnector conn = DataManager.RequestPlugin<IRegionInfoConnector>();
            if (conn == null)
                return null;
            RegionInfo[] infos = conn.GetRegionInfos(true);
            return infos.Length == 0 ? null : infos;
        }

        /// <summary>
        /// Creates a new region based on the parameters specified.   This will ask the user questions on the console
        /// </summary>
        /// <param name="cmd">0,1,region name, region XML file</param>
        public void AddRegion(string[] cmd)
        {
            try
            {
                if(m_noGUI)
                {
                    RegionLoaderFileSystem system = new RegionLoaderFileSystem ();
                    system.Initialise (m_configSource, m_openSim);
                    system.AddRegion (new string[0]);
                }
                else
                {
                    bool done = false, errored = false;
                    Thread t = new Thread(delegate()
                    {
                        try
                        {
                            RegionManager manager = new RegionManager(false, true, m_openSim);
                            Application.Run(manager);
                            done = true;
                        }
                        catch
                        {
                            errored = true;
                        }
                    });
                    t.SetApartmentState(ApartmentState.STA);
                    t.Start();
                    while (!done)
                        if (errored)
                            throw new Exception();
                        Thread.Sleep(100);
                }
            }
            catch
            {
                //Probably no winforms
                RegionLoaderFileSystem system = new RegionLoaderFileSystem ();
                system.Initialise (m_configSource, m_openSim);
                system.AddRegion (new string[0]);
            }
        }

        protected void OpenRegionManager(string[] cmdparams)
        {
            Thread t = new Thread(StartRegionManagerThread);
            t.SetApartmentState(ApartmentState.STA);
            t.Start ();
        }

        protected void StartRegionManagerThread()
        {
            try
            {
                RegionManager manager = new RegionManager(false, false, m_openSim);
                Application.Run(manager);
            }
            catch(Exception ex)
            {
                MainConsole.Instance.Output("Failed to start the region manager: " + ex);
            }
        }

        private void FindOldRegionFiles()
        {
            try
            {
                //Load the file loader and set it up and make sure that we pull any regions from it
                RegionLoaderFileSystem system = new RegionLoaderFileSystem();
                system.Initialise(m_configSource, m_openSim);
                RegionInfo[] regionsToConvert = system.InternalLoadRegions(true);
                if (regionsToConvert == null)
                    return;

                bool changed = false;
                //Now load all the regions into the database
                IRegionInfoConnector conn = DataManager.RequestPlugin<IRegionInfoConnector>();
                foreach (RegionInfo info in regionsToConvert)
                {
                    RegionInfo alreadyExists;
                    if ((alreadyExists = conn.GetRegionInfo (info.RegionID)) == null)
                    {
                        changed = true;
                        if (!info.UDPPorts.Contains (info.InternalEndPoint.Port))
                            info.UDPPorts.Add (info.InternalEndPoint.Port);
                        info.Disabled = false;
                        conn.UpdateRegionInfo (info);
                    }
                    else
                    {
                        //Update some atributes...
                        alreadyExists.RegionName = info.RegionName;
                        alreadyExists.RegionLocX = info.RegionLocX;
                        alreadyExists.RegionLocY = info.RegionLocY;
                        alreadyExists.RegionSizeX = info.RegionSizeX;
                        alreadyExists.RegionSizeY = info.RegionSizeY;
                        alreadyExists.Disabled = false;
                        if (!alreadyExists.UDPPorts.Contains (info.InternalEndPoint.Port))
                            alreadyExists.UDPPorts.Add (info.InternalEndPoint.Port);
                        conn.UpdateRegionInfo (alreadyExists);
                    }
                }

                //Make sure all the regions got saved
                bool foundAll = true;
                foreach (RegionInfo info in regionsToConvert)
                {
                    if (conn.GetRegionInfo(info.RegionID) == null)
                        foundAll = false;
                }
                //We found some new ones, they are all loaded
                if (foundAll && regionsToConvert.Length != 0 && changed)
                {
                    try
                    {
                        MessageBox.Show ("All region .ini and .xml files have been successfully converted to the new region loader style.");
                        MessageBox.Show ("To change your region settings, type 'open region manager' on the console, and a GUI will pop up for you to use.");
                        DialogResult t = Utilities.InputBox ("Remove .ini files", "Do you want to remove your old .ini files?");
                        if (t == DialogResult.OK)
                            system.DeleteAllRegionFiles ();
                    }
                    catch
                    {
                        //For people who only have consoles, no winforms
                        MainConsole.Instance.Output ("All region .ini and .xml files have been successfully converted to the new region loader style.");
                        MainConsole.Instance.Output ("To change your region settings, well, you don't have Mono-Winforms installed. Get that, stick with just modifying the .ini files, or get something to modify the region database that isn't a GUI.");
                    }
                }
            }
            catch
            {
            }
        }

        public void DeleteRegion(RegionInfo regionInfo)
        {
            IRegionInfoConnector connector = DataManager.RequestPlugin<IRegionInfoConnector>();
            if (connector != null)
            {
                connector.Delete(regionInfo);
            }
        }

        public bool FailedToStartRegions(string reason)
        {
            try
            {
                //Open the region manager for them
                MessageBox.Show (reason, "Startup failed, regions did not validate!");
                OpenRegionManager(new string[0]);
            }
            catch
            {
                MainConsole.Instance.Output(string.Format("Startup failed, regions did not validate - {0}!", reason));
            }
            return true;
        }

        public void Dispose()
        {
        }

        public void UpdateRegionInfo(string oldName, RegionInfo regionInfo)
        {
            IRegionInfoConnector connector = DataManager.RequestPlugin<IRegionInfoConnector>();
            if (connector != null)
            {
                //Make sure we have this region in the database
                if (connector.GetRegionInfo(oldName) == null)
                    return;
                RegionInfo copy = new RegionInfo();
                //Make an exact copy
                copy.UnpackRegionInfoData(regionInfo.PackRegionInfoData(true));

                //Fix the name of the region so we can delete the old one
                copy.RegionName = oldName;
                DeleteRegion(copy);
                //Now add the new one
                connector.UpdateRegionInfo(regionInfo);
            }
        }
    }
}
