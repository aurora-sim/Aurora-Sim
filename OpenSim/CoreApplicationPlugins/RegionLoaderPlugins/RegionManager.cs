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
        private bool OpenedForCreateRegion = false;
        private UUID CurrentRegionID = UUID.Zero;
        private ISimulationBase m_OpenSimBase;
        private IRegionInfoConnector m_connector = null;

        public RegionManager(bool create, ISimulationBase baseOpenSim)
        {
            m_OpenSimBase = baseOpenSim;
            m_connector = Aurora.DataManager.DataManager.RequestPlugin<IRegionInfoConnector>();
            OpenedForCreateRegion = create;
            InitializeComponent();
            if (create)
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
            region.RegionLocX = uint.Parse(LocX.Text);
            region.RegionLocY = uint.Parse(LocY.Text);
            
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

            m_connector.UpdateRegionInfo(region);
            IScene scene;
            m_log.Info("[LOADREGIONS]: Creating Region: " + region.RegionName + ")");
            SceneManager manager = m_OpenSimBase.ApplicationRegistry.RequestModuleInterface<SceneManager>();
            manager.CreateRegion(region, true, out scene);

            if (OpenedForCreateRegion)
            {
                System.Windows.Forms.Application.Exit();
                return;
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
            textBox9.Text = region.ExternalHostName;
            textBox7.Text = region.HttpPort.ToString();
            textBox3.Text = region.RegionLocX.ToString();
            textBox5.Text = region.RegionLocY.ToString();
            textBox1.Text = region.RegionName;
        }

        private void Update_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "")
            {
                MessageBox.Show("You must enter a region name!");
                return;
            }
            RegionInfo region = new RegionInfo();
            region.RegionName = textBox1.Text;
            region.RegionID = CurrentRegionID;
            region.RegionLocX = uint.Parse(textBox3.Text);
            region.RegionLocY = uint.Parse(textBox5.Text);

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
            textBox9.Text = region.ExternalHostName;
            textBox7.Text = region.InternalEndPoint.Port.ToString();
            textBox3.Text = region.RegionLocX.ToString();
            textBox5.Text = region.RegionLocY.ToString();
            textBox1.Text = region.RegionName;
            StartupNumberBox.Text = region.NumberStartup.ToString();
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
    }
}
