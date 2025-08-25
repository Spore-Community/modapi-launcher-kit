using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ModAPI.Common;

namespace SporeModAPI_Launcher
{
    public partial class GameVersionSelector : Form
    {
        public GameVersionType SelectedVersion = GameVersionType.None;

        public GameVersionSelector()
        {
            InitializeComponent();
        }

        private void btnDisc_Click(object sender, EventArgs e)
        {
            SelectedVersion = GameVersionType.Disc;
            this.Close();
        }

        private void btnSteam_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(this, Strings.SteamLastVersion,
                Strings.SteamLastVersionTitle, MessageBoxButtons.YesNo);

            SelectedVersion = result == DialogResult.Yes ? GameVersionType.Steam_Patched : GameVersionType.Steam;

            this.Close();
        }

        private void btnEAApp_Click(object sender, EventArgs e)
        {
            SelectedVersion = GameVersionType.Origin;
            this.Close();
        }
    }
}
