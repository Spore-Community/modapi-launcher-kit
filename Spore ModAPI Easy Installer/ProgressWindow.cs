using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Spore_ModAPI_Easy_Installer
{
    public partial class ProgressWindow : Form
    {
        public ProgressWindow(string title)
        {
            InitializeComponent();

            this.Text = title;

            this.progressBar.Maximum = 100;
            this.progressBar.Minimum = 0;
        }

        public void SetProgress(int value)
        {
            this.progressBar.Value = value;
        }

        public void SetProgressText(string text)
        {
            this.lblCurrentFile.Text = text;
        }

        public void SetDescriptionText(string text)
        {
            this.lblModIsInstalling.Text = text;
        }
    }
}
