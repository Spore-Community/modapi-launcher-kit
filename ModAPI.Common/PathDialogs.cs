using System.Threading;
using System.IO;
using System.Windows.Forms;
using System;

namespace ModAPI.Common
{
    public static class PathDialogs
    {
        // Returns path to SporebinEP1 or null
        private static string _galacticAdventuresPath = null;
        public static string ProcessGalacticAdventures()
        {
            if (_galacticAdventuresPath != null)
            {
                return _galacticAdventuresPath;
            }

            try
            {
                // attempt to retrieve spore path from registry
                string path = SporePath.GetFromRegistry(SporePath.Game.GalacticAdventures);
                if (!String.IsNullOrEmpty(path) && Directory.Exists(path))
                {
                    path = SporePath.MoveToSporebinEP1(path);
                }

                // fallback to specified game path
                if (String.IsNullOrEmpty(path) || !Directory.Exists(path))
                {
                    path = LauncherSettings.GamePath;
                    if (!String.IsNullOrEmpty(path) && Directory.Exists(path))
                    {
                        path = SporePath.MoveToSporebinEP1(path);
                    }
                }

                // ask the user when fallback wasn't found
                if (String.IsNullOrEmpty(path) || !Directory.Exists(path))
                {
                    var result = MessageBox.Show(CommonStrings.GalacticAdventuresNotFoundSpecifyManual, CommonStrings.GalacticAdventuresNotFound,
                        MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);

                    if (result == DialogResult.OK)
                    {
                        path = ShowGalacticAdventuresChooserDialog();
                    }
                }

                _galacticAdventuresPath = path;
            }
            catch (Exception)
            {
                return null;
            }

            return _galacticAdventuresPath;
        }

        // Returns path to Sporebin or null
        private static string _coreSporePath = null;
        public static string ProcessSpore()
        {
            if (_coreSporePath != null)
            {
                return _coreSporePath;
            }

            try
            {
                // attempt to retrieve spore path from registry
                string path = SporePath.GetFromRegistry(SporePath.Game.Spore);
                if (!String.IsNullOrEmpty(path) && Directory.Exists(path))
                {
                    path = SporePath.MoveToSporebin(path);
                }

                // fallback to specified game path
                if (String.IsNullOrEmpty(path) || !Directory.Exists(path))
                {
                    path = LauncherSettings.SporeGamePath;
                    if (!String.IsNullOrEmpty(path) && Directory.Exists(path))
                    {
                        path = SporePath.MoveToSporebin(path);
                    }
                }

                // ask the user when fallback wasn't found
                if (String.IsNullOrEmpty(path) || !Directory.Exists(path))
                {
                    var result = MessageBox.Show(CommonStrings.SporeNotFoundSpecifyManual, CommonStrings.SporeNotFound,
                        MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);

                    if (result == DialogResult.OK)
                    {
                        path = ShowSporeChooserDialog();
                    }
                }

                _coreSporePath = path;
            }
            catch (Exception)
            {
                return null;
            }

            return _coreSporePath;
        }

        // -- DIALOGS -- //
        private static string ShowGalacticAdventuresChooserDialog()
        {
            string path = null;
            Thread thread = new Thread(() =>
            {
                var dialog = new FolderBrowserDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    path = dialog.SelectedPath;
                    // move the path to SporebinEP1
                    path = SporePath.MoveToSporebinEP1(path);
                    LauncherSettings.GamePath = path;
                    LauncherSettings.Save();
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            return path;
        }

        private static string ShowSporeChooserDialog()
        {
            string path = null;
            Thread thread = new Thread(() =>
            {
                var dialog = new FolderBrowserDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    path = dialog.SelectedPath;
                    // move the path to SporebinEP1
                    path = SporePath.MoveToSporebin(path);
                    LauncherSettings.SporeGamePath = path;
                    LauncherSettings.Save();
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            return path;
        }
    }

}
