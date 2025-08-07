namespace Spore_ModAPI_Easy_Installer
{
    partial class ProgressWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProgressWindow));
            this.lblModIsInstalling = new System.Windows.Forms.Label();
            this.lblCurrentFile = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // lblModIsInstalling
            // 
            this.lblModIsInstalling.AutoSize = true;
            this.lblModIsInstalling.Location = new System.Drawing.Point(13, 13);
            this.lblModIsInstalling.Name = "lblModIsInstalling";
            this.lblModIsInstalling.Size = new System.Drawing.Size(241, 13);
            this.lblModIsInstalling.TabIndex = 0;
            this.lblModIsInstalling.Text = "The mod is installing. Please wait until it\'s finished.";
            // 
            // lblCurrentFile
            // 
            this.lblCurrentFile.AutoSize = true;
            this.lblCurrentFile.Location = new System.Drawing.Point(16, 30);
            this.lblCurrentFile.Name = "lblCurrentFile";
            this.lblCurrentFile.Size = new System.Drawing.Size(194, 13);
            this.lblCurrentFile.TabIndex = 1;
            this.lblCurrentFile.Text = "Extracting file [INSERT FILE].... (2 / 16)";
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(19, 58);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(576, 66);
            this.progressBar.TabIndex = 2;
            // 
            // ProgressWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(607, 136);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.lblCurrentFile);
            this.Controls.Add(this.lblModIsInstalling);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ProgressWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Installing mod...";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblModIsInstalling;
        private System.Windows.Forms.Label lblCurrentFile;
        private System.Windows.Forms.ProgressBar progressBar;
    }
}