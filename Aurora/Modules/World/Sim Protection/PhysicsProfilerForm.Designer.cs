namespace Aurora.Modules
{
    partial class PhysicsProfilerForm
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
            this.RegionNameSelector = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.PhysicsTaintBox = new System.Windows.Forms.PictureBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.PhysicsMoveTimeBox = new System.Windows.Forms.PictureBox();
            this.label4 = new System.Windows.Forms.Label();
            this.CollisionOptimizedTimeBox = new System.Windows.Forms.PictureBox();
            this.label5 = new System.Windows.Forms.Label();
            this.SendCollisionsTimeBox = new System.Windows.Forms.PictureBox();
            this.label6 = new System.Windows.Forms.Label();
            this.AvatarUpdatePosAndVelocityBox = new System.Windows.Forms.PictureBox();
            this.label7 = new System.Windows.Forms.Label();
            this.PrimUpdatePosAndVelocityBox = new System.Windows.Forms.PictureBox();
            this.label8 = new System.Windows.Forms.Label();
            this.UnlockedTimeBox = new System.Windows.Forms.PictureBox();
            this.label9 = new System.Windows.Forms.Label();
            this.MaxValBox = new System.Windows.Forms.TextBox();
            this.Change = new System.Windows.Forms.Button();
            this.Max1 = new System.Windows.Forms.Label();
            this.Max2 = new System.Windows.Forms.Label();
            this.Max3 = new System.Windows.Forms.Label();
            this.Max4 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.HMax1 = new System.Windows.Forms.Label();
            this.HMax2 = new System.Windows.Forms.Label();
            this.HMax3 = new System.Windows.Forms.Label();
            this.HMax4 = new System.Windows.Forms.Label();
            this.HMax5 = new System.Windows.Forms.Label();
            this.label23 = new System.Windows.Forms.Label();
            this.Max5 = new System.Windows.Forms.Label();
            this.HMax6 = new System.Windows.Forms.Label();
            this.label26 = new System.Windows.Forms.Label();
            this.Max6 = new System.Windows.Forms.Label();
            this.HMax7 = new System.Windows.Forms.Label();
            this.label29 = new System.Windows.Forms.Label();
            this.Max7 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.PhysicsTaintBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PhysicsMoveTimeBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.CollisionOptimizedTimeBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SendCollisionsTimeBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.AvatarUpdatePosAndVelocityBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PrimUpdatePosAndVelocityBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.UnlockedTimeBox)).BeginInit();
            this.SuspendLayout();
            // 
            // RegionNameSelector
            // 
            this.RegionNameSelector.FormattingEnabled = true;
            this.RegionNameSelector.Location = new System.Drawing.Point(90, 12);
            this.RegionNameSelector.Name = "RegionNameSelector";
            this.RegionNameSelector.Size = new System.Drawing.Size(121, 21);
            this.RegionNameSelector.TabIndex = 0;
            this.RegionNameSelector.SelectedIndexChanged += new System.EventHandler(this.RegionNameSelector_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(72, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Region Name";
            // 
            // PhysicsTaintBox
            // 
            this.PhysicsTaintBox.Location = new System.Drawing.Point(35, 75);
            this.PhysicsTaintBox.Name = "PhysicsTaintBox";
            this.PhysicsTaintBox.Size = new System.Drawing.Size(200, 200);
            this.PhysicsTaintBox.TabIndex = 2;
            this.PhysicsTaintBox.TabStop = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(87, 50);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(96, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Physics Taint Time";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(322, 50);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(99, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Physics Move Time";
            // 
            // PhysicsMoveTimeBox
            // 
            this.PhysicsMoveTimeBox.Location = new System.Drawing.Point(271, 75);
            this.PhysicsMoveTimeBox.Name = "PhysicsMoveTimeBox";
            this.PhysicsMoveTimeBox.Size = new System.Drawing.Size(200, 200);
            this.PhysicsMoveTimeBox.TabIndex = 4;
            this.PhysicsMoveTimeBox.TabStop = false;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(548, 50);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(120, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Collision Optimized Time";
            // 
            // CollisionOptimizedTimeBox
            // 
            this.CollisionOptimizedTimeBox.Location = new System.Drawing.Point(508, 75);
            this.CollisionOptimizedTimeBox.Name = "CollisionOptimizedTimeBox";
            this.CollisionOptimizedTimeBox.Size = new System.Drawing.Size(200, 200);
            this.CollisionOptimizedTimeBox.TabIndex = 6;
            this.CollisionOptimizedTimeBox.TabStop = false;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(793, 50);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(104, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "Send Collisions Time";
            // 
            // SendCollisionsTimeBox
            // 
            this.SendCollisionsTimeBox.Location = new System.Drawing.Point(745, 75);
            this.SendCollisionsTimeBox.Name = "SendCollisionsTimeBox";
            this.SendCollisionsTimeBox.Size = new System.Drawing.Size(200, 200);
            this.SendCollisionsTimeBox.TabIndex = 8;
            this.SendCollisionsTimeBox.TabStop = false;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(56, 312);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(159, 13);
            this.label6.TabIndex = 11;
            this.label6.Text = "Avatar Update Pos And Velocity";
            // 
            // AvatarUpdatePosAndVelocityBox
            // 
            this.AvatarUpdatePosAndVelocityBox.Location = new System.Drawing.Point(35, 338);
            this.AvatarUpdatePosAndVelocityBox.Name = "AvatarUpdatePosAndVelocityBox";
            this.AvatarUpdatePosAndVelocityBox.Size = new System.Drawing.Size(200, 200);
            this.AvatarUpdatePosAndVelocityBox.TabIndex = 10;
            this.AvatarUpdatePosAndVelocityBox.TabStop = false;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(297, 312);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(148, 13);
            this.label7.TabIndex = 13;
            this.label7.Text = "Prim Update Pos And Velocity";
            // 
            // PrimUpdatePosAndVelocityBox
            // 
            this.PrimUpdatePosAndVelocityBox.Location = new System.Drawing.Point(271, 338);
            this.PrimUpdatePosAndVelocityBox.Name = "PrimUpdatePosAndVelocityBox";
            this.PrimUpdatePosAndVelocityBox.Size = new System.Drawing.Size(200, 200);
            this.PrimUpdatePosAndVelocityBox.TabIndex = 12;
            this.PrimUpdatePosAndVelocityBox.TabStop = false;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(568, 312);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(79, 13);
            this.label8.TabIndex = 15;
            this.label8.Text = "Unlocked Time";
            // 
            // UnlockedTimeBox
            // 
            this.UnlockedTimeBox.Location = new System.Drawing.Point(507, 338);
            this.UnlockedTimeBox.Name = "UnlockedTimeBox";
            this.UnlockedTimeBox.Size = new System.Drawing.Size(200, 200);
            this.UnlockedTimeBox.TabIndex = 14;
            this.UnlockedTimeBox.TabStop = false;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(243, 15);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(97, 13);
            this.label9.TabIndex = 16;
            this.label9.Text = "Max Value for stats";
            // 
            // MaxValBox
            // 
            this.MaxValBox.Location = new System.Drawing.Point(346, 13);
            this.MaxValBox.Name = "MaxValBox";
            this.MaxValBox.Size = new System.Drawing.Size(100, 20);
            this.MaxValBox.TabIndex = 17;
            this.MaxValBox.Text = "200";
            // 
            // Change
            // 
            this.Change.Location = new System.Drawing.Point(452, 12);
            this.Change.Name = "Change";
            this.Change.Size = new System.Drawing.Size(75, 23);
            this.Change.TabIndex = 18;
            this.Change.Text = "Set";
            this.Change.UseVisualStyleBackColor = true;
            this.Change.Click += new System.EventHandler(this.Change_Click);
            // 
            // Max1
            // 
            this.Max1.AutoSize = true;
            this.Max1.Location = new System.Drawing.Point(8, 75);
            this.Max1.Name = "Max1";
            this.Max1.Size = new System.Drawing.Size(25, 13);
            this.Max1.TabIndex = 19;
            this.Max1.Text = "200";
            // 
            // Max2
            // 
            this.Max2.AutoSize = true;
            this.Max2.Location = new System.Drawing.Point(241, 75);
            this.Max2.Name = "Max2";
            this.Max2.Size = new System.Drawing.Size(25, 13);
            this.Max2.TabIndex = 20;
            this.Max2.Text = "200";
            // 
            // Max3
            // 
            this.Max3.AutoSize = true;
            this.Max3.Location = new System.Drawing.Point(477, 75);
            this.Max3.Name = "Max3";
            this.Max3.Size = new System.Drawing.Size(25, 13);
            this.Max3.TabIndex = 21;
            this.Max3.Text = "200";
            // 
            // Max4
            // 
            this.Max4.AutoSize = true;
            this.Max4.Location = new System.Drawing.Point(714, 75);
            this.Max4.Name = "Max4";
            this.Max4.Size = new System.Drawing.Size(25, 13);
            this.Max4.TabIndex = 22;
            this.Max4.Text = "200";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(726, 262);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(13, 13);
            this.label14.TabIndex = 23;
            this.label14.Text = "0";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(489, 262);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(13, 13);
            this.label15.TabIndex = 24;
            this.label15.Text = "0";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(253, 262);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(13, 13);
            this.label16.TabIndex = 25;
            this.label16.Text = "0";
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(16, 262);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(13, 13);
            this.label17.TabIndex = 26;
            this.label17.Text = "0";
            // 
            // HMax1
            // 
            this.HMax1.AutoSize = true;
            this.HMax1.Location = new System.Drawing.Point(8, 169);
            this.HMax1.Name = "HMax1";
            this.HMax1.Size = new System.Drawing.Size(25, 13);
            this.HMax1.TabIndex = 27;
            this.HMax1.Text = "100";
            // 
            // HMax2
            // 
            this.HMax2.AutoSize = true;
            this.HMax2.Location = new System.Drawing.Point(243, 169);
            this.HMax2.Name = "HMax2";
            this.HMax2.Size = new System.Drawing.Size(25, 13);
            this.HMax2.TabIndex = 28;
            this.HMax2.Text = "100";
            // 
            // HMax3
            // 
            this.HMax3.AutoSize = true;
            this.HMax3.Location = new System.Drawing.Point(477, 169);
            this.HMax3.Name = "HMax3";
            this.HMax3.Size = new System.Drawing.Size(25, 13);
            this.HMax3.TabIndex = 29;
            this.HMax3.Text = "100";
            // 
            // HMax4
            // 
            this.HMax4.AutoSize = true;
            this.HMax4.Location = new System.Drawing.Point(714, 169);
            this.HMax4.Name = "HMax4";
            this.HMax4.Size = new System.Drawing.Size(25, 13);
            this.HMax4.TabIndex = 30;
            this.HMax4.Text = "100";
            // 
            // HMax5
            // 
            this.HMax5.AutoSize = true;
            this.HMax5.Location = new System.Drawing.Point(8, 432);
            this.HMax5.Name = "HMax5";
            this.HMax5.Size = new System.Drawing.Size(25, 13);
            this.HMax5.TabIndex = 33;
            this.HMax5.Text = "100";
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.Location = new System.Drawing.Point(16, 525);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(13, 13);
            this.label23.TabIndex = 32;
            this.label23.Text = "0";
            // 
            // Max5
            // 
            this.Max5.AutoSize = true;
            this.Max5.Location = new System.Drawing.Point(4, 338);
            this.Max5.Name = "Max5";
            this.Max5.Size = new System.Drawing.Size(25, 13);
            this.Max5.TabIndex = 31;
            this.Max5.Text = "200";
            // 
            // HMax6
            // 
            this.HMax6.AutoSize = true;
            this.HMax6.Location = new System.Drawing.Point(244, 432);
            this.HMax6.Name = "HMax6";
            this.HMax6.Size = new System.Drawing.Size(25, 13);
            this.HMax6.TabIndex = 36;
            this.HMax6.Text = "100";
            // 
            // label26
            // 
            this.label26.AutoSize = true;
            this.label26.Location = new System.Drawing.Point(252, 525);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(13, 13);
            this.label26.TabIndex = 35;
            this.label26.Text = "0";
            // 
            // Max6
            // 
            this.Max6.AutoSize = true;
            this.Max6.Location = new System.Drawing.Point(240, 338);
            this.Max6.Name = "Max6";
            this.Max6.Size = new System.Drawing.Size(25, 13);
            this.Max6.TabIndex = 34;
            this.Max6.Text = "200";
            // 
            // HMax7
            // 
            this.HMax7.AutoSize = true;
            this.HMax7.Location = new System.Drawing.Point(476, 432);
            this.HMax7.Name = "HMax7";
            this.HMax7.Size = new System.Drawing.Size(25, 13);
            this.HMax7.TabIndex = 39;
            this.HMax7.Text = "100";
            // 
            // label29
            // 
            this.label29.AutoSize = true;
            this.label29.Location = new System.Drawing.Point(484, 525);
            this.label29.Name = "label29";
            this.label29.Size = new System.Drawing.Size(13, 13);
            this.label29.TabIndex = 38;
            this.label29.Text = "0";
            // 
            // Max7
            // 
            this.Max7.AutoSize = true;
            this.Max7.Location = new System.Drawing.Point(472, 338);
            this.Max7.Name = "Max7";
            this.Max7.Size = new System.Drawing.Size(25, 13);
            this.Max7.TabIndex = 37;
            this.Max7.Text = "200";
            // 
            // PhysicsProfilerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(962, 550);
            this.Controls.Add(this.HMax7);
            this.Controls.Add(this.label29);
            this.Controls.Add(this.Max7);
            this.Controls.Add(this.HMax6);
            this.Controls.Add(this.label26);
            this.Controls.Add(this.Max6);
            this.Controls.Add(this.HMax5);
            this.Controls.Add(this.label23);
            this.Controls.Add(this.Max5);
            this.Controls.Add(this.HMax4);
            this.Controls.Add(this.HMax3);
            this.Controls.Add(this.HMax2);
            this.Controls.Add(this.HMax1);
            this.Controls.Add(this.label17);
            this.Controls.Add(this.label16);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.Max4);
            this.Controls.Add(this.Max3);
            this.Controls.Add(this.Max2);
            this.Controls.Add(this.Max1);
            this.Controls.Add(this.Change);
            this.Controls.Add(this.MaxValBox);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.UnlockedTimeBox);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.PrimUpdatePosAndVelocityBox);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.AvatarUpdatePosAndVelocityBox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.SendCollisionsTimeBox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.CollisionOptimizedTimeBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.PhysicsMoveTimeBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.PhysicsTaintBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.RegionNameSelector);
            this.Name = "PhysicsProfilerForm";
            this.Text = "PhysicsProfilerForm";
            this.Load += new System.EventHandler(this.PhysicsProfilerForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.PhysicsTaintBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PhysicsMoveTimeBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.CollisionOptimizedTimeBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SendCollisionsTimeBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.AvatarUpdatePosAndVelocityBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PrimUpdatePosAndVelocityBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.UnlockedTimeBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox RegionNameSelector;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PictureBox PhysicsTaintBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.PictureBox PhysicsMoveTimeBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.PictureBox CollisionOptimizedTimeBox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.PictureBox SendCollisionsTimeBox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.PictureBox AvatarUpdatePosAndVelocityBox;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.PictureBox PrimUpdatePosAndVelocityBox;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.PictureBox UnlockedTimeBox;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox MaxValBox;
        private System.Windows.Forms.Button Change;
        private System.Windows.Forms.Label Max1;
        private System.Windows.Forms.Label Max2;
        private System.Windows.Forms.Label Max3;
        private System.Windows.Forms.Label Max4;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label HMax1;
        private System.Windows.Forms.Label HMax2;
        private System.Windows.Forms.Label HMax3;
        private System.Windows.Forms.Label HMax4;
        private System.Windows.Forms.Label HMax5;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.Label Max5;
        private System.Windows.Forms.Label HMax6;
        private System.Windows.Forms.Label label26;
        private System.Windows.Forms.Label Max6;
        private System.Windows.Forms.Label HMax7;
        private System.Windows.Forms.Label label29;
        private System.Windows.Forms.Label Max7;
    }
}