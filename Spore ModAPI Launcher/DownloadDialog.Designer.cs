namespace SporeModAPI_Launcher
{
    partial class DownloadDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DownloadDialog));
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.lblDownloadingFile = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(12, 37);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(419, 32);
            this.progressBar.Step = 1;
            this.progressBar.TabIndex = 0;
            // 
            // lblDownloadingFile
            // 
            this.lblDownloadingFile.AutoSize = true;
            this.lblDownloadingFile.Location = new System.Drawing.Point(12, 9);
            this.lblDownloadingFile.Name = "lblDownloadingFile";
            this.lblDownloadingFile.Size = new System.Drawing.Size(100, 13);
            this.lblDownloadingFile.TabIndex = 1;
            this.lblDownloadingFile.Text = "Downloading file.... ";
            // 
            // DownloadDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(443, 82);
            this.Controls.Add(this.lblDownloadingFile);
            this.Controls.Add(this.progressBar);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "DownloadDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Shown += new System.EventHandler(this.DownloadDialog_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label lblDownloadingFile;
    }
}