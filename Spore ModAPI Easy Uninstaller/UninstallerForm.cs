using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ModAPI_Installers;

namespace Spore_ModAPI_Easy_Uninstaller
{
    public partial class UninstallerForm : Form
    {
        private bool eventBeingHandled = false;

        public UninstallerForm()
        {
            InitializeComponent();

            // create header row

            this.dataGridView1.Rows.Add(new object[] { false, "Installed Mods"});
            this.dataGridView1.Rows[0].DefaultCellStyle.ApplyStyle(dataGridView1.ColumnHeadersDefaultCellStyle);
            this.dataGridView1.Rows[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            this.dataGridView1.Rows[0].Cells[2].Value = Strings.InstalledMods;
            this.dataGridView1.Columns[1].Visible = false;
            

            this.dataGridView1.CellDoubleClick += dataGridView_CellDoubleClick;
            this.dataGridView1.CellContentClick += dataGridView_CellContentClick;
            this.dataGridView1.CellValueChanged += dataGridView_CellValueChanged;
            this.dataGridView1.CellPainting += dataGridView_CellPainting;
            this.label1.Text = Strings.ChooseTheMods;
            this.btnCancel.Text = Strings.Cancel;
            this.btnUninstall.Text = Strings.UninstallSelected + " (0)";
            this.btnUninstall.Enabled = false;
            //this.btnUninstall.Text = "asd";
            this.label2.Text = "Spore ModAPI Launcher Kit Version " + ModApi.UpdateManager.UpdateManager.CurrentVersion.ToString() + "\nDLLs Build " + ModApi.UpdateManager.UpdateManager.CurrentDllsBuild;
            this.BringToFront();
        }

        public void AddMod(ModConfiguration mod)
        {
            int index = this.dataGridView1.Rows.Add(new object[] { false, mod, mod.DisplayName });

            if (GetConfiguratorPath(index) != null)
                this.dataGridView1.Rows[index].Cells[0].ReadOnly = true;
            //this.dataGridView1.Rows[index].Cells[2].Value = mod.DisplayName;
            
            //this.dataGridView1.Rows[index].Cells[0].Tag = mod;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void dataGridView_CellDoubleClick(Object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex != 0 && GetConfiguratorPath(e.RowIndex) != null)
            {
                // execute configurator and close uninstaller
                ExecuteConfigurator(GetModConfiguration(e.RowIndex));
            }
            else
            {
                this.dataGridView1.Rows[e.RowIndex].Cells[0].Value = !(bool)this.dataGridView1.Rows[e.RowIndex].Cells[0].Value;
            }
        }

        private void dataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex != 0 && GetConfiguratorPath(e.RowIndex) != null && e.ColumnIndex == 0)
            {
                // execute configurator and close uninstaller
                ExecuteConfigurator(GetModConfiguration(e.RowIndex));
            }
            else
            {
                this.dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);

            }
        }

        private void dataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (!eventBeingHandled)
            {
                eventBeingHandled = true;

                if (e.RowIndex == 0)
                {
                    bool value = (bool)this.dataGridView1.Rows[0].Cells[0].Value;
                    for (int i = 1; i < this.dataGridView1.RowCount; i++)
                    {
                        if (GetConfiguratorPath(i) == null)
                        {
                            this.dataGridView1.Rows[i].Cells[0].Value = value;
                        }
                    }
                }
                else
                {
                    // unselect header row
                    this.dataGridView1.Rows[0].Cells[0].Value = false;
                }

                int selectedCount = 0;
                for (int i = 1; i < this.dataGridView1.RowCount; i++)
                {
                    if ((bool) this.dataGridView1.Rows[i].Cells[0].Value)
                    {
                        selectedCount++;
                    }
                }

                this.btnUninstall.Text = Strings.UninstallSelected + " (" + selectedCount + ")";
                this.btnUninstall.Enabled = selectedCount > 0;

                eventBeingHandled = false;
            }
        }

        private void btnUninstall_Click(object sender, EventArgs e)
        {
            var list = new List<ModConfiguration>();

            foreach (DataGridViewRow row in this.dataGridView1.Rows)
            {
                if (((bool)row.Cells[0].Value) && (row.Cells[1].Value is ModConfiguration conf))
                    list.Add(conf);
                /*else
                    MessageBox.Show("COULD NOT READ MOD FROM DATAGRID CELL THING");*/
            }//list.Add(new ModConfiguration(row.Cells[1].Value.ToString()));

            if (list.Count > 0)
            {
                var result = MessageBox.Show(this, Strings.AreYouSure, Strings.ConfirmationNeeded, MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    EasyUninstaller.UninstallMods(list);

                    this.Close();

                }
            }

        }

        private void dataGridView_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            // if (e.ColumnIndex == this.dataGridView1.Columns["ConfiguratorColumn"].Index)
            if (e.ColumnIndex == 0 && e.RowIndex > 0)
            {
                if (GetConfiguratorPath(e.RowIndex) != null)
                {
                    e.Paint(e.CellBounds, DataGridViewPaintParts.All);

                    var w = Properties.Resources.ConfigIcon.Width;
                    var h = Properties.Resources.ConfigIcon.Height;
                    var x = e.CellBounds.Left + (e.CellBounds.Width - w) / 2;
                    var y = e.CellBounds.Top + (e.CellBounds.Height - h) / 2;

                    e.Graphics.DrawImage(Properties.Resources.ConfigIcon, new Rectangle(x, y, w, h));
                    e.Handled = true;

                }
            }
        }

        private ModConfiguration GetModConfiguration(int rowIndex)
        {
            return (ModConfiguration)this.dataGridView1.Rows[rowIndex].Cells[1].Value;
        }

        private string GetConfiguratorPath(int rowIndex)
        {
            return ((ModConfiguration)this.dataGridView1.Rows[rowIndex].Cells[1].Value).ConfiguratorPath;
        }

        private void ExecuteConfigurator(ModConfiguration mod)
        {
            try
            {
                EasyUninstaller.ExecuteConfigurator(mod);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, CommonStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
