namespace SporeModAPI_Launcher
{
    partial class GameVersionSelector
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GameVersionSelector));
            this.label1 = new System.Windows.Forms.Label();
            this.btnSteam = new System.Windows.Forms.Button();
            this.btnOrigin = new System.Windows.Forms.Button();
            this.btnDisk = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(503, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = Strings.ChooseGameVersion;
            // 
            // btnSteam
            // 
            this.btnSteam.BackgroundImage = global::SporeModAPI_Launcher.Properties.Resources.VersionButton_Steam_GoG1;
            this.btnSteam.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnSteam.Location = new System.Drawing.Point(13, 133);
            this.btnSteam.Name = "btnSteam";
            this.btnSteam.Size = new System.Drawing.Size(538, 80);
            this.btnSteam.TabIndex = 3;
            this.btnSteam.UseVisualStyleBackColor = true;
            this.btnSteam.Click += new System.EventHandler(this.btnSteam_Click);
            // 
            // btnOrigin
            // 
            this.btnOrigin.BackgroundImage = global::SporeModAPI_Launcher.Properties.Resources.VersionButton_Origin1;
            this.btnOrigin.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnOrigin.Location = new System.Drawing.Point(13, 219);
            this.btnOrigin.Name = "btnOrigin";
            this.btnOrigin.Size = new System.Drawing.Size(538, 80);
            this.btnOrigin.TabIndex = 2;
            this.btnOrigin.UseVisualStyleBackColor = true;
            this.btnOrigin.Click += new System.EventHandler(this.btnOrigin_Click);
            // 
            // btnDisk
            // 
            this.btnDisk.BackgroundImage = global::SporeModAPI_Launcher.Properties.Resources.VersionButton_Disk3;
            this.btnDisk.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnDisk.Location = new System.Drawing.Point(13, 47);
            this.btnDisk.Name = "btnDisk";
            this.btnDisk.Size = new System.Drawing.Size(538, 80);
            this.btnDisk.TabIndex = 1;
            this.btnDisk.UseVisualStyleBackColor = true;
            this.btnDisk.Click += new System.EventHandler(this.btnDisk_Click);
            // 
            // GameVersionSelector
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(563, 313);
            this.Controls.Add(this.btnSteam);
            this.Controls.Add(this.btnOrigin);
            this.Controls.Add(this.btnDisk);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "GameVersionSelector";
            this.Text = Strings.ChooseGameVersionTitle;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnDisk;
        private System.Windows.Forms.Button btnOrigin;
        private System.Windows.Forms.Button btnSteam;
    }
}