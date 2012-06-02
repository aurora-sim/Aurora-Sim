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
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Aurora.Framework;
using OpenSim.Services.Interfaces;
using Nini.Config;
using Nini.Ini;
using OpenMetaverse;

namespace Aurora.Management
{
    public partial class RegionManager : Form
    {
        public delegate void NewRegion(RegionInfo info);
        public delegate void NoOp();
        public event NewRegion OnNewRegion;
        private readonly bool KillAfterRegionCreation = false;
        private UUID CurrentRegionID = UUID.Zero;
        private UUID _CurrentEstateRegionSelectedID = UUID.Zero;

        private IConfigSource _config;
        private bool _changingRegion = false;
        private bool _textHasChanged = false;
        private readonly IRegionManagement _regionManager;

        private readonly System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer();
        private readonly List<NoOp> _timerEvents = new List<NoOp>();

        public static void StartAsynchronously(bool killWindowOnRegionCreation, bool openCreatePageFirst, IConfigSource config, IRegionManagement regionManagement)
        {
            Thread t = new Thread(delegate()
            {
                try
                {
                    RegionManager manager = new RegionManager(killWindowOnRegionCreation, openCreatePageFirst, config, regionManagement);
                    Application.Run(manager);
                }
                catch { }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }

        public static void StartSynchronously(bool killWindowOnRegionCreation, bool openCreatePageFirst, IConfigSource config, IRegionManagement regionManagement)
        {
            bool done = false;
            Thread t = new Thread(delegate()
                {
                    try
                    {
                        RegionManager manager = new RegionManager(killWindowOnRegionCreation, openCreatePageFirst, config, regionManagement);
                        Application.Run(manager);
                        done = true;
                    }
                    catch { done = true; }
                });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            while (!done)
                Thread.Sleep(100);
        }

        public RegionManager(bool killWindowOnRegionCreation, bool openCreatePageFirst, IConfigSource config, IRegionManagement regionManagement)
        {
            _regionManager = regionManagement;
            _config = config;
            KillAfterRegionCreation = killWindowOnRegionCreation;
            InitializeComponent();
            if (openCreatePageFirst)
                tabControl1.SelectedTab = tabPage2;
            CStartupType.SelectedIndex = 1;
            RefreshCurrentRegions();
            GetDefaultRegions ();
            _timer.Interval = 100;
            _timer.Tick += m_timer_Tick;
            _timer.Start ();
        }

        void m_timer_Tick (object sender, EventArgs e)
        {
            lock (_timerEvents)
            {
                foreach (NoOp o in _timerEvents)
                    o ();
                _timerEvents.Clear ();
            }
        }

        private void RefreshCurrentRegions()
        {
            List<RegionInfo> infos = _regionManager.GetRegionInfos(false);
            infos.Sort (delegate (RegionInfo a, RegionInfo b)
            {
                if (!a.Disabled || !b.Disabled)
                    return a.Disabled.CompareTo(b.Disabled);//At the top
                return a.RegionName.CompareTo (b.RegionName);
            });
            RegionListBox.Items.Clear();
            estateRegionSelection.Items.Clear();
            foreach(RegionInfo r in infos)
            {
                bool online = _regionManager.GetWhetherRegionIsOnline(r.RegionID);
                RegionListBox.Items.Add(online ? "Online - " + r.RegionName : r.RegionName);
                estateRegionSelection.Items.Add(r.RegionName);
            }
        }

        private void CreateNewRegion(object sender, EventArgs e)
        {
            if (RName.Text == "")
            {
                MessageBox.Show("You must enter a region name!");
                return;
            }
            RegionInfo region = new RegionInfo
                                    {
                                        RegionName = RName.Text,
                                        RegionID = UUID.Random(),
                                        RegionLocX = int.Parse(LocX.Text)*Constants.RegionSize,
                                        RegionLocY = int.Parse(LocY.Text)*Constants.RegionSize
                                    };

            IPAddress address = IPAddress.Parse("0.0.0.0");
            string[] ports = Port.Text.Split (',');
            
            foreach (string port in ports)
            {
                string tPort = port.Trim ();
                int iPort = 0;
                if (int.TryParse (tPort, out iPort))
                    region.UDPPorts.Add (iPort);
            }
            region.InternalEndPoint = new IPEndPoint (address, region.UDPPorts[0]);

            region.RegionType = Type.Text;
            region.ObjectCapacity = int.Parse(ObjectCount.Text);
            int maturityLevel = 0;
            if (!int.TryParse(Maturity.Text, out maturityLevel))
            {
                if (Maturity.Text == "Adult")
                    maturityLevel = 2;
                else if (Maturity.Text == "Mature")
                    maturityLevel = 1;
                else //Leave it as PG by default if they do not select a valid option
                    maturityLevel = 0;
            }
            region.RegionSettings.Maturity = maturityLevel;
            region.Disabled = DisabledEdit.Checked;
            region.RegionSizeX = int.Parse(CRegionSizeX.Text);
            region.RegionSizeY = int.Parse(CRegionSizeY.Text);
            if ((region.RegionSizeX % Constants.MinRegionSize) != 0 ||
                (region.RegionSizeY % Constants.MinRegionSize) != 0)
            {
                MessageBox.Show ("You must enter a valid region size (multiple of " + Constants.MinRegionSize + "!");
                return;
            }
            region.NumberStartup = int.Parse (CStartNum.Text);
            region.Startup = ConvertIntToStartupType(CStartupType.SelectedIndex);
            region.InfiniteRegion = cInfiniteRegion.Checked;

            _regionManager.UpdateRegionInfo(region);
            CopyOverDefaultRegion (region.RegionName);
            if (KillAfterRegionCreation)
            {
                Application.Exit();
                return;
            }
            if(MainConsole.Instance != null)
                MainConsole.Instance.Info("[LOADREGIONS]: Creating Region: " + region.RegionName + ")");
            _regionManager.StartNewRegion(region);
            RefreshCurrentRegions();
        }

        private void SearchForRegionByName_Click(object sender, EventArgs e)
        {
            RegionInfo region = _regionManager.GetRegionInfo(RegionToFind.Text);
            if (region == null)
            {
                MessageBox.Show("Region was not found!");
                return;
            }
            ChangeRegionInfo (region);
        }

        private void ChangeRegionInfo (RegionInfo region)
        {
            _changingRegion = true;
            if (region == null)
            {
                button20.Enabled = false;
                CurrentRegionID = UUID.Zero;
                _changingRegion = false;
                textBox11.Text = "";
                textBox6.Text = ""; 
                textBox4.Text = "";
                DisabledEdit.Checked = false;
                textBox7.Text = "";
                textBox3.Text = "";
                textBox5.Text = "";
                textBox1.Text = "";
                RegionSizeX.Text = "";
                RegionSizeY.Text = "";
                StartupNumberBox.Text = "0";
                startupType.SelectedIndex = 0;
                einfiniteRegion.Checked = false;
                return;
            }
            button20.Enabled = true;
            CurrentRegionID = region.RegionID;
            textBox11.Text = region.RegionType;
            textBox6.Text = region.ObjectCapacity.ToString ();
            uint maturityLevel = Util.ConvertAccessLevelToMaturity (region.AccessLevel);
            if (maturityLevel == 0)
                textBox4.Text = "PG";
            else if (maturityLevel == 1)
                textBox4.Text = "Mature";
            else
                textBox4.Text = "Adult";
            DisabledEdit.Checked = region.Disabled;
#if (!ISWIN)
            textBox7.Text = string.Join(", ", region.UDPPorts.ConvertAll<string>(delegate(int i) { return i.ToString(); }).ToArray());
#else
            textBox7.Text = string.Join (", ", region.UDPPorts.ConvertAll (i => i.ToString()).ToArray ());
#endif
            textBox3.Text = (region.RegionLocX / Constants.RegionSize).ToString ();
            textBox5.Text = (region.RegionLocY / Constants.RegionSize).ToString ();
            textBox1.Text = region.RegionName;
            RegionSizeX.Text = region.RegionSizeX.ToString ();
            RegionSizeY.Text = region.RegionSizeY.ToString ();
            startupType.SelectedIndex = ConvertStartupType (region.Startup);
            einfiniteRegion.Checked = region.InfiniteRegion;
            StartupNumberBox.Text = region.NumberStartup.ToString();
            if (_regionManager.GetWhetherRegionIsOnline(region.RegionID))
                SetOnlineStatus ();
            else
                SetOfflineStatus ();

            _changingRegion = false;
        }

        private void SetOfflineStatus ()
        {
            _timerEvents.Add (delegate
                                   {
                RegionStatus.Text = "Offline";
                RegionStatus.BackColor = Color.Red;
                putOnline.Enabled = true;
                takeOffline.Enabled = false;
                resetRegion.Enabled = false;
                deleteRegion.Enabled = true;
            });
        }

        private void SetOnlineStatus ()
        {
            _timerEvents.Add (delegate
                                   {
                RegionStatus.Text = "Online";
                RegionStatus.BackColor = Color.SpringGreen;
                putOnline.Enabled = false;
                takeOffline.Enabled = true;
                resetRegion.Enabled = true;
                deleteRegion.Enabled = true;
            });
        }

        private void RefreshCurrentRegionsThreaded()
        {
            _timerEvents.Add (delegate
            {
                RefreshCurrentRegions();
            });
        }

        private void SetStoppingStatus ()
        {
            _timerEvents.Add (delegate
                                   {
                RegionStatus.Text = "Stopping";
                RegionStatus.BackColor = Color.LightPink;
                putOnline.Enabled = false;
                takeOffline.Enabled = false;
                resetRegion.Enabled = false;
                deleteRegion.Enabled = true;
            });
        }

        private void SetStartingStatus ()
        {
            _timerEvents.Add (delegate
                                   {
                RegionStatus.Text = "Starting";
                RegionStatus.BackColor = Color.LightGreen;
                putOnline.Enabled = false;
                takeOffline.Enabled = false;
                resetRegion.Enabled = false;
                deleteRegion.Enabled = true;
            });
        }

        private new void Update()
        {
            if(RegionListBox.SelectedIndex < 0)
                return;
            object item = RegionListBox.Items[RegionListBox.SelectedIndex];
            if(item == null)
            {
                MessageBox.Show("Select a valid region from the list.");
                return;
            }
            if (item.ToString().StartsWith("Online - "))
                item = item.ToString().Remove(0, 9);
            RegionInfo region = _regionManager.GetRegionInfo(item.ToString());
            if (region == null)
            {
                MessageBox.Show("You must enter a valid region name!");
                return;
            }
            string oldRegionName = region.RegionName;
            bool listNeedsUpdated = oldRegionName != textBox1.Text;
            region.RegionName = textBox1.Text;
            region.RegionID = CurrentRegionID;
            region.RegionLocX = int.Parse(textBox3.Text) * Constants.RegionSize;
            region.RegionLocY = int.Parse(textBox5.Text) * Constants.RegionSize;

            IPAddress address = IPAddress.Parse ("0.0.0.0");
            string[] ports = textBox7.Text.Split (',');

            region.UDPPorts.Clear ();
            foreach (string port in ports)
            {
                string tPort = port.Trim ();
                int iPort = 0;
                if (int.TryParse (tPort, out iPort))
                    region.UDPPorts.Add (iPort);
            }
            region.InternalEndPoint = new IPEndPoint (address, region.UDPPorts[0]);

            region.RegionType = textBox11.Text;
            region.ObjectCapacity = int.Parse(textBox6.Text);
            int maturityLevel = 0;
            if (!int.TryParse(Maturity.Text, out maturityLevel))
            {
                if (Maturity.Text == "Adult")
                    maturityLevel = 2;
                else if (Maturity.Text == "Mature")
                    maturityLevel = 1;
                else //Leave it as PG by default if they do not select a valid option
                    maturityLevel = 0;
            }
            region.RegionSettings.Maturity = maturityLevel;
            region.Disabled = DisabledEdit.Checked;
            region.NumberStartup = int.Parse(StartupNumberBox.Text);
            region.RegionSizeX = int.Parse(RegionSizeX.Text);
            region.RegionSizeY = int.Parse(RegionSizeY.Text);
            region.Startup = ConvertIntToStartupType(startupType.SelectedIndex);
            region.InfiniteRegion = einfiniteRegion.Checked;

            if ((region.RegionSizeX % Constants.MinRegionSize) != 0 || 
                (region.RegionSizeY % Constants.MinRegionSize) != 0)
            {
                MessageBox.Show("You must enter a valid region size (multiple of " + Constants.MinRegionSize + "!");
                return;
            }

            _regionManager.UpdateRegionInfo(region);
            if (OnNewRegion != null)
                OnNewRegion(region);
            if(listNeedsUpdated)
                RefreshCurrentRegions();
            RegionListBox.SelectedItem = region.RegionName;
        }

        private void RegionListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(RegionListBox.SelectedIndex < 0)
                return;
            string item = RegionListBox.Items[RegionListBox.SelectedIndex].ToString();
            if(item.StartsWith("Online - "))
                item = item.Remove(0, 9);
            RegionInfo region = _regionManager.GetRegionInfo(item);
            if (region == null)
            {
                MessageBox.Show("Region was not found!");
                return;
            }
            ChangeRegionInfo (region);
            RegionManager_Load(null, new EventArgs());
        }

        private int ConvertStartupType (StartupType startupType2)
        {
            return startupType2 == StartupType.Normal ? 0 : 1;
        }

        private StartupType ConvertIntToStartupType (int i)
        {
            return i == 1 ? StartupType.Medium : StartupType.Normal;
        }

        private void RegionNameHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This is the name of your region.");
        }

        private void RegionLocationX_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This is the X (in the X,Y corrdinate plane) of the location of your region.");
        }

        private void RegionLocationY_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This is the Y (in the X,Y corrdinate plane) of the location of your region.");
        }

        private void RegionPort_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This are the ports that your region will run on. Put a comma between multiple ports to have more than one.");
        }

        private void ExternalIPHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This is your external IP or DNS hostname. \n"
                + "Note: Use 'DEFAULT' (without the quotes) to have the IP automatically found");
        }

        private void RegionTypeHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This is the type of region you are running. It is shown in the client in parcel info and Region/Estate.");
        }

        private void MaxPrimsHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This is the maxiumum number of prims you can have in the region.");
        }

        private void MaturityHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This is the maturity level for the region. You can choose from: PG, Mature, Adult.");
        }

        private void DisabledHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show("If this is set to 'true', the region is not loaded.");
        }

        private void button12_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This determines the order that your regions are started.");
        }

        private void RSizeXHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This is the size of the region in the X (width, west to east) direction.");
        }

        private void RSizeYHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This is the size of the region in the Y (height, north to south) direction.");
        }

        private void startupType_Click (object sender, EventArgs e)
        {
            MessageBox.Show (@"This determines the type of startup the region will use. There are a few options:
'None', 'Soft', 'Medium', and 'Normal'.
--Normal loads your region at startup, while both soft and medium do not. 
--None loads only the basics, no terrain, no parcels, no prims.
--Soft only loads parcels and terrain on startup (not prims). 
--Medium does the same as Soft, except that it loads the prims as well.
Note: Neither 'None' nor 'Soft' nor 'Medium' start the heartbeats immediately.");
        }

        private void InfiniteRegion_Click (object sender, EventArgs e)
        {
            MessageBox.Show("This disables the borders around the region, so that you can leave the boundries of the region and go into the 'void'.");
        }

        private void Export_Click(object sender, EventArgs e)
        {
            if(CurrentRegionID == UUID.Zero)
            {
                MessageBox.Show("Select a region before attempting to export.");
                return;
            }
            RegionInfo region = _regionManager.GetRegionInfo(CurrentRegionID);
            if (region != null) //It never should be, but who knows
            {
                //Make sure the directory exists
                if (!Directory.Exists("Regions"))
                    Directory.CreateDirectory("Regions");
                if (!File.Exists ("Regions\\" + ExportFileName.Text))
                     File.Create ("Regions\\" + ExportFileName.Text).Close();
                IniConfigSource source = new IniConfigSource("Regions\\" + ExportFileName.Text, IniFileType.AuroraStyle);
                if (source.Configs[region.RegionName] != null)
                {
                    source.Configs.Remove(region.RegionName);
                }
                //Add the config to the given source
                region.CreateIConfig(source);
                source.Save();
            }
        }

        private void startupType_TextChanged (object sender, EventArgs e)
        {
            if (!_changingRegion)
                _textHasChanged = true; //When the user finishes and deselects the box, it will save
        }

        private void startupType_Leave (object sender, EventArgs e)
        {
            if (_textHasChanged)
            {
                Update ();
                _textHasChanged = false;
            }
        }

        private void RegionManager_FormClosing (object sender, FormClosingEventArgs e)
        {
            //Save at the end if it hasn't yet
            if (_textHasChanged)
            {
                Update ();
                _textHasChanged = false;
            }
        }

        private void putOnline_Click (object sender, EventArgs e)
        {
            SetStartingStatus ();
            RegionInfo region = _regionManager.GetRegionInfo(CurrentRegionID);
            Util.FireAndForget (delegate
            {
                _regionManager.StartRegion(region);
                if (CurrentRegionID == region.RegionID)
                {
                    SetOnlineStatus();
                    RefreshCurrentRegionsThreaded();
                }
            });
        }

        private void takeOffline_Click (object sender, EventArgs e)
        {
            SetStoppingStatus();
            Util.FireAndForget (delegate
            {
                if (_regionManager.StopRegion(CurrentRegionID))
                {
                    SetOfflineStatus();
                    RefreshCurrentRegionsThreaded();
                }
            });
        }

        private void resetRegion_Click (object sender, EventArgs e)
        {
            if (_regionManager.GetWhetherRegionIsOnline(CurrentRegionID))
            {
                DialogResult r = Utilities.InputBox("Are you sure?", "Are you sure you want to reset this region (deletes all prims and reverts terrain)?");
                if (r == DialogResult.OK)
                    _regionManager.ResetRegion(CurrentRegionID);
            }
            else
                MessageBox.Show("The region is not online, please turn it online before doing this command.");
        }

        private void deleteregion_Click (object sender, EventArgs e)
        {
            RegionInfo region = _regionManager.GetRegionInfo(CurrentRegionID);
            if (region != null) //It never should be, but who knows
            {
                DialogResult r = Utilities.InputBox ("Are you sure?", "Are you sure you want to delete this region?");
                if (r == DialogResult.OK)
                {
                    SetStoppingStatus();
                    _regionManager.DeleteRegion(region.RegionID);
                    SetOfflineStatus();
                    //Remove everything from the GUI
                    ChangeRegionInfo(null);
                    //Update the regions in the list box as well
                    RefreshCurrentRegions ();
                }
            }
        }

        #region Default Region pieces

        private void GetDefaultRegions ()
        {
            RegionSelections.Items.Add("None");//Add one for the default default
            List<string> files = _regionManager.GetDefaultRegionNames();

            foreach (string file in files)
                RegionSelections.Items.Add (Path.GetFileNameWithoutExtension (file));//Remove the extension

            if (RegionSelections.Items.Count > 1)//Select the first one by default so that its pretty for the user
                RegionSelections.SelectedIndex = 1;
            else
                RegionSelections.SelectedIndex = 0;
        }

        private void CopyOverDefaultRegion (string regionName)
        {
            string fileName = RegionSelections.Items[RegionSelections.SelectedIndex].ToString();
            if (!_regionManager.MoveDefaultRegion(regionName, fileName, false))
            {
                DialogResult s = Utilities.InputBox("Delete file?", "The file " + regionName + ".abackup already exists, delete?");
                if (s == DialogResult.OK)
                    _regionManager.MoveDefaultRegion(regionName, fileName, true);
                else
                    return;//Don't copy the region then
            }
        }

        private void RegionSelections_SelectedIndexChanged (object sender, EventArgs e)
        {
            string name = RegionSelections.Items[RegionSelections.SelectedIndex].ToString ();
            Image b = _regionManager.GetDefaultRegionImage(name);
            if (b == null)
            {
                RegionSelectionsPicture.Image = null;
                return;
            }
            Bitmap result = new Bitmap(RegionSelectionsPicture.Width, RegionSelectionsPicture.Height);
            using (Graphics g = Graphics.FromImage (result))
                g.DrawImage(b, 0, 0, RegionSelectionsPicture.Width, RegionSelectionsPicture.Height);
            RegionSelectionsPicture.Image = result;
        }

        #endregion

        private void RegionManager_Load(object sender, EventArgs e)
        {
            string url;
            if((url = _regionManager.GetOpenRegionSettingsHTMLPage(CurrentRegionID)) != "")
                webBrowser1.Navigate(url);
        }

        private string BuildRegionManagerHTTPPage(UUID currentRegionID)
        {
            RegionInfo region = _regionManager.GetRegionInfo(currentRegionID);
            string html;
            string path = Util.BasePathCombine(System.IO.Path.Combine("data", "RegionManager.html"));
            if (System.IO.File.Exists(path) && region != null)
            {
                html = System.IO.File.ReadAllText(path);
                Dictionary<string, string> vars = new Dictionary<string, string>();
                vars.Add("Region Name", region.RegionName);
                vars.Add("Region Location X", (region.RegionLocX / Constants.RegionSize).ToString());
                vars.Add("Region Location Y", (region.RegionLocY / Constants.RegionSize).ToString());
                vars.Add("Region Ports", string.Join(", ", region.UDPPorts.ConvertAll<string>(delegate(int i) { return i.ToString(); }).ToArray()));
                vars.Add("Region Type", region.RegionType);
                vars.Add("Region Size X", region.RegionSizeX.ToString());
                vars.Add("Region Size Y", region.RegionSizeY.ToString());
                vars.Add("Maturity", region.AccessLevel.ToString());
                vars.Add("Disabled", region.Disabled ? "checked" : "");
                vars.Add("Startup Type", "");//Placeholder for the list later
                vars.Add("Normal", region.Startup == StartupType.Normal ? "selected" : "");
                vars.Add("Medium", region.Startup == StartupType.Medium ? "selected" : "");
                vars.Add("Infinite Region", region.InfiniteRegion ? "checked" : "");
                return CSHTMLCreator.AddHTMLPage(html, "", "RegionManager", vars, RegionMangerHTMLChanged);
            }
            return "";
        }

        private string RegionMangerHTMLChanged(Dictionary<string, string> vars)
        {
            RegionInfo region = _regionManager.GetRegionInfo(CurrentRegionID);
            region.RegionName = vars["Region Name"];
            region.RegionLocX = int.Parse(vars["Region Location X"]) * Constants.RegionSize;
            region.RegionLocY = int.Parse(vars["Region Location Y"]) * Constants.RegionSize;
            string[] ports = vars["Region Ports"].Split(',');

            region.UDPPorts.Clear();
            foreach (string port in ports)
            {
                string tPort = port.Trim();
                int iPort = 0;
                if (int.TryParse(tPort, out iPort))
                    region.UDPPorts.Add(iPort);
            }
            region.RegionSizeX = int.Parse(vars["Region Size X"]);
            region.RegionSizeY = int.Parse(vars["Region Size Y"]);
            region.AccessLevel = byte.Parse(vars["Maturity"]);
            region.Disabled = vars["Disabled"] != null;
            region.Startup = vars["Startup Type"] == "Normal" ? StartupType.Normal : StartupType.Medium;
            region.InfiniteRegion = vars["Infinite Region"] != null;
            return BuildRegionManagerHTTPPage(CurrentRegionID);
        }

        private void button20_Click(object sender, EventArgs e)
        {
            groupBox3.Visible = !groupBox3.Visible;
            webBrowser1.Visible = !webBrowser1.Visible;
        }

        private void einfiniteRegion_CheckedChanged(object sender, EventArgs e)
        {
            Update();
        }

        private void estateOwnerLookupSearch_Click(object sender, EventArgs e)
        {
            RegionInfo region = _regionManager.GetRegionInfo(estateRegionSelection.SelectedItem.ToString());
            if (region == null)
            {
                MessageBox.Show("Region was not found!");
                return;
            }
            _CurrentEstateRegionSelectedID = region.RegionID;
            estateSelection.Items.Clear();
            List<string> estateItems = _regionManager.GetEstatesForUser(estateOwnerName.Text);
            estateSelection.Items.AddRange(estateItems.ToArray());
            UpdateCurrentEstateText(null);
            createNewEstate.Enabled = true;
            changeRegionEstateButton.Enabled = true;
        }

        private void UpdateCurrentEstateText(string p)
        {
            if (p != null)
                currentEstateName.Text = p;
            else
            {
                string estateName = _regionManager.GetCurrentEstate(_CurrentEstateRegionSelectedID);
                currentEstateName.Text = estateName == "" ? "No estates exist" : estateName;
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            string ownerName = estateOwnerName.Text;
            string estateToJoin = estateSelection.SelectedItem.ToString();

            _regionManager.ChangeEstate(ownerName, estateToJoin, _CurrentEstateRegionSelectedID);
            UpdateCurrentEstateText(estateToJoin);
        }

        private void createNewEstate_Click(object sender, EventArgs e)
        {
            UUID regionID = _CurrentEstateRegionSelectedID;
            string estateName = this.estateName.Text;
            string ownerName = estateOwnerName.Text;

            if (!_regionManager.CreateNewEstate(regionID, estateName, ownerName))
                MessageBox.Show("Failed to create the estate, possibly duplicate estate name?");
            else
                UpdateCurrentEstateText(estateName);
        }

        private void estateRegionSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            RegionInfo region = _regionManager.GetRegionInfo(estateRegionSelection.SelectedItem.ToString());
            if (region == null)
                return;

            estateOwnerName.Text = _regionManager.GetEstateOwnerName(region.RegionID);
        }
    }
}
