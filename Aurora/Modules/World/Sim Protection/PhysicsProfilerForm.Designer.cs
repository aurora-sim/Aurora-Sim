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
            this.PhysicsTaintBox.Location = new System.Drawing.Point(11, 66);
            this.PhysicsTaintBox.Name = "PhysicsTaintBox";
            this.PhysicsTaintBox.Size = new System.Drawing.Size(200, 200);
            this.PhysicsTaintBox.TabIndex = 2;
            this.PhysicsTaintBox.TabStop = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(56, 50);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(96, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Physics Taint Time";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(262, 50);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(99, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Physics Move Time";
            // 
            // PhysicsMoveTimeBox
            // 
            this.PhysicsMoveTimeBox.Location = new System.Drawing.Point(217, 66);
            this.PhysicsMoveTimeBox.Name = "PhysicsMoveTimeBox";
            this.PhysicsMoveTimeBox.Size = new System.Drawing.Size(200, 200);
            this.PhysicsMoveTimeBox.TabIndex = 4;
            this.PhysicsMoveTimeBox.TabStop = false;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(468, 50);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(120, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Collision Optimized Time";
            // 
            // CollisionOptimizedTimeBox
            // 
            this.CollisionOptimizedTimeBox.Location = new System.Drawing.Point(423, 66);
            this.CollisionOptimizedTimeBox.Name = "CollisionOptimizedTimeBox";
            this.CollisionOptimizedTimeBox.Size = new System.Drawing.Size(200, 200);
            this.CollisionOptimizedTimeBox.TabIndex = 6;
            this.CollisionOptimizedTimeBox.TabStop = false;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(674, 50);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(104, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "Send Collisions Time";
            // 
            // SendCollisionsTimeBox
            // 
            this.SendCollisionsTimeBox.Location = new System.Drawing.Point(629, 66);
            this.SendCollisionsTimeBox.Name = "SendCollisionsTimeBox";
            this.SendCollisionsTimeBox.Size = new System.Drawing.Size(200, 200);
            this.SendCollisionsTimeBox.TabIndex = 8;
            this.SendCollisionsTimeBox.TabStop = false;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(30, 278);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(159, 13);
            this.label6.TabIndex = 11;
            this.label6.Text = "Avatar Update Pos And Velocity";
            // 
            // AvatarUpdatePosAndVelocityBox
            // 
            this.AvatarUpdatePosAndVelocityBox.Location = new System.Drawing.Point(11, 294);
            this.AvatarUpdatePosAndVelocityBox.Name = "AvatarUpdatePosAndVelocityBox";
            this.AvatarUpdatePosAndVelocityBox.Size = new System.Drawing.Size(200, 200);
            this.AvatarUpdatePosAndVelocityBox.TabIndex = 10;
            this.AvatarUpdatePosAndVelocityBox.TabStop = false;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(236, 278);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(148, 13);
            this.label7.TabIndex = 13;
            this.label7.Text = "Prim Update Pos And Velocity";
            // 
            // PrimUpdatePosAndVelocityBox
            // 
            this.PrimUpdatePosAndVelocityBox.Location = new System.Drawing.Point(217, 294);
            this.PrimUpdatePosAndVelocityBox.Name = "PrimUpdatePosAndVelocityBox";
            this.PrimUpdatePosAndVelocityBox.Size = new System.Drawing.Size(200, 200);
            this.PrimUpdatePosAndVelocityBox.TabIndex = 12;
            this.PrimUpdatePosAndVelocityBox.TabStop = false;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(468, 278);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(79, 13);
            this.label8.TabIndex = 15;
            this.label8.Text = "Unlocked Time";
            // 
            // UnlockedTimeBox
            // 
            this.UnlockedTimeBox.Location = new System.Drawing.Point(423, 294);
            this.UnlockedTimeBox.Name = "UnlockedTimeBox";
            this.UnlockedTimeBox.Size = new System.Drawing.Size(200, 200);
            this.UnlockedTimeBox.TabIndex = 14;
            this.UnlockedTimeBox.TabStop = false;
            // 
            // PhysicsProfilerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(921, 550);
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
    }
}