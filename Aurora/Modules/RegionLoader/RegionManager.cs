using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Aurora.Modules.RegionLoader
{
    public partial class RegionManager : Form
    {
        bool CreateNewRegion = false;
        public RegionManager(bool create)
        {
            CreateNewRegion = create;
            InitializeComponent();
        }

        private void Next_Click(object sender, EventArgs e)
        {

        }

        private void RegionManager_Load(object sender, EventArgs e)
        {
            if (CreateNewRegion)
            {
                CreateNewRegionEntry();
            }
        }

        private void CreateNewRegionEntry()
        {
        }
    }
}
