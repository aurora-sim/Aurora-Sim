using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Aurora.DataManager;
using Aurora.Framework;
using Microsoft.VisualBasic;

namespace Aurora.Modules.AbuseReportsGUI
{
    public partial class Abuse : Form
    {
        private int formNumber = 1;
        private IAbuseReportsConnector AbuseReportsConnector;
        private string Password;
        private AbuseReport CurrentReport = null;

        public Abuse()
        {
            InitializeComponent();
            AbuseReportsConnector = Aurora.DataManager.DataManager.RequestPlugin<IAbuseReportsConnector>("IAbuseReportsConnector");
            Password = Microsoft.VisualBasic.Interaction.InputBox("Password for abuse reports database.","Password Input Required","",0,0);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            AbuseReport AR = AbuseReportsConnector.GetAbuseReport(formNumber, Password);
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
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            formNumber -= 1;
            if (formNumber == 0)
            {
                formNumber = 1;
            }

            AbuseReport AR = AbuseReportsConnector.GetAbuseReport(formNumber, Password);
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
            }
            else
            {
                Category.Text = "";
                ReporterName.Text = "";
                ObjectName.Text = "";
                Abusername.Text = "";
                Location.Text = "";
                Details.Text = "";
                Summary.Text = "";
                AssignedTo.Text = "";
                Active.Text = "";
                Checked.Text = "";
                ObjectPos.Text = "";
                Notes.Text = "";
                CardNumber.Text = formNumber.ToString();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            formNumber += 1;
            AbuseReport AR = AbuseReportsConnector.GetAbuseReport(formNumber, Password);
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
            }
            else
            {
                Category.Text = "";
                ReporterName.Text = "";
                ObjectName.Text = "";
                Abusername.Text = "";
                Location.Text = "";
                Details.Text = "";
                Summary.Text = "";
                AssignedTo.Text = "";
                Active.Text = "";
                Checked.Text = "";
                ObjectPos.Text = "";
                Notes.Text = "";
                CardNumber.Text = formNumber.ToString();
            }
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
            AbuseReport AR = AbuseReportsConnector.GetAbuseReport(formNumber, Password);
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
            }
        }
    }
}
