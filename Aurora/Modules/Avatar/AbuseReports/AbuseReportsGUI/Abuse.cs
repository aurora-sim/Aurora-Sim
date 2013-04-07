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
using System.Drawing;
using System.Windows.Forms;
using Aurora.Framework;
using Aurora.Framework.DatabaseInterfaces;
using Aurora.Framework.Modules;
using Aurora.Framework.Services;
using Aurora.Framework.Utilities;
using OpenMetaverse;

namespace Aurora.Modules.AbuseReportsGUI
{
    public partial class Abuse : Form
    {
        private readonly IAbuseReportsConnector AbuseReportsConnector;
        private readonly string Password;
        private readonly IAssetService m_assetService;
        private readonly IJ2KDecoder m_decoder;
        private AbuseReport CurrentReport;
        private int formNumber = 1;

        public Abuse(IAssetService assetService, IJ2KDecoder j2k)
        {
            InitializeComponent();
            m_decoder = j2k;
            m_assetService = assetService;
            AbuseReportsConnector = Framework.Utilities.DataManager.RequestPlugin<IAbuseReportsConnector>();
            Password = "";
            Utilities.InputBox("Password Input Required", "Password for abuse reports database", ref Password);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SetGUI(AbuseReportsConnector.GetAbuseReport(formNumber, Password));
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
                ObjectPos.Text = AR.ObjectPosition;
                Abusername.Text = AR.AbuserName;
                AbuseLocation.Text = AR.AbuseLocation;
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
                AbuseLocation.Text = "";
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

            byte[] asset = m_assetService.GetData(TextureID.ToString());
            if (asset == null || m_decoder == null)
                return new Bitmap(1, 1);
            Image image = m_decoder.DecodeToImage(asset);
            if (image != null)
                return image;
            else
                return new Bitmap(1, 1);
        }
    }
}