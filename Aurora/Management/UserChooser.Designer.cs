namespace Aurora.Management
{
    partial class UserChooser
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
            this.select_user = new System.Windows.Forms.Button();
            this.listBox = new System.Windows.Forms.ListBox();
            this.search = new System.Windows.Forms.Button();
            this.user_name = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.create_user = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // select_user
            // 
            this.select_user.Location = new System.Drawing.Point(197, 227);
            this.select_user.Name = "select_user";
            this.select_user.Size = new System.Drawing.Size(75, 23);
            this.select_user.TabIndex = 0;
            this.select_user.Text = "Select user";
            this.select_user.UseVisualStyleBackColor = true;
            this.select_user.Click += new System.EventHandler(this.select_user_Click);
            // 
            // listBox
            // 
            this.listBox.FormattingEnabled = true;
            this.listBox.Location = new System.Drawing.Point(12, 106);
            this.listBox.Name = "listBox";
            this.listBox.Size = new System.Drawing.Size(260, 108);
            this.listBox.TabIndex = 1;
            // 
            // search
            // 
            this.search.Location = new System.Drawing.Point(197, 77);
            this.search.Name = "search";
            this.search.Size = new System.Drawing.Size(75, 23);
            this.search.TabIndex = 2;
            this.search.Text = "Search";
            this.search.UseVisualStyleBackColor = true;
            this.search.Click += new System.EventHandler(this.search_Click);
            // 
            // user_name
            // 
            this.user_name.Location = new System.Drawing.Point(12, 77);
            this.user_name.Name = "user_name";
            this.user_name.Size = new System.Drawing.Size(179, 20);
            this.user_name.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 56);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(63, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "User Name:";
            // 
            // create_user
            // 
            this.create_user.Location = new System.Drawing.Point(171, 46);
            this.create_user.Name = "create_user";
            this.create_user.Size = new System.Drawing.Size(101, 23);
            this.create_user.TabIndex = 5;
            this.create_user.Text = "Create this user";
            this.create_user.UseVisualStyleBackColor = true;
            this.create_user.Click += new System.EventHandler(this.create_user_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(12, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(125, 26);
            this.label2.TabIndex = 6;
            this.label2.Text = "User Picker";
            // 
            // UserChooser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.create_user);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.user_name);
            this.Controls.Add(this.search);
            this.Controls.Add(this.listBox);
            this.Controls.Add(this.select_user);
            this.Name = "UserChooser";
            this.Text = "UserChooser";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button select_user;
        private System.Windows.Forms.ListBox listBox;
        private System.Windows.Forms.Button search;
        private System.Windows.Forms.TextBox user_name;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button create_user;
        private System.Windows.Forms.Label label2;
    }
}