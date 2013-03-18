using Aurora.Framework;
using OpenMetaverse;
using System;
using System.Windows.Forms;

namespace Aurora.Management
{
    public partial class CreateUser : Form
    {
        private IRegionManagement _regionManagement;
        public string UserName { get; private set; }

        public CreateUser(IRegionManagement regionManagement)
        {
            _regionManagement = regionManagement;
            InitializeComponent();
            user_id.Text = UUID.Random().ToString();
            scope_id.Text = UUID.Zero.ToString();
        }

        private void create_Click(object sender, EventArgs e)
        {
            UUID scopeID;
            UUID userID;
            if (user_name.Text == "" ||
                !UUID.TryParse(scope_id.Text, out scopeID) ||
                !UUID.TryParse(user_id.Text, out userID) ||
                password.Text == "")
            {
                MessageBox.Show("Please fill in all the information");
                return;
            }

            UserName = user_name.Text;
            _regionManagement.CreateUser(user_name.Text, password.Text, email.Text, userID, scopeID);
            Close();
        }

        private void password_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
