using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Aurora.DataManager;
using Aurora.Framework;

namespace Aurora.Modules.AbuseReportsGUI
{
    public partial class Abuse : Form
    {
        private int formNumber = 1;
        private IGenericData GenericData;
        private IRegionData RegionData;
        public Abuse()
        {
            InitializeComponent();
            GenericData = Aurora.DataManager.DataManager.GetGenericPlugin();
            RegionData = Aurora.DataManager.DataManager.GetRegionPlugin();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            AbuseReport AR = RegionData.GetAbuseReport(formNumber);
            if (AR.ReportNumber != null)
            {
                Category.Text = AR.Category;
                ReporterName.Text = AR.Reporter;
                ObjectName.Text = AR.ObjectName;
                ObjectPos.Text = AR.Position;
                Abusername.Text = AR.Abuser;
                Location.Text = AR.Location;
                Summary.Text = AR.Summary;
                Details.Text = AR.Details;
                AssignedTo.Text = AR.AssignedTo;
                Active.Text = AR.Active;
                Checked.Text = AR.Checked;
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

            AbuseReport AR = RegionData.GetAbuseReport(formNumber);
            if (AR.ReportNumber != null)
            {
                Category.Text = AR.Category;
                ReporterName.Text = AR.Reporter;
                ObjectName.Text = AR.ObjectName;
                Abusername.Text = AR.Abuser;
                Location.Text = AR.Position;
                Details.Text = AR.Details;
                Summary.Text = AR.Summary;
                ObjectPos.Text = AR.Position;
                AssignedTo.Text = AR.AssignedTo;
                Active.Text = AR.Active;
                Checked.Text = AR.Checked;
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
            AbuseReport AR = RegionData.GetAbuseReport(formNumber);
            if (AR.ReportNumber != null)
            {
                Category.Text = AR.Category;
                ReporterName.Text = AR.Reporter;
                ObjectName.Text = AR.ObjectName;
                Abusername.Text = AR.Abuser;
                Location.Text = AR.Position;
                Details.Text = AR.Details;
                Summary.Text = AR.Summary;
                ObjectPos.Text = AR.Position;
                AssignedTo.Text = AR.AssignedTo;
                Active.Text = AR.Active;
                Checked.Text = AR.Checked;
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
            GenericData.Update("abusereports", new string[] { AssignedTo.Text }, new string[] { "AssignedTo" }, new string[] { "number" }, new string[] { formNumber.ToString() });
        }

        private void textBox8_TextChanged(object sender, EventArgs e)
        {
            GenericData.Update("abusereports", new string[] { Active.Text }, new string[] { "Active" }, new string[] { "number" }, new string[] { formNumber.ToString() });
        }

        private void textBox10_TextChanged(object sender, EventArgs e)
        {
            GenericData.Update("abusereports", new string[] { Checked.Text }, new string[] { "Checked" }, new string[] { "number" }, new string[] { formNumber.ToString() });
        }

        private void textBox12_TextChanged(object sender, EventArgs e)
        {
            GenericData.Update("abusereports", new string[] { Notes.Text }, new string[] { "Notes" }, new string[] { "number" }, new string[] { formNumber.ToString() });
        }
    }
}
