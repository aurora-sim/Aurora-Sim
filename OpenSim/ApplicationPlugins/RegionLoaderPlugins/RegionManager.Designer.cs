namespace Aurora.Modules.RegionLoader
{
    partial class RegionManager
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
            this.RName = new System.Windows.Forms.TextBox();
            this.Create = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.LocX = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.LocY = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.Port = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.ExternalIP = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.Type = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.MaxNonPhys = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.MaxPhys = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.ObjectCount = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.Maturity = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.Disabled = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.label17 = new System.Windows.Forms.Label();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.label16 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // RName
            // 
            this.RName.Location = new System.Drawing.Point(247, 13);
            this.RName.Name = "RName";
            this.RName.Size = new System.Drawing.Size(100, 20);
            this.RName.TabIndex = 2;
            // 
            // Create
            // 
            this.Create.Location = new System.Drawing.Point(541, 363);
            this.Create.Name = "Create";
            this.Create.Size = new System.Drawing.Size(104, 23);
            this.Create.TabIndex = 1;
            this.Create.Text = "Create my Region!";
            this.Create.UseVisualStyleBackColor = true;
            this.Create.Click += new System.EventHandler(this.CreateNewRegion);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(72, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Region Name";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 42);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(95, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Region Location X";
            // 
            // LocX
            // 
            this.LocX.Location = new System.Drawing.Point(247, 39);
            this.LocX.Name = "LocX";
            this.LocX.Size = new System.Drawing.Size(100, 20);
            this.LocX.TabIndex = 4;
            this.LocX.Text = "1000";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 68);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(95, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Region Location Y";
            // 
            // LocY
            // 
            this.LocY.Location = new System.Drawing.Point(247, 65);
            this.LocY.Name = "LocY";
            this.LocY.Size = new System.Drawing.Size(100, 20);
            this.LocY.TabIndex = 6;
            this.LocY.Text = "1000";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 94);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(63, 13);
            this.label4.TabIndex = 9;
            this.label4.Text = "Region Port";
            // 
            // Port
            // 
            this.Port.Location = new System.Drawing.Point(247, 91);
            this.Port.Name = "Port";
            this.Port.Size = new System.Drawing.Size(100, 20);
            this.Port.TabIndex = 8;
            this.Port.Text = "9000";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 120);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(58, 13);
            this.label5.TabIndex = 11;
            this.label5.Text = "External IP";
            // 
            // ExternalIP
            // 
            this.ExternalIP.Location = new System.Drawing.Point(247, 117);
            this.ExternalIP.Name = "ExternalIP";
            this.ExternalIP.Size = new System.Drawing.Size(100, 20);
            this.ExternalIP.TabIndex = 10;
            this.ExternalIP.Text = "DEFAULT";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(26, 139);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(321, 12);
            this.label6.TabIndex = 12;
            this.label6.Text = "Note: Use \'DEFAULT\' (without the quotes) to have the IP automatically found";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(6, 157);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(68, 13);
            this.label7.TabIndex = 14;
            this.label7.Text = "Region Type";
            // 
            // Type
            // 
            this.Type.Location = new System.Drawing.Point(247, 154);
            this.Type.Name = "Type";
            this.Type.Size = new System.Drawing.Size(100, 20);
            this.Type.TabIndex = 13;
            this.Type.Text = "Mainland";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(6, 183);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(162, 13);
            this.label8.TabIndex = 16;
            this.label8.Text = "Maximum Non-Physical Prim Size";
            // 
            // MaxNonPhys
            // 
            this.MaxNonPhys.Location = new System.Drawing.Point(247, 180);
            this.MaxNonPhys.Name = "MaxNonPhys";
            this.MaxNonPhys.Size = new System.Drawing.Size(100, 20);
            this.MaxNonPhys.TabIndex = 15;
            this.MaxNonPhys.Text = "256";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(6, 209);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(139, 13);
            this.label9.TabIndex = 18;
            this.label9.Text = "Maximum Physical Prim Size";
            // 
            // MaxPhys
            // 
            this.MaxPhys.Location = new System.Drawing.Point(247, 206);
            this.MaxPhys.Name = "MaxPhys";
            this.MaxPhys.Size = new System.Drawing.Size(100, 20);
            this.MaxPhys.TabIndex = 17;
            this.MaxPhys.Text = "50";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(6, 235);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(146, 13);
            this.label10.TabIndex = 20;
            this.label10.Text = "Maximum Prims in this Region";
            // 
            // ObjectCount
            // 
            this.ObjectCount.Location = new System.Drawing.Point(247, 232);
            this.ObjectCount.Name = "ObjectCount";
            this.ObjectCount.Size = new System.Drawing.Size(100, 20);
            this.ObjectCount.TabIndex = 19;
            this.ObjectCount.Text = "65536";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(6, 261);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(44, 13);
            this.label11.TabIndex = 22;
            this.label11.Text = "Maturity";
            // 
            // Maturity
            // 
            this.Maturity.Location = new System.Drawing.Point(247, 258);
            this.Maturity.Name = "Maturity";
            this.Maturity.Size = new System.Drawing.Size(100, 20);
            this.Maturity.TabIndex = 21;
            this.Maturity.Text = "0";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(6, 287);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(48, 13);
            this.label12.TabIndex = 24;
            this.label12.Text = "Disabled";
            // 
            // Disabled
            // 
            this.Disabled.Location = new System.Drawing.Point(247, 284);
            this.Disabled.Name = "Disabled";
            this.Disabled.Size = new System.Drawing.Size(100, 20);
            this.Disabled.TabIndex = 23;
            this.Disabled.Text = "false";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label13.Location = new System.Drawing.Point(26, 274);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(141, 12);
            this.label13.TabIndex = 25;
            this.label13.Text = "Note: 0 - PG, 1 - Mature, 2 - Adult";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Font = new System.Drawing.Font("Microsoft Sans Serif", 22F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label14.Location = new System.Drawing.Point(161, 22);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(335, 36);
            this.label14.TabIndex = 26;
            this.label14.Text = "Aurora Region Manager";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.label13);
            this.groupBox1.Controls.Add(this.RName);
            this.groupBox1.Controls.Add(this.label12);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.Disabled);
            this.groupBox1.Controls.Add(this.LocX);
            this.groupBox1.Controls.Add(this.label11);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.Maturity);
            this.groupBox1.Controls.Add(this.LocY);
            this.groupBox1.Controls.Add(this.label10);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.ObjectCount);
            this.groupBox1.Controls.Add(this.Port);
            this.groupBox1.Controls.Add(this.label9);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.MaxPhys);
            this.groupBox1.Controls.Add(this.ExternalIP);
            this.groupBox1.Controls.Add(this.label8);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.MaxNonPhys);
            this.groupBox1.Controls.Add(this.Type);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Location = new System.Drawing.Point(12, 66);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(362, 320);
            this.groupBox1.TabIndex = 27;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Region Info";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.radioButton2);
            this.groupBox2.Controls.Add(this.label17);
            this.groupBox2.Controls.Add(this.pictureBox2);
            this.groupBox2.Controls.Add(this.radioButton1);
            this.groupBox2.Controls.Add(this.label16);
            this.groupBox2.Controls.Add(this.label15);
            this.groupBox2.Controls.Add(this.pictureBox1);
            this.groupBox2.Location = new System.Drawing.Point(380, 73);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(265, 284);
            this.groupBox2.TabIndex = 28;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Region Settings";
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Location = new System.Drawing.Point(128, 132);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(68, 17);
            this.radioButton2.TabIndex = 6;
            this.radioButton2.TabStop = true;
            this.radioButton2.Text = "Default 2";
            this.radioButton2.UseVisualStyleBackColor = true;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(134, 65);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(79, 13);
            this.label17.TabIndex = 5;
            this.label17.Text = "Coming Soon...";
            // 
            // pictureBox2
            // 
            this.pictureBox2.Location = new System.Drawing.Point(122, 45);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(110, 81);
            this.pictureBox2.TabIndex = 4;
            this.pictureBox2.TabStop = false;
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Location = new System.Drawing.Point(12, 132);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(59, 17);
            this.radioButton1.TabIndex = 3;
            this.radioButton1.TabStop = true;
            this.radioButton1.Text = "Default";
            this.radioButton1.UseVisualStyleBackColor = true;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(18, 65);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(79, 13);
            this.label16.TabIndex = 2;
            this.label16.Text = "Coming Soon...";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(6, 29);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(105, 13);
            this.label15.TabIndex = 1;
            this.label15.Text = "Default Region Look";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(6, 45);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(110, 81);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // RegionManager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(657, 397);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.Create);
            this.Controls.Add(this.groupBox1);
            this.Name = "RegionManager";
            this.Text = "RegionManager";
            this.Load += new System.EventHandler(this.RegionManager_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox RName;
        private System.Windows.Forms.Button Create;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox LocX;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox LocY;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox Port;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox ExternalIP;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox Type;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox MaxNonPhys;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox MaxPhys;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox ObjectCount;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox Maturity;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox Disabled;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}