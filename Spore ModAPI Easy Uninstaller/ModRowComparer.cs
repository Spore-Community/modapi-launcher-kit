using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Spore_ModAPI_Easy_Uninstaller
{
    public class ModRowComparer : System.Collections.IComparer
    {
        public int Compare(object x, object y)
        {
            string modName1 = ((DataGridViewRow)x).Cells[2].Value.ToString();
            string modName2 = ((DataGridViewRow)y).Cells[2].Value.ToString();

            if (modName1 == Strings.InstalledMods)
            {
                return -1;
            }
            else if (modName2 == Strings.InstalledMods)
            {
                return 1;
            }

            return String.Compare(modName1, modName2);
        }
    }
}
