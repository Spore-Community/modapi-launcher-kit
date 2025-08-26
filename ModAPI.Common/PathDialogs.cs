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
            string path = null;

            if (_galacticAdventuresPath != null)
            {
                return _galacticAdventuresPath;
            }

            try
            {
                if (!LauncherSettings.ForceGamePath)
                {
                    path = SporePath.GetFromRegistry(SporePath.Game.GalacticAdventures);
                }

                if (path != null)
                {
                    // move the path to SporebinEP1
                    path = SporePath.MoveToSporebinEP1(path);
                }

                // If we didn't find the path in the registry or was not valid, ask the user
                if (path == null || !Directory.Exists(path))
                {

                    if (path == null)
                    {
                        path = LauncherSettings.GamePath;
                        if (path == null || path.Length == 0)
                        {
                            var result = MessageBox.Show(CommonStrings.GalacticAdventuresNotFoundSpecifyManual, CommonStrings.GalacticAdventuresNotFound,
                                MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);

                            if (result == DialogResult.OK)
                            {
                                path = ShowGalacticAdventuresChooserDialog();
                            }
                        }
                    }
                    else
                    {
                        // move the path to SporebinEP1
                        path = SporePath.MoveToSporebinEP1(path);
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }

            _galacticAdventuresPath = path;
            return _galacticAdventuresPath;
        }

        // Returns path to Sporebin or null
        private static string _coreSporePath = null;
        public static string ProcessSpore()
        {
            string path = null;

            if (_coreSporePath != null)
            {
                return _coreSporePath;
            }

            try
            {
                if (!LauncherSettings.ForceGamePath)
                {
                    path = SporePath.GetFromRegistry(SporePath.Game.Spore);
                }

                if (path != null)
                {
                    // move the path to Sporebin
                    path = SporePath.MoveToSporebin(path);
                }

                // If we didn't find the path in the registry or was not valid, ask the user
                if (path == null || !Directory.Exists(path))
                {

                    if (path == null)
                    {
                        path = LauncherSettings.GamePath;
                        if (path == null || path.Length == 0)
                        {
                            var result = MessageBox.Show(CommonStrings.SporeNotFoundSpecifyManual, CommonStrings.SporeNotFound,
                                MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);

                            if (result == DialogResult.OK)
                            {
                                path = ShowSporeChooserDialog();
                            }
                        }
                    }
                    else
                    {
                        // move the path to Sporebin
                        path = SporePath.MoveToSporebin(path);
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }

            _coreSporePath = path;
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

        private static string ShowSteamChooserDialog()
        {
            string path = null;
            Thread thread = new Thread(() =>
            {
                var dialog = new FolderBrowserDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    path = dialog.SelectedPath;
                    // move the path to the folder that contains Steam.exe (just in case)
                    path = SporePath.MoveToSteam(path);
                    LauncherSettings.SteamPath = path;
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
