using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;

namespace SporeModAPI_Launcher
{
    public partial class DownloadDialog : Form
    {
        public string DownloadURL;
        public string FileName;
        public string DownloadCompletedText;
        public DownloadDataCompletedEventHandler DownloadCompletedHandler;

        public DownloadDialog(string title)
        {
            InitializeComponent(title);
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs args)
        {
            progressBar.Value = args.ProgressPercentage;

            lblDownloadingFile.Text = Strings.Downloading + " " + FileName + "... (" + (args.BytesReceived / 1000) + " / " + (args.TotalBytesToReceive / 1000) + " KB)";
        }

        private void DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs args)
        {
            lblDownloadingFile.Text = DownloadCompletedText;

            try
            {
                DownloadCompletedHandler(sender, args);
            }
            finally
            {
                this.Close();
            }
        }

        private void DownloadDialog_Shown(object sender, EventArgs e)
        {
            WebClient client = new WebClient();
            client.DownloadProgressChanged += DownloadProgressChanged;
            client.DownloadDataCompleted += DownloadDataCompleted;

            Uri uri = new Uri(DownloadURL);

            client.DownloadDataAsync(uri);

        }
    }
}
