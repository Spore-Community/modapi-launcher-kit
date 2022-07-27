using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace ModApi.Updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string[] args = Environment.GetCommandLineArgs();
        bool launcherExists = true;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (args.Length > 1)
            {
                string path = args[1];
                bool hasLauncher = false;

                //MessageBox.Show(args[1], "args[1]");

                if (File.Exists(path))
                    hasLauncher = true;
                else if (File.Exists(Path.Combine(path, "Spore ModAPI Launcher.exe")))
                    hasLauncher = true;

                if (hasLauncher)
                {
                    //byte[] zipObject = (byte[]);
                    using (MemoryStream unmStream = new MemoryStream(ModApi.Updater.Properties.Resources.ModApiUpdate))
                    {
                        using (ZipStorer archive = ZipStorer.Open(unmStream, FileAccess.Read, true))
                        {
                            InstallProgressBar.Maximum = archive.ReadCentralDir().Count;
                            //MessageBox.Show(InstallProgressBar.Maximum.ToString(), "InstallProgressBar.Maximum");

                            foreach (ZipStorer.ZipFileEntry s in archive.ReadCentralDir())
                            {
                                string fileOutPath = Path.Combine(path, s.FilenameInZip);
                                //MessageBox.Show(fileOutPath, "fileOutPath");
                                if (File.Exists(fileOutPath))
                                    File.Delete(fileOutPath);

                                if (IsPathNotPartOfConfiguration(fileOutPath))
                                    archive.ExtractFile(s, fileOutPath);

                                if (InstallProgressBar.Value < InstallProgressBar.Maximum)
                                    InstallProgressBar.Value++;
                            }
                            string noUpdateFilePath = Path.Combine(path, "noUpdateCheck.info");
                            if (File.Exists(noUpdateFilePath))
                                File.Delete(noUpdateFilePath);

                            StateTextBlock.Text = "Updates were installed successfully.";
                            InstallProgressBar.Visibility = Visibility.Collapsed;
                            AcknowledgeButton.Visibility = Visibility.Visible;
                        }
                    }
                }
                else
                {
                    launcherExists = false;
                    MessageBox.Show("No Spore ModAPI Launcher Kit could be found in the following directory:\n" + Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString());
                    Close();
                }
            }
        }

        private bool IsPathNotPartOfConfiguration(string extractToPath)
        {
            string path = extractToPath.ToLowerInvariant();
            if (path.Contains(@"\modconfigs"))
                return false;
            else if (path.Contains(@"\mlibs"))
                return false;
            else if (path.EndsWith(@"\sporemodapi-disk.dll", StringComparison.OrdinalIgnoreCase))
                return true;
            else if (path.EndsWith(@"\sporemodapi-steam_patched.dll", StringComparison.OrdinalIgnoreCase))
                return true;
            else if (path.EndsWith("-disk.dll", StringComparison.OrdinalIgnoreCase))
                return false;
            else if (path.EndsWith("-steam.dll", StringComparison.OrdinalIgnoreCase))
                return false;
            else if (path.EndsWith("-steam_patched.dll", StringComparison.OrdinalIgnoreCase))
                return false;
            else if (path.EndsWith("installedmods.config", StringComparison.OrdinalIgnoreCase))
                return false;
            else if (path.EndsWith("launchersettings.config", StringComparison.OrdinalIgnoreCase))
                return false;
            else if (path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                return false;
            else
                return true;
        }

        private void AcknowledgeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            PreCloseWindowActions();
        }

        private void PreCloseWindowActions()
        {
            /*for (int i = 0; i < args.Length; i++)
            {
                MessageBox.Show(args[i], "args[" + i.ToString() + "]");
            }*/

            if (launcherExists)
            {
                //MessageBox.Show(Environment.CommandLine, "Environment.CommandLine");
                if (args.Length > 2)
                {
                    /*int index = 0;
                    foreach (string s in args)
                    {
                        MessageBox.Show(s, "args[" + index.ToString() + "]");
                        index++;
                    }*/
                    string pathArg = args[2];
                    string exeArgs = string.Empty;

                    if (args.Length > 3)
                    {
                        for (int i = 3; i < args.Length; i++)
                        {
                            string currentArg = args[i];

                            if (!(currentArg.StartsWith("\"")))
                                currentArg = "\"" + currentArg;

                            if (!(currentArg.EndsWith("\"")))
                                currentArg = currentArg + "\"";

                            exeArgs += currentArg + " ";
                        }
                    }
                    //MessageBox.Show(args[2], "value of args[2]");
                    //MessageBox.Show(exeArgs, "value of exeArgs");
                    if (Permissions.IsAdministrator() && Path.GetFileName(pathArg).ToLowerInvariant().Contains("modapi launcher"))
                    {
                        MessageBox.Show("Please note that in order to drag creation PNGs into the game window after an update to the Spore ModAPI Launcher Kit, you will have to exit Spore and run the ModAPI Launcher yourself.");
                    }
                    Process.Start(pathArg/*Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString(), pathArg)*/, exeArgs);
                }
            }
        }
    }
}