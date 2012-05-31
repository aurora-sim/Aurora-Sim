using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Nini.Config;
using Aurora.Framework;

namespace Aurora.Management
{
    public partial class RemoteManagementSetup : Form
    {
        private IConfigSource _config; 
        public RemoteManagementSetup(IConfigSource config)
        {
            _config = config;
            InitializeComponent();
        }

        private void connect_Click(object sender, EventArgs e)
        {
            string IPAddress = _ipaddress.Text.StartsWith("http://") ?
                _ipaddress.Text :
                "http://" + _ipaddress.Text;
            IPAddress += ":" + _port.Text;
            IRegionManagement management = new RegionManagement(IPAddress + "/regionmanagement",
                        _password.Text);
            if (!management.ConnectionIsWorking())
            {
                MessageBox.Show("Failed to connect to remote instance, check the IP and password and try again");
                return;
            }
            RegionManager.StartAsynchronously(false,
                false,
                _config,
                management);
        }

        private void _history_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
