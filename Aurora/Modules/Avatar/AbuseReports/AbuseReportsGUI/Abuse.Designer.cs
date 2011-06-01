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

namespace Aurora.Modules.AbuseReportsGUI
{
    partial class Abuse
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.Category = new System.Windows.Forms.TextBox();
            this.ReporterName = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.Previous = new System.Windows.Forms.Button();
            this.Next = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.CardNumber = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.Abusername = new System.Windows.Forms.TextBox();
            this.AbuseLocation = new System.Windows.Forms.TextBox();
            this.Details = new System.Windows.Forms.TextBox();
            this.Summary = new System.Windows.Forms.TextBox();
            this.AssignedTo = new System.Windows.Forms.TextBox();
            this.Active = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.ObjectName = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.Checked = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.ObjectPos = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.Notes = new System.Windows.Forms.TextBox();
            this.label15 = new System.Windows.Forms.Label();
            this.GotoARNumber = new System.Windows.Forms.TextBox();
            this.GotoAR = new System.Windows.Forms.Button();
            this.SnapshotUUID = new System.Windows.Forms.PictureBox();
            this.ScreenshotLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.SnapshotUUID)).BeginInit();
            this.SuspendLayout();
            // 
            // Category
            // 
            this.Category.Location = new System.Drawing.Point(178, 44);
            this.Category.Name = "Category";
            this.Category.Size = new System.Drawing.Size(260, 20);
            this.Category.TabIndex = 0;
            // 
            // ReporterName
            // 
            this.ReporterName.Location = new System.Drawing.Point(178, 70);
            this.ReporterName.Name = "ReporterName";
            this.ReporterName.Size = new System.Drawing.Size(260, 20);
            this.ReporterName.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Location = new System.Drawing.Point(32, 51);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(49, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Category";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Location = new System.Drawing.Point(32, 77);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(48, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Reporter";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.Color.Transparent;
            this.label3.Location = new System.Drawing.Point(203, 9);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(77, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Abuse Reports";
            this.label3.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // Previous
            // 
            this.Previous.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Previous.Location = new System.Drawing.Point(35, 366);
            this.Previous.Name = "Previous";
            this.Previous.Size = new System.Drawing.Size(75, 23);
            this.Previous.TabIndex = 5;
            this.Previous.Text = "Previous";
            this.Previous.UseVisualStyleBackColor = true;
            this.Previous.Click += new System.EventHandler(this.button1_Click);
            // 
            // Next
            // 
            this.Next.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Next.Location = new System.Drawing.Point(363, 366);
            this.Next.Name = "Next";
            this.Next.Size = new System.Drawing.Size(75, 23);
            this.Next.TabIndex = 6;
            this.Next.Text = "Next";
            this.Next.UseVisualStyleBackColor = true;
            this.Next.Click += new System.EventHandler(this.button2_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.BackColor = System.Drawing.Color.Transparent;
            this.label4.Location = new System.Drawing.Point(32, 28);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(82, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Abuse Report #";
            // 
            // CardNumber
            // 
            this.CardNumber.AutoSize = true;
            this.CardNumber.BackColor = System.Drawing.Color.Transparent;
            this.CardNumber.Location = new System.Drawing.Point(175, 28);
            this.CardNumber.Name = "CardNumber";
            this.CardNumber.Size = new System.Drawing.Size(13, 13);
            this.CardNumber.TabIndex = 8;
            this.CardNumber.Text = "1";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.BackColor = System.Drawing.Color.Transparent;
            this.label5.Location = new System.Drawing.Point(32, 155);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(40, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "Abuser";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.BackColor = System.Drawing.Color.Transparent;
            this.label6.Location = new System.Drawing.Point(32, 181);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(48, 13);
            this.label6.TabIndex = 10;
            this.label6.Text = "Location";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.BackColor = System.Drawing.Color.Transparent;
            this.label7.Location = new System.Drawing.Point(32, 233);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(39, 13);
            this.label7.TabIndex = 11;
            this.label7.Text = "Details";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.BackColor = System.Drawing.Color.Transparent;
            this.label8.Location = new System.Drawing.Point(32, 207);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(50, 13);
            this.label8.TabIndex = 12;
            this.label8.Text = "Summary";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.BackColor = System.Drawing.Color.Transparent;
            this.label9.Location = new System.Drawing.Point(32, 259);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(66, 13);
            this.label9.TabIndex = 13;
            this.label9.Text = "Assigned To";
            // 
            // Abusername
            // 
            this.Abusername.Location = new System.Drawing.Point(178, 148);
            this.Abusername.Name = "Abusername";
            this.Abusername.Size = new System.Drawing.Size(260, 20);
            this.Abusername.TabIndex = 14;
            // 
            // Location
            // 
            this.AbuseLocation.Location = new System.Drawing.Point(178, 174);
            this.AbuseLocation.Name = "Location";
            this.AbuseLocation.Size = new System.Drawing.Size(260, 20);
            this.AbuseLocation.TabIndex = 15;
            // 
            // Details
            // 
            this.Details.Location = new System.Drawing.Point(178, 226);
            this.Details.Name = "Details";
            this.Details.Size = new System.Drawing.Size(260, 20);
            this.Details.TabIndex = 16;
            // 
            // Summary
            // 
            this.Summary.Location = new System.Drawing.Point(178, 200);
            this.Summary.Name = "Summary";
            this.Summary.Size = new System.Drawing.Size(260, 20);
            this.Summary.TabIndex = 17;
            // 
            // AssignedTo
            // 
            this.AssignedTo.Location = new System.Drawing.Point(178, 252);
            this.AssignedTo.Name = "AssignedTo";
            this.AssignedTo.Size = new System.Drawing.Size(260, 20);
            this.AssignedTo.TabIndex = 18;
            this.AssignedTo.TextChanged += new System.EventHandler(this.textBox7_TextChanged);
            // 
            // Active
            // 
            this.Active.Location = new System.Drawing.Point(178, 278);
            this.Active.Name = "Active";
            this.Active.Size = new System.Drawing.Size(260, 20);
            this.Active.TabIndex = 19;
            this.Active.TextChanged += new System.EventHandler(this.textBox8_TextChanged);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.BackColor = System.Drawing.Color.Transparent;
            this.label10.Location = new System.Drawing.Point(32, 285);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(37, 13);
            this.label10.TabIndex = 20;
            this.label10.Text = "Active";
            // 
            // ObjectName
            // 
            this.ObjectName.Location = new System.Drawing.Point(178, 96);
            this.ObjectName.Name = "ObjectName";
            this.ObjectName.Size = new System.Drawing.Size(260, 20);
            this.ObjectName.TabIndex = 21;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.BackColor = System.Drawing.Color.Transparent;
            this.label11.Location = new System.Drawing.Point(32, 103);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(69, 13);
            this.label11.TabIndex = 22;
            this.label11.Text = "Object Name";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.BackColor = System.Drawing.Color.Transparent;
            this.label12.Location = new System.Drawing.Point(32, 311);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(50, 13);
            this.label12.TabIndex = 23;
            this.label12.Text = "Checked";
            // 
            // Checked
            // 
            this.Checked.Location = new System.Drawing.Point(178, 304);
            this.Checked.Name = "Checked";
            this.Checked.Size = new System.Drawing.Size(260, 20);
            this.Checked.TabIndex = 24;
            this.Checked.TextChanged += new System.EventHandler(this.textBox10_TextChanged);
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.BackColor = System.Drawing.Color.Transparent;
            this.label13.Location = new System.Drawing.Point(32, 129);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(59, 13);
            this.label13.TabIndex = 26;
            this.label13.Text = "Object Pos";
            // 
            // ObjectPos
            // 
            this.ObjectPos.Location = new System.Drawing.Point(178, 122);
            this.ObjectPos.Name = "ObjectPos";
            this.ObjectPos.Size = new System.Drawing.Size(260, 20);
            this.ObjectPos.TabIndex = 25;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.BackColor = System.Drawing.Color.Transparent;
            this.label14.Location = new System.Drawing.Point(32, 333);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(35, 13);
            this.label14.TabIndex = 27;
            this.label14.Text = "Notes";
            // 
            // Notes
            // 
            this.Notes.Location = new System.Drawing.Point(178, 330);
            this.Notes.Name = "Notes";
            this.Notes.Size = new System.Drawing.Size(260, 20);
            this.Notes.TabIndex = 28;
            this.Notes.TextChanged += new System.EventHandler(this.textBox12_TextChanged);
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(128, 371);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(104, 13);
            this.label15.TabIndex = 29;
            this.label15.Text = "Goto Abuse Report: ";
            // 
            // GotoARNumber
            // 
            this.GotoARNumber.Location = new System.Drawing.Point(229, 368);
            this.GotoARNumber.Name = "GotoARNumber";
            this.GotoARNumber.Size = new System.Drawing.Size(51, 20);
            this.GotoARNumber.TabIndex = 30;
            // 
            // GotoAR
            // 
            this.GotoAR.Location = new System.Drawing.Point(282, 366);
            this.GotoAR.Name = "GotoAR";
            this.GotoAR.Size = new System.Drawing.Size(47, 23);
            this.GotoAR.TabIndex = 31;
            this.GotoAR.Text = "Go";
            this.GotoAR.UseVisualStyleBackColor = true;
            this.GotoAR.Click += new System.EventHandler(this.GotoAR_Click);
            // 
            // SnapshotUUID
            // 
            this.SnapshotUUID.Location = new System.Drawing.Point(444, 77);
            this.SnapshotUUID.Name = "SnapshotUUID";
            this.SnapshotUUID.Size = new System.Drawing.Size(292, 235);
            this.SnapshotUUID.TabIndex = 32;
            this.SnapshotUUID.TabStop = false;
            // 
            // ScreenshotLabel
            // 
            this.ScreenshotLabel.AutoSize = true;
            this.ScreenshotLabel.BackColor = System.Drawing.Color.Transparent;
            this.ScreenshotLabel.Location = new System.Drawing.Point(549, 61);
            this.ScreenshotLabel.Name = "ScreenshotLabel";
            this.ScreenshotLabel.Size = new System.Drawing.Size(61, 13);
            this.ScreenshotLabel.TabIndex = 33;
            this.ScreenshotLabel.Text = "Screenshot";
            this.ScreenshotLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // Abuse
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(748, 401);
            this.Controls.Add(this.ScreenshotLabel);
            this.Controls.Add(this.SnapshotUUID);
            this.Controls.Add(this.GotoAR);
            this.Controls.Add(this.GotoARNumber);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.Notes);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.ObjectPos);
            this.Controls.Add(this.Checked);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.ObjectName);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.Active);
            this.Controls.Add(this.AssignedTo);
            this.Controls.Add(this.Summary);
            this.Controls.Add(this.Details);
            this.Controls.Add(this.AbuseLocation);
            this.Controls.Add(this.Abusername);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.CardNumber);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.Next);
            this.Controls.Add(this.Previous);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ReporterName);
            this.Controls.Add(this.Category);
            this.Name = "Abuse";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Abuse Reports";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.SnapshotUUID)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox Category;
        private System.Windows.Forms.TextBox ReporterName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button Previous;
        private System.Windows.Forms.Button Next;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label CardNumber;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox Abusername;
        private System.Windows.Forms.TextBox AbuseLocation;
        private System.Windows.Forms.TextBox Details;
        private System.Windows.Forms.TextBox Summary;
        private System.Windows.Forms.TextBox AssignedTo;
        private System.Windows.Forms.TextBox Active;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox ObjectName;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox Checked;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox ObjectPos;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.TextBox Notes;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TextBox GotoARNumber;
        private System.Windows.Forms.Button GotoAR;
        private System.Windows.Forms.PictureBox SnapshotUUID;
        private System.Windows.Forms.Label ScreenshotLabel;
    }
}

