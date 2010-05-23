using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Net;
using System.Text;
using System.Windows.Forms;
using Aurora.Framework;
using OpenSim.Framework;
using OpenMetaverse;

namespace Aurora.Modules.RegionLoader
{
    public partial class RegionManager : Form
    {
        public delegate void NewRegion(RegionInfo info);
        public event NewRegion OnNewRegion;
        private bool OpenedForCreateRegion = false;
        private UUID CurrentRegionID = UUID.Zero;

        public RegionManager(bool create)
        {
            OpenedForCreateRegion = create;
            InitializeComponent();
            if (create)
                tabControl1.SelectedTab = tabPage2;
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
                externalName = Aurora.Framework.Utils.GetExternalIp();
                region.FindExternalAutomatically = true;
            }
            else
                region.FindExternalAutomatically = false;
            region.ExternalHostName = externalName;

            region.RegionType = Type.Text;
            region.NonphysPrimMax = int.Parse(MaxNonPhys.Text);
            region.PhysPrimMax = int.Parse(MaxPhys.Text);
            region.ClampPrimSize = true;
            region.ObjectCapacity = int.Parse(ObjectCount.Text);
            region.AccessLevel = Util.ConvertMaturityToAccessLevel(uint.Parse(Maturity.Text));

            Aurora.DataManager.DataManager.IRegionInfoConnector.UpdateRegionInfo(region, bool.Parse(Disabled.Text));
            if (OnNewRegion != null)
                OnNewRegion(region);

            if(OpenedForCreateRegion)
                Application.Exit();
        }

        private void SearchForRegionByName_Click(object sender, EventArgs e)
        {
            RegionInfo region = Aurora.DataManager.DataManager.IRegionInfoConnector.GetRegionInfo(RegionToFind.Text);
            if (region == null)
            {
                MessageBox.Show("Region was not found!");
                return;
            }
            CurrentRegionID = region.RegionID;
            textBox11.Text = region.RegionType;
            textBox10.Text = region.NonphysPrimMax.ToString();
            textBox8.Text = region.PhysPrimMax.ToString();
            textBox6.Text = region.ObjectCapacity.ToString();
            textBox4.Text = region.AccessLevel.ToString();
            textBox2.Text = region.Disabled.ToString();
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

            string externalName = textBox9.Text;
            if (externalName == "DEFAULT")
            {
                externalName = Aurora.Framework.Utils.GetExternalIp();
                region.FindExternalAutomatically = true;
            }
            else
                region.FindExternalAutomatically = false;
            region.ExternalHostName = externalName;

            region.RegionType = textBox11.Text;
            region.NonphysPrimMax = int.Parse(textBox10.Text);
            region.PhysPrimMax = int.Parse(textBox8.Text);
            region.ClampPrimSize = true;
            region.ObjectCapacity = int.Parse(textBox6.Text);
            region.AccessLevel = Util.ConvertMaturityToAccessLevel(uint.Parse(textBox4.Text));

            Aurora.DataManager.DataManager.IRegionInfoConnector.UpdateRegionInfo(region, bool.Parse(textBox2.Text));
            if (OnNewRegion != null)
                OnNewRegion(region);
        }
    }
}
