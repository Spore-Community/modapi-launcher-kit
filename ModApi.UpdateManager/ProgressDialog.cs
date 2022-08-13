using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModApi.UpdateManager
{
    public partial class ProgressDialog : Form
    {
        readonly DoWorkEventHandler _action;

        public ProgressDialog(string text, string title, DoWorkEventHandler action)
        {
            InitializeComponent();

            label.Text = text;
            Text = title;
            _action = action;
        }

        private void ProgressDialog_Load(object sender, EventArgs e)
        {
            var worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += _action;
            worker.ProgressChanged += (_, args) => progressBar.Value = args.ProgressPercentage;
            worker.RunWorkerCompleted += (_, args) => Close();

            worker.RunWorkerAsync();
        }
    }
}
