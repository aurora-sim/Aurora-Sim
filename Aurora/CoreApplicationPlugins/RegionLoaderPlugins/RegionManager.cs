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
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Aurora.Framework;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using OpenSim;
using log4net;
using Nini.Config;
using Nini.Ini;

namespace Aurora.Modules.RegionLoader
{
    public partial class RegionManager : Form
    {
        private static readonly ILog m_log
           = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public delegate void NewRegion(RegionInfo info);
        public delegate void NoOp();
        public event NewRegion OnNewRegion;
        private bool KillAfterRegionCreation = false;
        private UUID CurrentRegionID = UUID.Zero;
        private ISimulationBase m_OpenSimBase;
        private IRegionInfoConnector m_connector = null;
        private bool m_changingRegion = false;
        private bool m_textHasChanged = false;
        private SceneManager m_sceneManager;
        private string m_defaultRegionsLocation = "DefaultRegions";

        private System.Windows.Forms.Timer m_timer = new Timer ();
        private List<NoOp> m_timerEvents = new List<NoOp> ();

        public RegionManager(bool killOnCreate, bool openCreatePageFirst, ISimulationBase baseOpenSim)
        {
            m_OpenSimBase = baseOpenSim;
            m_sceneManager = m_OpenSimBase.ApplicationRegistry.RequestModuleInterface<SceneManager> ();
            m_connector = Aurora.DataManager.DataManager.RequestPlugin<IRegionInfoConnector>();
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
                return a.RegionName.CompareTo (b.RegionName);
            });
            RegionListBox.Items.Clear ();
            foreach(RegionInfo r in infos)
            {
                RegionListBox.Items.Add(r.RegionName);
            }
        }

        private void CreateNewRegion(object sender, EventArgs e)
        {
            if (RName.Text == "")
            {
                MessageBox.Show("You must enter a region name!");
                return;
            }
            RegionInfo region = new RegionInfo();
            region.RegionName = RName.Text;
            region.RegionID = UUID.Random();
            region.RegionLocX = int.Parse(LocX.Text) * Constants.RegionSize;
            region.RegionLocY = int.Parse(LocY.Text) * Constants.RegionSize;
            
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
            region.HttpPort = (uint)region.InternalEndPoint.Port;

            string externalName = ExternalIP.Text;
            if (externalName == "DEFAULT")
            {
                externalName = Aurora.Framework.Utilities.GetExternalIp();
                region.FindExternalAutomatically = true;
            }
            else
                region.FindExternalAutomatically = false;
            region.ExternalHostName = externalName;

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

            m_connector.UpdateRegionInfo(region);
            CopyOverDefaultRegion (region.RegionName);
            if (KillAfterRegionCreation)
            {
                System.Windows.Forms.Application.Exit();
                return;
            }
            else
            {
                m_log.Info("[LOADREGIONS]: Creating Region: " + region.RegionName + ")");
                SceneManager manager = m_OpenSimBase.ApplicationRegistry.RequestModuleInterface<SceneManager>();
                manager.StartNewRegion(region);
            }
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
            if (region.FindExternalAutomatically)
                textBox9.Text = "DEFAULT";
            else
                textBox9.Text = region.ExternalHostName;
            textBox7.Text = string.Join (", ", region.UDPPorts.ConvertAll<string> (delegate (int i) { return i.ToString (); }).ToArray ());
            textBox3.Text = (region.RegionLocX / Constants.RegionSize).ToString ();
            textBox5.Text = (region.RegionLocY / Constants.RegionSize).ToString ();
            textBox1.Text = region.RegionName;
            RegionSizeX.Text = region.RegionSizeX.ToString ();
            RegionSizeY.Text = region.RegionSizeY.ToString ();
            startupType.SelectedIndex = ConvertStartupType (region.Startup);
            IScene scene;
            if (m_sceneManager.TryGetScene (region.RegionID, out scene))
                SetOnlineStatus ();
            else
                SetOfflineStatus ();

            m_changingRegion = false;
        }

        private void SetOfflineStatus ()
        {
            m_timerEvents.Add (delegate ()
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
            m_timerEvents.Add (delegate ()
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
            m_timerEvents.Add (delegate ()
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
            m_timerEvents.Add (delegate ()
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
            object item = RegionListBox.Items[RegionListBox.SelectedIndex];
            if(item == null)
            {
                MessageBox.Show("Select a valid region from the list.");
                return;
            }
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
            region.HttpPort = (uint)region.InternalEndPoint.Port;

            string externalName = textBox9.Text;
            if (externalName == "DEFAULT")
            {
                externalName = Aurora.Framework.Utilities.GetExternalIp();
                region.FindExternalAutomatically = true;
            }
            else
                region.FindExternalAutomatically = false;
            region.ExternalHostName = externalName;

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

            if ((region.RegionSizeX % Constants.MinRegionSize) != 0 || 
                (region.RegionSizeY % Constants.MinRegionSize) != 0)
            {
                MessageBox.Show("You must enter a valid region size (multiple of " + Constants.MinRegionSize + "!");
                return;
            }

            m_connector.UpdateRegionInfo(region);
            m_OpenSimBase.ApplicationRegistry.RequestModuleInterface<SceneManager>().UpdateRegionInfo(oldRegionName, region);
            if (OnNewRegion != null)
                OnNewRegion(region);
            if(listNeedsUpdated)
                RefreshCurrentRegions();
            RegionListBox.SelectedItem = region.RegionName;
        }

        private void RegionListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            object item = RegionListBox.Items[RegionListBox.SelectedIndex];
            RegionInfo region = m_connector.GetRegionInfo(item.ToString());
            if (region == null)
            {
                MessageBox.Show("Region was not found!");
                return;
            }
            ChangeRegionInfo (region);
        }

        private int ConvertStartupType (StartupType startupType)
        {
            if (startupType == StartupType.Normal)
                return 0;
            else 
                return 1;
        }

        private StartupType ConvertIntToStartupType (int i)
        {
            if (i == 1)
                return StartupType.Medium;
            else
                return StartupType.Normal;
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
            Util.FireAndForget (delegate (object o)
            {
                m_sceneManager.StartNewRegion (region);
                if (CurrentRegionID == region.RegionID)
                    SetOnlineStatus ();
            });
        }

        private void takeOffline_Click (object sender, EventArgs e)
        {
            IScene scene;
            SetStoppingStatus ();
            Util.FireAndForget (delegate (object o)
            {
                m_sceneManager.TryGetScene (CurrentRegionID, out scene);
                m_sceneManager.CloseRegion (scene, ShutdownType.Immediate, 0);
                if(CurrentRegionID == scene.RegionInfo.RegionID)
                    SetOfflineStatus ();
            });
        }

        private void resetRegion_Click (object sender, EventArgs e)
        {
            IScene scene;
            m_sceneManager.TryGetScene (CurrentRegionID, out scene);
            DialogResult r = Utilities.InputBox ("Are you sure?", "Are you sure you want to reset this region (deletes all prims and reverts terrain)?");
            if (r == DialogResult.OK)
                m_sceneManager.ResetRegion (scene);
        }

        private void deleteregion_Click (object sender, EventArgs e)
        {
            RegionInfo region = m_connector.GetRegionInfo (CurrentRegionID);
            if (region != null) //It never should be, but who knows
            {
                DialogResult r = Utilities.InputBox ("Are you sure?", "Are you sure you want to delete this region?");
                if (r == DialogResult.OK)
                {
                    m_connector.Delete (region);
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
                if (s == System.Windows.Forms.DialogResult.OK)
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
                b = Bitmap.FromFile (Path.Combine (m_defaultRegionsLocation, name + ".png"));
            else if (File.Exists (Path.Combine (m_defaultRegionsLocation, name + ".jpg")))
                b = Bitmap.FromFile (Path.Combine (m_defaultRegionsLocation, name + ".jpg"));
            else if (File.Exists (Path.Combine (m_defaultRegionsLocation, name + ".jpeg")))
                b = Bitmap.FromFile (Path.Combine (m_defaultRegionsLocation, name + ".jpeg"));
            if (b == null)
            {
                RegionSelectionsPicture.Image = b;
                return;
            }
            Bitmap result = new Bitmap(RegionSelectionsPicture.Width, RegionSelectionsPicture.Height);
            using (Graphics g = Graphics.FromImage (result))
                g.DrawImage(b, 0, 0, RegionSelectionsPicture.Width, RegionSelectionsPicture.Height);
            RegionSelectionsPicture.Image = result;
        }

        #endregion
    }
}
