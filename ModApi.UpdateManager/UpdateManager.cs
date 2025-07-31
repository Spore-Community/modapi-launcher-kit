using ModAPI_Installers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Xml;

namespace ModApi.UpdateManager
{
    public static class UpdateManager
    {
        public static bool Development = false;
        public static string PathPrefix = "http://update.launcherkit.sporecommunity.com/";
        public static string AppDataPath = Environment.ExpandEnvironmentVariables(@"%appdata%\Spore ModAPI Launcher");
        public static string UpdateInfoDestPath = Path.Combine(AppDataPath, "update.info");
        public static string CurrentInfoDestPath = Path.Combine(AppDataPath, "current.info");
        public static string UpdaterDestPath = Path.Combine(AppDataPath, "updater.exe");
        public static string UpdaterBlockPath = Path.Combine(AppDataPath, "noUpdateCheck.info");
        public static string UpdaterOverridePath = Path.Combine(AppDataPath, "overrideUpdatePath.info");
        public static string MaintenancePath = Path.Combine(AppDataPath, "maintenance.info");
        public static Version CurrentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;// new Version(1, 1, 0, 0);
        public static Version CurrentDllsBuild
        {
            get
            {
                string path = Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString(), "coreLibs");
                List<Version> versions = new List<Version>();
                if (Directory.Exists(path))
                {
                    foreach (string s in Directory.EnumerateFiles(path).Where(x => x.EndsWith(".dll")))
                    {
                        string ver = FileVersionInfo.GetVersionInfo(s).FileVersion;
                        if (Version.TryParse(ver, out Version sVersion))
                            versions.Add(sVersion);
                    }
                }
                if (versions.Count() > 0)
                {
                    Version minVer = versions.Min();
                    return new Version(minVer.Major, minVer.Minor, minVer.Build, 0);
                }
                else
                    return new Version(999, 999, 999, 999);
            }
        }

        /// <summary>
        /// Core DLLs version is the one that came with the old launcher, 2.5.20
        /// </summary>
        public static Version OldLauncherDllsBuild = new Version(2, 5, 20, 0);

        public static bool HasValidDllsVersion(XmlDocument document)
        {
            var modNode = document.SelectSingleNode("/mod");

            if (modNode != null)
            {
                Version requiredDllsVersion = null;
                if (modNode.Attributes["dllsBuild"] != null)
                    Version.TryParse(modNode.Attributes["dllsBuild"].Value, out requiredDllsVersion);

                if (requiredDllsVersion > OldLauncherDllsBuild)
                {
                    // Only allow these mods unless they have the newer installer version, to ensure they don't get released into old Launcher versions
                    if (modNode.Attributes["installerSystemVersion"] == null ||
                        !Version.TryParse(modNode.Attributes["installerSystemVersion"].Value, out Version installerSystemVersion) ||
                        installerSystemVersion < new Version(1, 0, 1, 2))
                    {
                        // Some mods already existed with newer DLL version but same installerSystemVersion
                        // We make exceptions for them here
                        string unique = modNode.Attributes["unique"] == null ? "" : modNode.Attributes["unique"].Value;
                        if (!((unique == "AssetSharing" || unique == "CaptainVoiceDiversity") &&
                              requiredDllsVersion.Major == 2 && requiredDllsVersion.Minor == 5 && requiredDllsVersion.Build == 179))
                            return false;
                    }
                }

                if (requiredDllsVersion != null &&
                    requiredDllsVersion > CurrentDllsBuild)
                {
                    return false;
                }
            }
            return true;
        }

        public static void CheckForUpdates()
        {
            if ((Process.GetProcessesByName("SporeApp").Length > 0) || (Process.GetProcessesByName("SporeApp_ModAPIFix").Length > 0))
            {
                MessageBox.Show("Please close Spore before attempting to use the Spore ModAPI Launcher Kit again. If you have just closed Spore, wait a moment for the game to fully exit.", string.Empty);
                Process.GetCurrentProcess().Kill();
            }

            if (!Directory.Exists(AppDataPath))
                Directory.CreateDirectory(AppDataPath);

            File.WriteAllText(Path.Combine(AppDataPath, "path.info"), Directory.GetParent(Assembly.GetEntryAssembly().Location).ToString());

            if (File.Exists(UpdaterDestPath))
                File.Delete(UpdaterDestPath);

            if (File.Exists(UpdaterOverridePath))
                PathPrefix = File.ReadAllText(UpdaterOverridePath);

            if (!File.Exists(UpdaterBlockPath))
            {
                bool maintenance = false;

                using (var maintenanceClient = new WebClient())
                {
                    try
                    {
                        maintenanceClient.DownloadFile(Path.Combine(PathPrefix, "maintenance.info"), MaintenancePath);
                        maintenance = true;
                    }
                    catch (Exception)
                    {
                    }
                }

                if (Development)
                    maintenance = false;

                try
                {
                    if (File.Exists(MaintenancePath))
                        File.Delete(MaintenancePath);


                    if (!maintenance)
                    {
                        using (var infoClient = new WebClient())
                        {
                            try
                            {
                                infoClient.DownloadFile(Path.Combine(PathPrefix, "update.info"), UpdateInfoDestPath);
                            }
                            catch (Exception ex)
                            {
                                ShowUpdateCheckFailedMessage(ex);
                            }
                        }



                        if (!File.Exists(CurrentInfoDestPath))
                        {
                            string[] currentInfoLines = new string[]
                            {
                                new Version(1, 0, 0, 0).ToString(),
                                CurrentVersion.ToString(),
                                false.ToString(),
                                Path.Combine(PathPrefix, "ModAPIUpdateSetup.exe")
                            };

                            File.WriteAllLines(CurrentInfoDestPath, currentInfoLines);
                        }

                        if (File.Exists(UpdateInfoDestPath))
                        {
                            if (File.ReadAllText(CurrentInfoDestPath) != File.ReadAllText(UpdateInfoDestPath))
                            {
                                var updateInfoLines = File.ReadAllLines(UpdateInfoDestPath);
                                if (Version.TryParse(updateInfoLines[0], out Version ModApiSetupVersion))
                                {
                                    if (ModApiSetupVersion == new Version(1, 0, 0, 0))
                                    {
                                        if (Version.Parse(updateInfoLines[1]) > CurrentVersion)
                                        {
                                            if (MessageBox.Show("An update to the Spore ModAPI Launcher Kit is now available. Would you like to install it now?", "Update Available", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                                            {
                                                if (bool.Parse(updateInfoLines[2]))
                                                {
                                                    Process.Start(updateInfoLines[3]);
                                                }
                                                else
                                                {
                                                    var installerClient = new WebClient();

                                                    if (File.Exists(UpdaterDestPath))
                                                        File.Delete(UpdaterDestPath);

                                                    installerClient.DownloadFile(/*new Uri(*/updateInfoLines[3]/*)*/, UpdaterDestPath);
                                                    /*installerClient.DownloadFileCompleted += (sneder, args) =>
                                                    {*/
                                                    if (File.Exists(UpdaterDestPath))
                                                    {
                                                        if (new FileInfo(UpdaterDestPath).Length > 0)
                                                        {
                                                            var args = Environment.GetCommandLineArgs().ToList();
                                                            //args.RemoveAt(0);
                                                            string currentArgs = string.Empty;
                                                            foreach (string s in args)
                                                                currentArgs += "\"" + s.TrimEnd('\\') + "\" "; //currentArgs = currentArgs + "\"" + s + "\" ";
                                                                                                               /*foreach (string s in args)
                                                                                                               {
                                                                                                                   MessageBox.Show(s, "args[" + args.IndexOf(s) + "]");
                                                                                                                   currentArgs = currentArgs + "\"" + s/*ConvertToArgument(s)* / + "\" ";
                                                                                                               }*/

                                                            string argOnePath = Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString().TrimEnd('\\');
                                                            if (!argOnePath.EndsWith(" "))
                                                                argOnePath = argOnePath + " ";
                                                            //MessageBox.Show(argOnePath, "argOnePath");

                                                            Process.Start(UpdaterDestPath, "\"" + argOnePath + "\" " + /*"\" \"" + Environment.GetCommandLineArgs()[0] + "\" " + */currentArgs);
                                                            Process.GetCurrentProcess().Kill();
                                                        }
                                                        else
                                                        {
                                                            File.Delete(UpdaterDestPath);
                                                        }
                                                    }
                                                    //};
                                                }
                                            }
                                        }
                                        /*}
                                        catch (Exception ex)
                                        {
                                            MessageBox.Show(ex.ToString(), "Something broke");
                                        }*/
                                    }
                                    else
                                        ShowUnrecognizedUpdateInfoVersionMessage();
                                }
                                else
                                    ShowUnrecognizedUpdateInfoVersionMessage();
                            }
                            else
                            {
                                File.Delete(UpdateInfoDestPath);
                            }
                        }
                        else
                        {

                        }

                        if (DllsUpdater.HasDllsUpdate(out var githubRelease))
                        {
                            var result = MessageBox.Show(CommonStrings.DllsUpdateAvailable, CommonStrings.DllsUpdateAvailableTitle, MessageBoxButton.YesNo);
                            if (result == MessageBoxResult.Yes)
                            {
                                var dialog = new ProgressDialog(
                                    CommonStrings.UpdatingDllsDialog + githubRelease.tag_name,
                                    CommonStrings.UpdatingDllsDialogTitle,
                                    (s, e) =>
                                    {
                                        DllsUpdater.UpdateDlls(githubRelease, progress =>
                                        {
                                            (s as BackgroundWorker).ReportProgress(progress);
                                        });
                                    });
                                dialog.ShowDialog();
                            }
                        }
                    }
                    else if (maintenance && !Development)
                    {
                        MessageBox.Show("The ModAPI Launcher Kit's automatic update functionality is currently down for maintenance. Offline usage may continue as normal.");
                    }

                    /*if (File.Exists(UpdaterDestPath))
                    {
                        if (new FileInfo(UpdaterDestPath).Length == 0)
                            File.Delete(UpdaterDestPath);
                    }*/
                }
                catch (Exception ex)
                {
                    ShowUpdateCheckFailedMessage(ex);
                }
            }
        }

        static string ConvertToArgument(string path)
        {
            if (path == null)
            {
                return "null";
            }
            else
            {
                return "\"" + path + "\"";
            }
        }

        static void ShowUpdateCheckFailedMessage(Exception ex)
        {
            MessageBox.Show("Update Check Failed. This may be due to the update service being down, or due to lack of a working internet connection.\n\n" + ex.ToString());
        }

        static void ShowUnrecognizedUpdateInfoVersionMessage()
        {
            MessageBox.Show("This update to the Spore ModAPI Launcher Kit must be downloaded manually. Closing this dialog box will open a website from which you may do so.");
            try
            {
                Process.Start("https://sporemodder.wordpress.com/spore-modapi/");
            }
            catch (Exception)
            {
            }
        }
    }
}