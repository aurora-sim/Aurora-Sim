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
        bool ShouldCreateNewRegion = false;
        public RegionManager(bool create)
        {
            InitializeComponent();
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
            Application.Exit();
        }

        private void RegionManager_Load(object sender, EventArgs e)
        {
        }
    }
}
