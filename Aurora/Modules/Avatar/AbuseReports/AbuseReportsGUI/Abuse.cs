using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Aurora.DataManager;
using Aurora.Framework;
using OpenMetaverse;
using OpenMetaverse.Imaging;
using OpenSim.Framework;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules.AbuseReportsGUI
{
    public partial class Abuse : Form
    {
        private int formNumber = 1;
        private IAbuseReportsConnector AbuseReportsConnector;
        private string Password;
        private AbuseReport CurrentReport = null;
        private IAssetService m_assetService;

        public Abuse(IAssetService assetService)
        {
            InitializeComponent();
            m_assetService = assetService;
            AbuseReportsConnector = Aurora.DataManager.DataManager.RequestPlugin<IAbuseReportsConnector>();
            Abuse.InputBox("Password Input Required", "Password for abuse reports database", ref Password);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SetGUI(AbuseReportsConnector.GetAbuseReport(formNumber, Password));
        }

        public static DialogResult InputBox(string title, string promptText, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            formNumber -= 1;
            if (formNumber == 0)
                formNumber = 1;

            SetGUI(AbuseReportsConnector.GetAbuseReport(formNumber, Password));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            formNumber += 1;
            SetGUI(AbuseReportsConnector.GetAbuseReport(formNumber, Password));
        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {
            CurrentReport.AssignedTo = AssignedTo.Text;
            AbuseReportsConnector.UpdateAbuseReport(CurrentReport, Password);
        }

        private void textBox8_TextChanged(object sender, EventArgs e)
        {
            bool.TryParse(Active.Text, out CurrentReport.Active);
            AbuseReportsConnector.UpdateAbuseReport(CurrentReport, Password);
        }

        private void textBox10_TextChanged(object sender, EventArgs e)
        {
            bool.TryParse(Checked.Text, out CurrentReport.Checked);
            AbuseReportsConnector.UpdateAbuseReport(CurrentReport, Password);
        }

        private void textBox12_TextChanged(object sender, EventArgs e)
        {
            CurrentReport.Notes = Notes.Text;
            AbuseReportsConnector.UpdateAbuseReport(CurrentReport, Password);
        }

        private void GotoAR_Click(object sender, EventArgs e)
        {
            if (GotoARNumber.Text == "")
                return;
            formNumber = Convert.ToInt32(GotoARNumber.Text);
            if (formNumber <= 0)
                formNumber = 1;
            GotoARNumber.Text = "";
            SetGUI(AbuseReportsConnector.GetAbuseReport(formNumber, Password));
            
        }

        public void SetGUI(AbuseReport AR)
        {
            if (AR != null)
            {
                CurrentReport = AR;
                Category.Text = AR.Category.ToString();
                ReporterName.Text = AR.ReporterName;
                ObjectName.Text = AR.ObjectName;
                ObjectPos.Text = AR.ObjectPosition.ToString();
                Abusername.Text = AR.AbuserName;
                Location.Text = AR.AbuseLocation;
                Summary.Text = AR.AbuseSummary;
                Details.Text = AR.AbuseDetails;
                AssignedTo.Text = AR.AssignedTo;
                Active.Text = AR.Active.ToString();
                Checked.Text = AR.Checked.ToString();
                Notes.Text = AR.Notes;
                CardNumber.Text = formNumber.ToString();
                SnapshotUUID.Image = GetTexture(AR.ScreenshotID);
            }
            else
            {
                Category.Text = "";
                ReporterName.Text = "";
                ObjectName.Text = "";
                ObjectPos.Text = "";
                Abusername.Text = "";
                Location.Text = "";
                Summary.Text = "";
                Details.Text = "";
                AssignedTo.Text = "";
                Active.Text = "";
                Checked.Text = "";
                Notes.Text = "";
                CardNumber.Text = formNumber.ToString();
                SnapshotUUID.Image = GetTexture(UUID.Zero);
            }
        }

        public Image GetTexture(UUID TextureID)
        {
            if (TextureID == UUID.Zero) //Send white instead of null
                TextureID = Util.BLANK_TEXTURE_UUID;

            AssetBase asset = m_assetService.Get(TextureID.ToString());
            ManagedImage managedImage;
            Image image;

            if (asset != null && OpenJPEG.DecodeToImage(asset.Data, out managedImage, out image))
                return image;
            else
                return new Bitmap(1, 1);
        }
    }
}
