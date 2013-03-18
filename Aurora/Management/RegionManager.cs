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
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace Aurora.Management
{
    public partial class RegionManager : Form
    {
        public delegate void NewRegion(RegionInfo info);
        public delegate void NoOp();
        public event NewRegion OnNewRegion;
        private readonly bool KillAfterRegionCreation = false;
        private RegionManagerPage _pageToStart = RegionManagerPage.CreateRegion;
        private IConfigSource _config;
        private bool _changingRegion = false;
        private bool _textHasChanged = false;
        private readonly IRegionManagement _regionManager;
        private RegionInfo _startingRegionInfo;

        private readonly System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer();
        private readonly List<NoOp> _timerEvents = new List<NoOp>();

        public RegionInfo RegionInfo
        {
            get;
            set;
        }

        public RegionManager(bool killWindowOnRegionCreation, RegionManagerPage page, IConfigSource config, IRegionManagement regionManagement, RegionInfo startingRegionInfo)
        {
            _regionManager = regionManagement;
            _config = config;
            _startingRegionInfo = startingRegionInfo;
            RegionInfo = _startingRegionInfo;
            KillAfterRegionCreation = killWindowOnRegionCreation;
            _pageToStart = page;
            InitializeComponent();
            ChangeRegionInfo(_startingRegionInfo);
            if (_startingRegionInfo == null)
                groupBox5.Visible = false;
            tabControl1.SelectedIndex = (int)_pageToStart;
            changeEstateBox.Visible = false;
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

        private void ChangeRegionInfo (RegionInfo region)
        {
            _changingRegion = true;
            if (region == null)
            {
                button20.Enabled = false;
                _changingRegion = false;
                textBox11.Text = "";
                textBox6.Text = "50000"; 
                textBox4.Text = "Adult";
                textBox7.Text = "9000";
                textBox3.Text = "1000";
                textBox5.Text = "1000";
                textBox1.Text = "My Sim";
                RegionSizeX.Text = "256";
                RegionSizeY.Text = "256";
                startupType.SelectedIndex = 0;
                einfiniteRegion.Checked = false;
                return;
            }
            button20.Enabled = true;
            textBox11.Text = region.RegionType;
            textBox6.Text = region.ObjectCapacity.ToString ();
            uint maturityLevel = Util.ConvertAccessLevelToMaturity (region.AccessLevel);
            switch (maturityLevel)
            {
                case 0:
                    textBox4.Text = "PG";
                    break;
                case 1:
                    textBox4.Text = "Mature";
                    break;
                default:
                    textBox4.Text = "Adult";
                    break;
            }
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
            RegionInfo region = _startingRegionInfo;
            if (region == null)
            {
                region = new RegionInfo();
                region.RegionID = UUID.Random();
            }
            region.RegionName = textBox1.Text;
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
            int capacity;
            if (int.TryParse(textBox6.Text, out capacity))
                region.ObjectCapacity = capacity;
            int maturityLevel = 0;
            if (!int.TryParse(textBox4.Text, out maturityLevel))
            {
                switch (textBox4.Text)
                {
                    case "Adult":
                        maturityLevel = 2;
                        break;
                    case "Mature":
                        maturityLevel = 1;
                        break;
                    default:
                        maturityLevel = 0;
                        break;
                }
            }
            region.RegionSettings.Maturity = maturityLevel;
            int.TryParse(RegionSizeX.Text, out region.RegionSizeX);
            int.TryParse(RegionSizeY.Text, out region.RegionSizeY);
            region.Startup = ConvertIntToStartupType(startupType.SelectedIndex);
            region.InfiniteRegion = einfiniteRegion.Checked;

            if ((region.RegionSizeX % Constants.MinRegionSize) != 0 || 
                (region.RegionSizeY % Constants.MinRegionSize) != 0)
            {
                MessageBox.Show("You must enter a valid region size (multiple of " + Constants.MinRegionSize + "!");
                return;
            }

            RegionInfo = region;

            if (OnNewRegion != null)
                OnNewRegion(region);

            if (KillAfterRegionCreation)
                Close();
        }

        private void updateRegion_click(object sender, EventArgs e)
        {
            Update();
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

        private void startupType_TextChanged (object sender, EventArgs e)
        {
            if (!_changingRegion)
                _textHasChanged = true; //When the user finishes and deselects the box, it will save
        }

        private void startupType_Leave (object sender, EventArgs e)
        {
            if (_textHasChanged)
                _textHasChanged = false;
        }

        private void RegionManager_FormClosing (object sender, FormClosingEventArgs e)
        {
            //Save at the end if it hasn't yet
            if (_textHasChanged)
                _textHasChanged = false;
        }

        private void putOnline_Click (object sender, EventArgs e)
        {
            SetStartingStatus ();
            Util.FireAndForget (delegate
            {
                _regionManager.StartRegion();
                SetOnlineStatus();
            });
        }

        private void takeOffline_Click (object sender, EventArgs e)
        {
            SetStoppingStatus();
            Util.FireAndForget (delegate
            {
                if (_regionManager.StopRegion(_startingRegionInfo.RegionID, 0))
                    SetOfflineStatus();
            });
        }

        private void resetRegion_Click (object sender, EventArgs e)
        {
            if (_regionManager.GetWhetherRegionIsOnline(_startingRegionInfo.RegionID))
            {
                DialogResult r = Utilities.InputBox("Are you sure?", "Are you sure you want to reset this region (deletes all prims and reverts terrain)?");
                if (r == DialogResult.OK)
                    _regionManager.ResetRegion(_startingRegionInfo.RegionID);
            }
            else
                MessageBox.Show("The region is not online, please turn it online before doing this command.");
        }

        private void deleteregion_Click (object sender, EventArgs e)
        {
            DialogResult r = Utilities.InputBox ("Are you sure?", "Are you sure you want to delete this region?");
            if (r == DialogResult.OK)
            {
                SetStoppingStatus();
                _regionManager.DeleteRegion(_startingRegionInfo.RegionID);
                SetOfflineStatus();
                //Remove everything from the GUI
                ChangeRegionInfo(null);
            }
        }

        private void RegionManager_Load(object sender, EventArgs e)
        {
            string url;
            if (_startingRegionInfo == null)
                return;
            if ((url = _regionManager.GetOpenRegionSettingsHTMLPage(_startingRegionInfo.RegionID)) != "")
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
                Dictionary<string, object> vars = new Dictionary<string, object>();
                vars.Add("Region Name", region.RegionName);
                vars.Add("Region Location X", (region.RegionLocX / Constants.RegionSize).ToString());
                vars.Add("Region Location Y", (region.RegionLocY / Constants.RegionSize).ToString());
                vars.Add("Region Ports", string.Join(", ", region.UDPPorts.ConvertAll(delegate(int i) { return i.ToString(); }).ToArray()));
                vars.Add("Region Type", region.RegionType);
                vars.Add("Region Size X", region.RegionSizeX.ToString());
                vars.Add("Region Size Y", region.RegionSizeY.ToString());
                vars.Add("Maturity", region.AccessLevel.ToString());
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
            _startingRegionInfo.RegionName = vars["Region Name"];
            _startingRegionInfo.RegionLocX = int.Parse(vars["Region Location X"]) * Constants.RegionSize;
            _startingRegionInfo.RegionLocY = int.Parse(vars["Region Location Y"]) * Constants.RegionSize;
            string[] ports = vars["Region Ports"].Split(',');

            _startingRegionInfo.UDPPorts.Clear();
            foreach (string port in ports)
            {
                string tPort = port.Trim();
                int iPort = 0;
                if (int.TryParse(tPort, out iPort))
                    _startingRegionInfo.UDPPorts.Add(iPort);
            }
            _startingRegionInfo.RegionSizeX = int.Parse(vars["Region Size X"]);
            _startingRegionInfo.RegionSizeY = int.Parse(vars["Region Size Y"]);
            _startingRegionInfo.AccessLevel = byte.Parse(vars["Maturity"]);
            _startingRegionInfo.Startup = vars["Startup Type"] == "Normal" ? StartupType.Normal : StartupType.Medium;
            _startingRegionInfo.InfiniteRegion = vars["Infinite Region"] != null;
            return BuildRegionManagerHTTPPage(_startingRegionInfo.RegionID);
        }

        private void button20_Click(object sender, EventArgs e)
        {
            groupBox3.Visible = !groupBox3.Visible;
            webBrowser1.Visible = !webBrowser1.Visible;
        }

        private void estateOwnerLookupSearch_Click(object sender, EventArgs e)
        {
            estateSelection.Items.Clear();
            List<string> estateItems = _regionManager.GetEstatesForUser(estateOwnerName.Text);
            estateSelection.Items.AddRange(estateItems.ToArray());
            UpdateCurrentEstateText(null);
            createNewEstate.Enabled = true;
            changeRegionEstateButton.Enabled = true;
            changeEstateBox.Visible = true;
        }

        private void UpdateCurrentEstateText(string p)
        {
            if (p != null)
                currentEstateName.Text = p;
            else
            {
                string estateName = _regionManager.GetCurrentEstate(_startingRegionInfo.RegionID);
                currentEstateName.Text = estateName == "" ? "No estates exist" : estateName;
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            string ownerName = estateOwnerName.Text;
            string estateToJoin = estateSelection.SelectedItem.ToString();

            _regionManager.ChangeEstate(ownerName, estateToJoin, _startingRegionInfo.RegionID);
            UpdateCurrentEstateText(estateToJoin);
            if (KillAfterRegionCreation)
                Close();
        }

        private void createNewEstate_Click(object sender, EventArgs e)
        {
            string estateName = this.estateName.Text;
            string ownerName = estateOwnerName.Text;

            if (!_regionManager.CreateNewEstate(_startingRegionInfo.RegionID, estateName, ownerName))
                MessageBox.Show("Failed to create the estate, possibly duplicate estate name?");
            else
            {
                UpdateCurrentEstateText(estateName);
                if (KillAfterRegionCreation)
                    Close();
            }
        }

        private void find_user_Click(object sender, EventArgs e)
        {
            UserChooser chooser = new UserChooser(estateOwnerName.Text, _regionManager);
            chooser.ShowDialog();
            estateOwnerName.Text = chooser.UserName;
        }
    }

    public enum RegionManagerPage
    {
        CreateRegion,
        EstateSetup
    }

    public class RegionManagerHelper
    {
        public static void StartAsynchronously(bool killWindowOnRegionCreation, RegionManagerPage page, IConfigSource config, IRegionManagement regionManagement, RegionInfo startingRegionInfo)
        {
            Thread t = new Thread(delegate()
            {
                try
                {
                    RegionManager manager = new RegionManager(killWindowOnRegionCreation, page, config, regionManagement, startingRegionInfo);
                    Application.Run(manager);
                }
                catch { }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }

        public static RegionInfo StartSynchronously(bool killWindowOnRegionCreation, RegionManagerPage page, IConfigSource config, IRegionManagement regionManagement, RegionInfo startingRegionInfo)
        {
            RegionManager manager = null;
            bool done = false;
            Thread t = new Thread(delegate()
            {
                try
                {
                    manager = new RegionManager(killWindowOnRegionCreation, page, config, regionManagement, startingRegionInfo);
                    Application.Run(manager);
                    done = true;
                }
                catch { done = true; }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            while (!done)
                Thread.Sleep(100);
            if (manager.RegionInfo == null)
            {
                MessageBox.Show("You did not create a region, try again (if you did, make sure you presed the Update button!)");
                return StartSynchronously(killWindowOnRegionCreation, page, config, regionManagement, startingRegionInfo);
            }
            return manager.RegionInfo;
        }
    }
}
