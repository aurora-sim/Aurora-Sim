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
        public event NewRegion OnNewRegion;
        private bool KillAfterRegionCreation = false;
        private UUID CurrentRegionID = UUID.Zero;
        private ISimulationBase m_OpenSimBase;
        private IRegionInfoConnector m_connector = null;
        private bool m_changingRegion = false;
        private bool m_textHasChanged = false;

        public RegionManager(bool killOnCreate, bool openCreatePageFirst, ISimulationBase baseOpenSim)
        {
            m_OpenSimBase = baseOpenSim;
            m_connector = Aurora.DataManager.DataManager.RequestPlugin<IRegionInfoConnector>();
            KillAfterRegionCreation = killOnCreate;
            InitializeComponent();
            if (openCreatePageFirst)
                tabControl1.SelectedTab = tabPage2;
            RefreshCurrentRegions();
        }

        private void RefreshCurrentRegions()
        {
            RegionListBox.Items.Clear();
            RegionInfo[] regionInfos = m_connector.GetRegionInfos(false);
            foreach(RegionInfo r in regionInfos)
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
            int port = port = Convert.ToInt32(Port.Text);
            region.InternalEndPoint = new IPEndPoint(address, port);

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
            if (KillAfterRegionCreation)
            {
                System.Windows.Forms.Application.Exit();
                return;
            }
            else
            {
                IScene scene;
                m_log.Info("[LOADREGIONS]: Creating Region: " + region.RegionName + ")");
                SceneManager manager = m_OpenSimBase.ApplicationRegistry.RequestModuleInterface<SceneManager>();
                manager.CreateRegion(region, out scene);
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
            m_changingRegion = true;
            CurrentRegionID = region.RegionID;
            textBox11.Text = region.RegionType;
            textBox6.Text = region.ObjectCapacity.ToString();
            uint maturityLevel = Util.ConvertAccessLevelToMaturity(region.AccessLevel);
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
            textBox7.Text = region.HttpPort.ToString();
            textBox3.Text = (region.RegionLocX / Constants.RegionSize).ToString();
            textBox5.Text = (region.RegionLocY / Constants.RegionSize).ToString();
            textBox1.Text = region.RegionName;
            RegionSizeX.Text = region.RegionSizeX.ToString ();
            RegionSizeY.Text = region.RegionSizeY.ToString ();
            startupType.SelectedIndex = ConvertStartupType(region.Startup);
            m_changingRegion = false;
        }

        private new void Update()
        {
            RegionInfo region = m_connector.GetRegionInfo(textBox1.Text);
            if (region == null)
            {
                MessageBox.Show("You must enter a valid region name!");
                return;
            }
            region.RegionName = textBox1.Text;
            region.RegionID = CurrentRegionID;
            region.RegionLocX = int.Parse(textBox3.Text) * Constants.RegionSize;
            region.RegionLocY = int.Parse(textBox5.Text) * Constants.RegionSize;

            IPAddress address = IPAddress.Parse("0.0.0.0");
            int port = port = Convert.ToInt32(textBox7.Text);
            region.InternalEndPoint = new IPEndPoint(address, port);
            region.HttpPort = (uint)port;

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
            if (OnNewRegion != null)
                OnNewRegion(region);
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
            m_changingRegion = true;
            CurrentRegionID = region.RegionID;
            textBox11.Text = region.RegionType;
            textBox6.Text = region.ObjectCapacity.ToString();
            int maturityLevel = region.RegionSettings.Maturity;
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
            textBox7.Text = region.InternalEndPoint.Port.ToString();
            textBox3.Text = (region.RegionLocX / Constants.RegionSize).ToString();
            textBox5.Text = (region.RegionLocY / Constants.RegionSize).ToString();
            textBox1.Text = region.RegionName;
            RegionSizeX.Text = region.RegionSizeX.ToString();
            RegionSizeY.Text = region.RegionSizeY.ToString();
            StartupNumberBox.Text = region.NumberStartup.ToString ();
            startupType.SelectedIndex = ConvertStartupType(region.Startup);
            m_changingRegion = false;
        }

        private int ConvertStartupType (StartupType startupType)
        {
            if (startupType == StartupType.Normal)
                return 0;
            else if (startupType == StartupType.Medium)
                return 1;
            else
                return 2;
        }

        private StartupType ConvertIntToStartupType (int i)
        {
            if (i == 2)
                return StartupType.Soft;
            else if (i == 1)
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
            MessageBox.Show("This is the port that your region will run on.");
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

        private void button13_Click(object sender, EventArgs e)
        {
            if(CurrentRegionID == UUID.Zero)
            {
                MessageBox.Show("Select a region before attempting to delete.");
                return;
            }
            RegionInfo region = m_connector.GetRegionInfo(CurrentRegionID);
            if (region != null) //It never should be, but who knows
            {
                DialogResult r = Utilities.InputBox("Are you sure?", "Are you sure you want to delete this region?");
                if (r == DialogResult.OK)
                {
                    m_connector.Delete(region);
                    //Update the regions in the list box as well
                    RefreshCurrentRegions();
                }
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
    }
}
