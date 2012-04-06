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
using System.Windows.Forms;
using Aurora.Framework;
using Nini.Config;
using Nini.Ini;
using OpenMetaverse;
using OpenSim.Region.Framework.Scenes;

namespace Aurora.Modules.RegionLoader
{
    public partial class RegionManager : Form
    {
        public delegate void NewRegion(RegionInfo info);
        public delegate void NoOp();
        public event NewRegion OnNewRegion;
        private readonly bool KillAfterRegionCreation = false;
        private UUID CurrentRegionID = UUID.Zero;
        private readonly ISimulationBase m_OpenSimBase;
        private readonly IRegionInfoConnector m_connector = null;
        private bool m_changingRegion = false;
        private bool m_textHasChanged = false;
        private readonly SceneManager m_sceneManager;
        private string m_defaultRegionsLocation = "DefaultRegions";

        private readonly Timer m_timer = new Timer ();
        private readonly List<NoOp> m_timerEvents = new List<NoOp> ();

        public RegionManager(bool killOnCreate, bool openCreatePageFirst, ISimulationBase baseOpenSim)
        {
            m_OpenSimBase = baseOpenSim;
            m_sceneManager = m_OpenSimBase.ApplicationRegistry.RequestModuleInterface<SceneManager> ();
            m_connector = DataManager.DataManager.RequestPlugin<IRegionInfoConnector>();
            KillAfterRegionCreation = killOnCreate;
            InitializeComponent();
            if (openCreatePageFirst)
                tabControl1.SelectedTab = tabPage2;
            CStartupType.SelectedIndex = 1;
            RefreshCurrentRegions();
            GetDefaultRegions ();
            m_timer.Interval = 100;
            m_timer.Tick += m_timer_Tick;
            m_timer.Start ();
        }

        void m_timer_Tick (object sender, EventArgs e)
        {
            lock (m_timerEvents)
            {
                foreach (NoOp o in m_timerEvents)
                    o ();
                m_timerEvents.Clear ();
            }
        }

        private void RefreshCurrentRegions()
        {
            RegionInfo[] regionInfos = m_connector.GetRegionInfos (false);
            List < RegionInfo >  infos = new List<RegionInfo> (regionInfos);
            infos.Sort (delegate (RegionInfo a, RegionInfo b)
            {
                if (!a.Disabled || !b.Disabled)
                    return a.Disabled.CompareTo(b.Disabled);//At the top
                return a.RegionName.CompareTo (b.RegionName);
            });
            RegionListBox.Items.Clear ();
            foreach(RegionInfo r in infos)
            {
                RegionListBox.Items.Add(!r.Disabled ? "Online - " + r.RegionName : r.RegionName);
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

            m_connector.UpdateRegionInfo(region);
            CopyOverDefaultRegion (region.RegionName);
            if (KillAfterRegionCreation)
            {
                Application.Exit();
                return;
            }
            MainConsole.Instance.Info("[LOADREGIONS]: Creating Region: " + region.RegionName + ")");
            SceneManager manager = m_OpenSimBase.ApplicationRegistry.RequestModuleInterface<SceneManager>();
            manager.AllRegions++;
            region.NewRegion = true;
            Util.FireAndForget(delegate(object o)
            {
                manager.StartNewRegion(region);
            });
            region.NewRegion = false;
            RefreshCurrentRegions();
        }

        private void SearchForRegionByName_Click(object sender, EventArgs e)
        {
            RegionInfo region = m_connector.GetRegionInfo(RegionToFind.Text);
            if (region == null)
            {
                MessageBox.Show("Region was not found!");
                return;
            }
            ChangeRegionInfo (region);
        }

        private void ChangeRegionInfo (RegionInfo region)
        {
            m_changingRegion = true;
            if (region == null)
            {
                button20.Enabled = false;
                CurrentRegionID = UUID.Zero;
                m_changingRegion = false;
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
            IScene scene;
            if (m_sceneManager.TryGetScene (region.RegionID, out scene))
                SetOnlineStatus ();
            else
                SetOfflineStatus ();

            m_changingRegion = false;
        }

        private void SetOfflineStatus ()
        {
            m_timerEvents.Add (delegate
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
            m_timerEvents.Add (delegate
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
            m_timerEvents.Add (delegate
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
            m_timerEvents.Add (delegate
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
            RegionInfo region = m_connector.GetRegionInfo(item.ToString());
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

            m_connector.UpdateRegionInfo(region);
            if (OnNewRegion != null)
                OnNewRegion(region);
            if(listNeedsUpdated)
                RefreshCurrentRegions();
            RegionListBox.SelectedItem = region.RegionName;

            IOpenRegionSettingsConnector orsc = Aurora.DataManager.DataManager.RequestPlugin<IOpenRegionSettingsConnector>();
            if (orsc != null)
            {
                OpenRegionSettings ors = orsc.GetSettings(region.RegionID);
                ors.MaximumPhysPrimScale = float.Parse(eMaxPhysPrim.Text);
                ors.MaximumPrimScale = float.Parse(eMaxPrimSize.Text);
                orsc.SetSettings(region.RegionID, ors);
            }
        }

        private void RegionListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(RegionListBox.SelectedIndex < 0)
                return;
            string item = RegionListBox.Items[RegionListBox.SelectedIndex].ToString();
            if(item.StartsWith("Online - "))
                item = item.Remove(0, 9);
            RegionInfo region = m_connector.GetRegionInfo(item);
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

        private void MaxNonPhysPrimHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This is the maxiumum size that you can make non physical prims.");
        }

        private void MaximumPhysPrimHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This is the maxiumum size that you can make physical prims.");
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
            RegionInfo region = m_connector.GetRegionInfo(CurrentRegionID);
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
            if (!m_changingRegion)
                m_textHasChanged = true; //When the user finishes and deselects the box, it will save
        }

        private void startupType_Leave (object sender, EventArgs e)
        {
            if (m_textHasChanged)
            {
                Update ();
                m_textHasChanged = false;
            }
        }

        private void RegionManager_FormClosing (object sender, FormClosingEventArgs e)
        {
            //Save at the end if it hasn't yet
            if (m_textHasChanged)
            {
                Update ();
                m_textHasChanged = false;
            }
        }

        private void putOnline_Click (object sender, EventArgs e)
        {
            SetStartingStatus ();
            RegionInfo region = m_connector.GetRegionInfo (CurrentRegionID);
            Util.FireAndForget (delegate
                                    {
                m_sceneManager.AllRegions++;
                m_sceneManager.StartNewRegion (region);
                if (CurrentRegionID == region.RegionID)
                    SetOnlineStatus ();
            });
        }

        private void takeOffline_Click (object sender, EventArgs e)
        {
            IScene scene;
            SetStoppingStatus();
            Util.FireAndForget (delegate
                                    {
                m_sceneManager.AllRegions--;
                m_sceneManager.TryGetScene (CurrentRegionID, out scene);
                if (scene != null)
                {
                    m_sceneManager.CloseRegion(scene, ShutdownType.Immediate, 0);
                }
                if (scene == null || CurrentRegionID == scene.RegionInfo.RegionID || CurrentRegionID ==  UUID.Zero)
                    SetOfflineStatus();
            });
        }

        private void resetRegion_Click (object sender, EventArgs e)
        {
            IScene scene;
            m_sceneManager.TryGetScene (CurrentRegionID, out scene);
            if (scene != null)
            {
                DialogResult r = Utilities.InputBox("Are you sure?", "Are you sure you want to reset this region (deletes all prims and reverts terrain)?");
                if (r == DialogResult.OK)
                    m_sceneManager.ResetRegion(scene);
            }
            else
                MessageBox.Show("The region is not online, please turn it online before doing this command.");
        }

        private void deleteregion_Click (object sender, EventArgs e)
        {
            RegionInfo region = m_connector.GetRegionInfo (CurrentRegionID);
            if (region != null) //It never should be, but who knows
            {
                DialogResult r = Utilities.InputBox ("Are you sure?", "Are you sure you want to delete this region?");
                if (r == DialogResult.OK)
                {
                    takeOffline_Click(sender, e);
                    m_connector.Delete (region);
                    SceneManager sm = m_OpenSimBase.ApplicationRegistry.RequestModuleInterface<SceneManager>();
                    if (sm != null)
                        sm.DeleteRegion(region);
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
            IConfig config = m_OpenSimBase.ConfigSource.Configs["RegionManager"];
            if (config != null)
                m_defaultRegionsLocation = config.GetString ("DefaultRegionsLocation", m_defaultRegionsLocation);

            RegionSelections.Items.Add ("None");//Add one for the default default

            if (!Directory.Exists (m_defaultRegionsLocation))
            {
                RegionSelections.SelectedIndex = 0;
                return;
            }

            string[] files = Directory.GetFiles (m_defaultRegionsLocation, "*.abackup");
            foreach (string file in files)
            {
                RegionSelections.Items.Add (Path.GetFileNameWithoutExtension (file));//Remove the extension
            }
            RegionSelections.SelectedIndex = 1;//Select the first one by default so that its pretty for the user
        }

        private void CopyOverDefaultRegion (string regionName)
        {
            string name = RegionSelections.Items[RegionSelections.SelectedIndex].ToString ();
            name = Path.Combine (m_defaultRegionsLocation, name + ".backup");//Full name
            if (!File.Exists (name))
                return;//None selected

            string loadAppenedFileName = "";
            string newFilePath = "";
            IConfig simData = m_OpenSimBase.ConfigSource.Configs["FileBasedSimulationData"];
            if (simData != null)
            {
                loadAppenedFileName = simData.GetString ("ApendedLoadFileName", loadAppenedFileName);
                newFilePath = simData.GetString ("LoadBackupDirectory", newFilePath);
            }
            string newFileName = Path.Combine (newFilePath, name + loadAppenedFileName + ".abackup");
            if (!File.Exists (name))
                return; //None selected
            if (File.Exists (newFileName))
            {
                DialogResult s = Utilities.InputBox ("Delete file?", "The file " + name + " already exists, delete?");
                if (s == DialogResult.OK)
                    File.Delete (name);
                else
                    return;//None selected
            }
            File.Copy (name, newFileName);
        }

        private void RegionSelections_SelectedIndexChanged (object sender, EventArgs e)
        {
            string name = RegionSelections.Items[RegionSelections.SelectedIndex].ToString ();
            Image b = null;
            if (File.Exists (Path.Combine (m_defaultRegionsLocation, name + ".png")))
                b = Image.FromFile (Path.Combine (m_defaultRegionsLocation, name + ".png"));
            else if (File.Exists (Path.Combine (m_defaultRegionsLocation, name + ".jpg")))
                b = Image.FromFile (Path.Combine (m_defaultRegionsLocation, name + ".jpg"));
            else if (File.Exists (Path.Combine (m_defaultRegionsLocation, name + ".jpeg")))
                b = Image.FromFile (Path.Combine (m_defaultRegionsLocation, name + ".jpeg"));
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
            IOpenRegionSettingsConnector orsc = DataManager.DataManager.RequestPlugin<IOpenRegionSettingsConnector>();
            if (orsc != null)
            {
                string navUrl = orsc.AddOpenRegionSettingsHTMLPage(CurrentRegionID);
                //string navUrl = BuildRegionManagerHTTPPage(CurrentRegionID);
                webBrowser1.Navigate(navUrl);
            }
        }

        private string BuildRegionManagerHTTPPage(UUID currentRegionID)
        {
            RegionInfo region = m_connector.GetRegionInfo(currentRegionID);
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
            RegionInfo region = m_connector.GetRegionInfo(CurrentRegionID);
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
    }
}
