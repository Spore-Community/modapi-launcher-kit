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
            this.labelDetectionError = new System.Windows.Forms.Label();
            this.btnSteam = new System.Windows.Forms.Button();
            this.btnEAApp = new System.Windows.Forms.Button();
            this.btnDisc = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // labelDetectionError
            // 
            this.labelDetectionError.AutoSize = true;
            this.labelDetectionError.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDetectionError.Location = new System.Drawing.Point(12, 9);
            this.labelDetectionError.Name = "labelDetectionError";
            this.labelDetectionError.Size = new System.Drawing.Size(399, 60);
            this.labelDetectionError.TabIndex = 0;
            this.labelDetectionError.Text = "Your game version could not be detected automatically.\r\n\r\nPlease choose your vers" +
    "ion:";
            // 
            // btnSteam
            // 
            this.btnSteam.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnSteam.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSteam.Image = global::SporeModAPI_Launcher.Properties.Resources.VersionButton_Steam_GOG_Icon;
            this.btnSteam.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSteam.Location = new System.Drawing.Point(12, 167);
            this.btnSteam.Name = "btnSteam";
            this.btnSteam.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.btnSteam.Size = new System.Drawing.Size(539, 80);
            this.btnSteam.TabIndex = 3;
            this.btnSteam.Text = "Steam / GOG";
            this.btnSteam.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnSteam.UseVisualStyleBackColor = true;
            this.btnSteam.Click += new System.EventHandler(this.btnSteam_Click);
            // 
            // btnEAApp
            // 
            this.btnEAApp.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnEAApp.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnEAApp.Image = global::SporeModAPI_Launcher.Properties.Resources.VersionButton_EAApp_Icon;
            this.btnEAApp.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnEAApp.Location = new System.Drawing.Point(12, 253);
            this.btnEAApp.Name = "btnEAApp";
            this.btnEAApp.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.btnEAApp.Size = new System.Drawing.Size(539, 80);
            this.btnEAApp.TabIndex = 2;
            this.btnEAApp.Text = "EA App";
            this.btnEAApp.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnEAApp.UseVisualStyleBackColor = true;
            this.btnEAApp.Click += new System.EventHandler(this.btnEAApp_Click);
            // 
            // btnDisc
            // 
            this.btnDisc.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnDisc.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnDisc.Image = global::SporeModAPI_Launcher.Properties.Resources.VersionButton_Disc_Icon;
            this.btnDisc.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnDisc.Location = new System.Drawing.Point(12, 81);
            this.btnDisc.Name = "btnDisc";
            this.btnDisc.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.btnDisc.Size = new System.Drawing.Size(539, 80);
            this.btnDisc.TabIndex = 1;
            this.btnDisc.Text = "Disc";
            this.btnDisc.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnDisc.UseVisualStyleBackColor = true;
            this.btnDisc.Click += new System.EventHandler(this.btnDisc_Click);
            // 
            // GameVersionSelector
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(563, 345);
            this.Controls.Add(this.btnSteam);
            this.Controls.Add(this.btnEAApp);
            this.Controls.Add(this.btnDisc);
            this.Controls.Add(this.labelDetectionError);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "GameVersionSelector";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Select your game version";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelDetectionError;
        private System.Windows.Forms.Button btnDisc;
        private System.Windows.Forms.Button btnEAApp;
        private System.Windows.Forms.Button btnSteam;
    }
}