using ModApi.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Panel = System.Windows.Controls.Panel;
using Path = System.IO.Path;
using ModApi.Updater;
using IWshRuntimeLibrary;
using File = System.IO.File;
using System.Diagnostics;
using System.IO.Compression;

namespace ModApi.InterimSetup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool _isUpgradeInstall = false;
        int _currentPage = 0;
        bool _createShortcuts = true;

        CircleEase _ease = new CircleEase()
        {
            EasingMode = EasingMode.EaseOut
        };
        TimeSpan _time = TimeSpan.FromMilliseconds(250);

        string _pathAcceptableText = string.Empty;
        string _pathNotAcceptableText = string.Empty;

        string _pageOneInstructionsUpgradeInstallText = "Please specify the path to your existing Spore ModAPI Launcher.";
        string _pageOneInstructionsNewInstallText = "Please specify where you would like to install the Spore ModAPI Launcher Kit.";

        string _pathSuffix = "Spore ModAPI Launcher Kit";
        string[] _forbiddenPrefixes = new string[]
        {
            Environment.ExpandEnvironmentVariables(@"%userprofile%\Desktop")
        };

        public MainWindow()
        {
            InitializeComponent();
            foreach (Panel p in RootGrid.Children)
            {
                if (RootGrid.Children.IndexOf(p) != _currentPage)
                    p.Visibility = Visibility.Collapsed;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            CyclePage(_currentPage - 1);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            CyclePage(_currentPage + 1);
            BeginInstallation();
        }

        private void UnzipLauncherKit(string path)
        {
            using (MemoryStream unmStream = new MemoryStream(ModApi.InterimSetup.Properties.Resources.ModApiUpdate))
            {
                string unm = Environment.ExpandEnvironmentVariables(@"%appdata%\ModAPITemp.zip");
                if (File.Exists(unm))
                    File.Delete(unm);
                using (var file = File.Create(unm))// ;
                {
                    unmStream.Seek(0, SeekOrigin.Begin);
                    unmStream.CopyTo(file);
                }
                //file.Close()
                //var stream = File.Create(unm);
                using (ZipArchive archive = ZipFile.Open(unm, ZipArchiveMode.Update)) //(ZipStorer archive = ZipStorer.Open(unmStream, FileAccess.Read, true))
                {
                    Dispatcher.BeginInvoke(new Action(() => InstallProgressBar.Maximum = archive.Entries.Count));
                    //MessageBox.Show(InstallProgressBar.Maximum.ToString(), "InstallProgressBar.Maximum");

                    foreach (var s in archive.Entries)
                    {
                        string fileOutPath = Path.Combine(path, s.FullName);
                        //MessageBox.Show(fileOutPath, "fileOutPath");
                        if (File.Exists(fileOutPath))
                            File.Delete(fileOutPath);

                        string extractDir = Directory.GetParent(fileOutPath).ToString();
                        if (!Directory.Exists(extractDir))
                            Directory.CreateDirectory(extractDir);


                        s.ExtractToFile(fileOutPath);// archive.ExtractFile(s, fileOutPath);

                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (InstallProgressBar.Value < InstallProgressBar.Maximum)
                                InstallProgressBar.Value++;
                        }));
                    }
                    string noUpdateFilePath = Path.Combine(path, "noUpdateCheck.info");
                    if (File.Exists(noUpdateFilePath))
                        File.Delete(noUpdateFilePath);
                }
            }
        }

        int _unzipAttemptCount = 0;

        private void BeginInstallation()
        {
            string path = PathTextBox.Text;
            if (File.Exists(path))
                path = Directory.GetParent(path).ToString();

            if (!_isUpgradeInstall)
            {
                if (!path.ToLowerInvariant().EndsWith(_pathSuffix.ToLowerInvariant()))
                    path = Path.Combine(path, _pathSuffix);
            }

            /*await Task.Run(new Action(() =>
            {*/
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                /*while (_unzipAttemptCount < 4)
                {
                    try
                    {*/
                UnzipLauncherKit(path);
                /*}
                catch
                {
                    _unzipAttemptCount++;
                }
            }*/

                string launcherAddress = "Spore ModAPI Launcher.bat";
                string launcherDesc = "Open Spore through the Spore ModAPI Launcher";
                string easyInstallerAddress = "Spore ModAPI Easy Installer.bat";
                string easyInstallerDesc = "Quickly and easily install mods for Spore";
                string easyUninstallerAddress = "Spore ModAPI Easy Uninstaller.bat";
                string easyUninstallerDesc = "Quickly and easily configure and remove mods for Spore";

                //string startPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "Spore ModAPI Launcher Kit");
                string startPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Spore ModAPI Launcher Kit");
                if (!Directory.Exists(startPath))
                    Directory.CreateDirectory(startPath);

                string launcherShortcutPathStart = Path.Combine(startPath, "Spore ModAPI Launcher.lnk");
                if (File.Exists(launcherShortcutPathStart))
                    File.Delete(launcherShortcutPathStart);

                string easyInstallerShortcutPathStart = Path.Combine(startPath, "Spore ModAPI Easy Installer.lnk");
                if (File.Exists(easyInstallerShortcutPathStart))
                    File.Delete(easyInstallerShortcutPathStart);

                string easyUninstallerShortcutPathStart = Path.Combine(startPath, "Spore ModAPI Easy Uninstaller.lnk");
                if (File.Exists(easyUninstallerShortcutPathStart))
                    File.Delete(easyUninstallerShortcutPathStart);

                CreateShortcut(launcherAddress, launcherDesc, Path.Combine(path, "Spore ModAPI Launcher.exe"), true);
                CreateShortcut(easyInstallerAddress, easyInstallerDesc, Path.Combine(path, "Spore ModAPI Easy Installer.exe"), true);
                CreateShortcut(easyUninstallerAddress, easyUninstallerDesc, Path.Combine(path, "Spore ModAPI Easy Uninstaller.exe"), true);

                if (_createShortcuts || (Environment.OSVersion.Version >= new Version(6, 2, 8400, 0)))
                {
                    try
                    {
                        string desktop = Environment.ExpandEnvironmentVariables(@"%userprofile%\Desktop");

                        string launcherShortcutPath = Path.Combine(desktop, "Spore ModAPI Launcher.lnk");
                        if (File.Exists(launcherShortcutPath))
                            File.Delete(launcherShortcutPath);

                        string easyInstallerShortcutPath = Path.Combine(desktop, "Spore ModAPI Easy Installer.lnk");
                        if (File.Exists(easyInstallerShortcutPath))
                            File.Delete(easyInstallerShortcutPath);

                        string easyUninstallerShortcutPath = Path.Combine(desktop, "Spore ModAPI Easy Uninstaller.lnk");
                        if (File.Exists(easyUninstallerShortcutPath))
                            File.Delete(easyUninstallerShortcutPath);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                    CreateShortcut(launcherAddress, launcherDesc, Path.Combine(path, "Spore ModAPI Launcher.exe"));
                    CreateShortcut(easyInstallerAddress, easyInstallerDesc, Path.Combine(path, "Spore ModAPI Easy Installer.exe"));
                    CreateShortcut(easyUninstallerAddress, easyUninstallerDesc, Path.Combine(path, "Spore ModAPI Easy Uninstaller.exe"));

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        CompletionInfoTextBlock.Text = "The Spore ModAPI Launcher kit has been successfully installed. Shortcuts to access it have been created on your Desktop. You can access it from those. Note that you must launch Spore through the Spore ModAPI Launcher from now on.";
                    }));
                }
                else
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        CompletionInfoTextBlock.Text = "The Spore ModAPI Launcher kit has been successfully installed. Note that you must launch Spore through the Spore ModAPI Launcher from now on.";
                    }));
                }

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    CyclePage(3);
                }));
            }).Start();
            //}));
        }

        private void UpgradeInstallButton_Click(object sender, RoutedEventArgs e)
        {
            _isUpgradeInstall = true;
            _pathAcceptableText = "This path is valid. An existing ModAPI Launcher was found.";
            _pathNotAcceptableText = "This path is not valid. No existing ModAPI Launcher was found to upgrade.";
            PageOneInstructionsTextBlock.Text = _pageOneInstructionsUpgradeInstallText;
            PathTextBox.Text = string.Empty;
            CyclePage(1);
        }

        private void NewInstallButton_Click(object sender, RoutedEventArgs e)
        {
            _isUpgradeInstall = false;
            _pathAcceptableText = "This path is valid. The ModAPI Launcher Kit can be installed here.";
            _pathNotAcceptableText = "This path is not valid. The ModAPI Launcher Kit cannot be installed here.";
            PageOneInstructionsTextBlock.Text = _pageOneInstructionsNewInstallText;
            PathTextBox.Text = Environment.ExpandEnvironmentVariables(@"%programdata%\SPORE ModAPI Launcher Kit");
            CyclePage(1);
        }

        public void CyclePage(int targetPage)
        {
            RootGrid.Children[targetPage].Visibility = Visibility.Visible;

            DoubleAnimation GlideInAnimation = new DoubleAnimation()
            {
                Duration = _time,
                EasingFunction = _ease,
                From = 50,
                To = 0
            };
            DoubleAnimation GlideOutAnimation = new DoubleAnimation()
            {
                Duration = _time,
                EasingFunction = _ease,
                From = 0,
                To = -50
            };
            DoubleAnimation FadeInAnimation = new DoubleAnimation()
            {
                Duration = _time,
                EasingFunction = _ease,
                From = 0,
                To = 1
            };
            DoubleAnimation FadeOutAnimation = new DoubleAnimation()
            {
                Duration = _time,
                EasingFunction = _ease,
                From = 1,
                To = 0
            };
            FadeOutAnimation.Completed += (sneder, args) =>
            {
                RootGrid.Children[_currentPage].Visibility = Visibility.Collapsed;
                _currentPage = targetPage;
            };

            RootGrid.Children[_currentPage].RenderTransform = new TranslateTransform();
            RootGrid.Children[targetPage].RenderTransform = new TranslateTransform();

            RootGrid.Children[_currentPage].BeginAnimation(Panel.OpacityProperty, FadeOutAnimation);
            (RootGrid.Children[_currentPage].RenderTransform as TranslateTransform).BeginAnimation(TranslateTransform.XProperty, GlideOutAnimation);
            RootGrid.Children[targetPage].BeginAnimation(Panel.OpacityProperty, FadeInAnimation);
            (RootGrid.Children[targetPage].RenderTransform as TranslateTransform).BeginAnimation(TranslateTransform.XProperty, GlideInAnimation);

            if (targetPage == 1)
                FooterScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation()
                {
                    Duration = _time,
                    EasingFunction = _ease,
                    To = 1
                });
            else
                FooterScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation()
                {
                    Duration = _time,
                    EasingFunction = _ease,
                    To = 0
                });

            if (targetPage == 3)
                SecondFooterScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation()
                {
                    Duration = _time,
                    EasingFunction = _ease,
                    To = 1
                });
            else
                SecondFooterScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation()
                {
                    Duration = _time,
                    EasingFunction = _ease,
                    To = 0
                });
        }

        private void PathBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isUpgradeInstall)
            {
                OpenFileDialog dialog = new OpenFileDialog()
                {
                    Title = "Select the path to your existing Spore ModAPI Launcher.",
                    Filter = "Spore ModAPI Executable (*.exe)|*.exe",
                    FilterIndex = 0,
                    CheckPathExists = true
                };

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string path = dialog.FileName;
                    bool verified = VerifyPath(path);
                    PathTextBox.Text = Environment.ExpandEnvironmentVariables(path);
                }
            }
            else
            {
                FolderBrowserDialog dialog = new FolderBrowserDialog()
                {
                    Description = "Select where you would like to install the Spore ModAPI Launcher Kit.",
                };

                string selectedPath = Environment.ExpandEnvironmentVariables(PathTextBox.Text);
                if (IsValidPath(selectedPath))
                    dialog.SelectedPath = selectedPath;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string path = dialog.SelectedPath;
                    bool verified = VerifyPath(path);
                    string expandedPath = Environment.ExpandEnvironmentVariables(path);
                    if (!expandedPath.ToLowerInvariant().EndsWith(_pathSuffix.ToLowerInvariant()))
                        expandedPath = Path.Combine(expandedPath, _pathSuffix);

                    PathTextBox.Text = expandedPath;
                }
            }
        }

        bool VerifyPath(string path)
        {
            if (_isUpgradeInstall)
            {
                bool returnValue = false;
                string fileName = Path.GetFileName(path).ToLowerInvariant();
                if (File.Exists(path) && fileName.StartsWith("spore modapi") && fileName.EndsWith("exe"))
                    returnValue = true;
                return returnValue;
            }
            else
            {
                if (IsValidPath(PathTextBox.Text) && (PathTextBox.Text.ToLowerInvariant() != Environment.ExpandEnvironmentVariables(@"%userprofile%\Desktop").ToLowerInvariant()))
                    return true;
                else
                    return false;
            }
        }

        //https://stackoverflow.com/questions/3137097/check-if-a-string-is-a-valid-windows-directory-folder-path
        private bool IsValidPath(string path, bool exactPath = true)
        {
            bool isValid = true;

            try
            {
                string fullPath = Path.GetFullPath(path);

                if (exactPath)
                {
                    string root = Path.GetPathRoot(path);
                    isValid = string.IsNullOrEmpty(root.Trim(new char[] { '\\', '/' })) == false;
                }
                else
                {
                    isValid = Path.IsPathRooted(path);
                }
            }
            catch (Exception ex)
            {
                isValid = false;
            }

            foreach (string s in _forbiddenPrefixes)
            {
                if (path.ToLowerInvariant().StartsWith(s.ToLowerInvariant()))
                {
                    isValid = false;
                    break;
                }
            }

            return isValid;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //e.Cancel = true;
        }

        private void PathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (VerifyPath(PathTextBox.Text))
            {
                GoodOrBadPathTextBlock.Text = _pathAcceptableText;
                GoodOrBadPathTextBlock.Foreground = new SolidColorBrush(Colors.ForestGreen);
                NextButton.IsEnabled = true;
            }
            else
            {
                GoodOrBadPathTextBlock.Text = _pathNotAcceptableText;
                GoodOrBadPathTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                NextButton.IsEnabled = false;
            }

            if (!string.IsNullOrWhiteSpace(PathTextBox.Text)) // && (File.Exists(PathTextBox.Text) || Directory.Exists(PathTextBox.Text))
                GoodOrBadPathTextBlock.Visibility = Visibility.Visible;
            else
                GoodOrBadPathTextBlock.Visibility = Visibility.Hidden;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CreateShortcut(string shortcutAddressName, string description, string targetPath)
        {
            CreateShortcut(shortcutAddressName, description, targetPath, false);
        }

        private void CreateShortcut(string shortcutAddressName, string description, string targetPath, bool inStartMenu)
        {
            object shDesktop = (object)"Desktop";
            WshShell shell = new WshShell();
            string shortcutAddress = (string)shell.SpecialFolders.Item(ref shDesktop) + @"\" + shortcutAddressName;

            if (inStartMenu)
                shortcutAddress = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Spore ModAPI Launcher Kit", shortcutAddressName);
            //shortcutAddress = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), @"Programs\Spore ModAPI Launcher Kit", shortcutAddressName);

            /*IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
            shortcut.Description = description;
            shortcut.TargetPath = targetPath;
            shortcut.Save();*/
            if (File.Exists(shortcutAddress))
                File.Delete(shortcutAddress);

            File.WriteAllText(shortcutAddress, "cd \"" + Directory.GetParent(targetPath) + "\"\n\"" + targetPath + "\"");
        }

        private void ShortcutsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _createShortcuts = true;
        }

        private void ShortcutsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _createShortcuts = false;
        }
    }
}