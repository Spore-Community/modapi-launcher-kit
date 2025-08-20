using ModAPI_Installers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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
        public static List<string> LauncherKitUpdateUrls = new List<string>
        {
            // Cloudflare R2 + Cache
            "https://update.launcherkit.sporecommunity.com/",
            // GitHub Releases
            "https://github.com/Spore-Community/modapi-launcher-kit/releases/latest/download/",
        };
        public static string PathPrefix = LauncherKitUpdateUrls.First();
        public static string AppDataPath = Environment.ExpandEnvironmentVariables(@"%appdata%\Spore ModAPI Launcher");
        public static string LastUpdateCheckTimePath = Path.Combine(AppDataPath, "lastUpdateCheckTime.info");
        public static string LastUpdateDateTimeFormat = "yyyy-MM-dd HH:mm";
        public static string UpdateInfoDestPath = Path.Combine(AppDataPath, "update.info");
        public static string UpdaterDestPath = Path.Combine(AppDataPath, "updater.exe");
        public static string UpdaterBlockPath = Path.Combine(AppDataPath, "noUpdateCheck.info");
        public static string UpdaterOverridePath = Path.Combine(AppDataPath, "overrideUpdatePath.info");
        public static Version CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version;
        public static Version CurrentDllsBuild
        {
            get
            {
                string path = Path.Combine(Directory.GetParent(Assembly.GetEntryAssembly().Location).ToString(), "coreLibs");
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
        public static string HttpUserAgent = "Spore-ModAPI-Launcher-Kit/" + CurrentVersion.ToString();

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

            if (File.Exists(LastUpdateCheckTimePath))
            {
                try
                {
                    string lastUpdateCheckDateTimeString = File.ReadAllText(LastUpdateCheckTimePath);
                    DateTime lastUpdateCheckDateTime = DateTime.ParseExact(lastUpdateCheckDateTimeString,
                                                                        LastUpdateDateTimeFormat,
                                                                        CultureInfo.InvariantCulture);

                    if ((DateTime.Now - lastUpdateCheckDateTime).TotalHours < 1)
                    {
                        return;
                    }
                }
                catch (Exception)
                {
                    File.Delete(LastUpdateCheckTimePath);
                }
            }

            if (!File.Exists(UpdaterBlockPath))
            {
                try
                {
                    List<Exception> exceptions = new List<Exception>();
                    bool didDownload = false;
                        
                    // Try to download the update info file from the override path first
                    if (File.Exists(UpdaterOverridePath))
                    {
                        PathPrefix = File.ReadAllText(UpdaterOverridePath);

                        // remove override if the URL is in our URL list
                        foreach (string url in LauncherKitUpdateUrls)
                        {
                            if (url == PathPrefix)
                            {
                                File.Delete(UpdaterOverridePath);
                                break;
                            }
                        }

                        try
                        {
                            using (var downloadClient = new DownloadClient(Path.Combine(PathPrefix, "update.info")))
                            {
                                downloadClient.SetTimeout(TimeSpan.FromSeconds(10));
                                downloadClient.DownloadFile(UpdateInfoDestPath);
                            }

                            // Hides exceptions if the download was successful
                            didDownload = true;
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(ex);
                        }
                    }
                    // Try to download the update info file from each URL in the list
                    else
                    {
                        foreach (string url in LauncherKitUpdateUrls)
                        {
                            try
                            {
                                using (var downloadClient = new DownloadClient(Path.Combine(PathPrefix, "update.info")))
                                {
                                    downloadClient.SetTimeout(TimeSpan.FromSeconds(10));
                                    downloadClient.DownloadFile(UpdateInfoDestPath);
                                }

                                // Hides exceptions if the download was successful
                                didDownload = true;
                                PathPrefix = url;
                                break;
                            }
                            catch (Exception ex)
                            {
                                exceptions.Add(ex);
                            }
                        }
                    }

                    // If no download was successful, show all exceptions, one at a time
                    if (!didDownload)
                    {
                        foreach (var ex in exceptions)
                        {
                            ShowUpdateCheckFailedMessage(ex);
                        }

                        // early return when failed
                        return;
                    }

                    if (File.Exists(UpdateInfoDestPath))
                    {
                        var updateInfoLines = File.ReadAllLines(UpdateInfoDestPath);
                        if (Version.TryParse(updateInfoLines[0], out Version ModApiSetupVersion) &&
                            ModApiSetupVersion == new Version(1, 0, 0, 0))
                        {
                            if (Version.Parse(updateInfoLines[1]) > CurrentVersion)
                            {
                                string versionString = "Current version: " + CurrentVersion + "\nNew version: " + updateInfoLines[1];

                                if (MessageBox.Show("An update to the Spore ModAPI Launcher Kit is now available. Would you like to install it now?\n\n" + versionString, "Update Available", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                                {
                                    if (bool.Parse(updateInfoLines[2]))
                                    {
                                        Process.Start(updateInfoLines[3]);
                                    }
                                    else
                                    {

                                        if (File.Exists(UpdaterDestPath))
                                            File.Delete(UpdaterDestPath);

                                        var downloadClient = new DownloadClient(updateInfoLines[3]);
                                        downloadClient.SetTimeout(TimeSpan.FromMinutes(5));
                                        downloadClient.DownloadFile(UpdaterDestPath);

                                        if (File.Exists(UpdaterDestPath))
                                        {
                                            if (new FileInfo(UpdaterDestPath).Length > 0)
                                            {
                                                var args = Environment.GetCommandLineArgs().ToList();

                                                string currentArgs = string.Empty;
                                                foreach (string s in args)
                                                    currentArgs += "\"" + s.TrimEnd('\\') + "\" ";

                                                string argOnePath = Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString().TrimEnd('\\');
                                                if (!argOnePath.EndsWith(" "))
                                                    argOnePath = argOnePath + " ";

                                                Process.Start(UpdaterDestPath, "\"" + argOnePath + "\" " + currentArgs);
                                                Process.GetCurrentProcess().Kill();
                                            }
                                            else
                                            {
                                                File.Delete(UpdaterDestPath);
                                            }
                                        }
                                    }
                                }
                                    
                            }
                        }
                        else
                            ShowUnrecognizedUpdateInfoVersionMessage();
                    }
                    else
                    {
                        File.Delete(UpdateInfoDestPath);
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

                    File.WriteAllText(LastUpdateCheckTimePath, DateTime.Now.ToString(LastUpdateDateTimeFormat));
                }
                catch (Exception ex)
                {
                    ShowUpdateCheckFailedMessage(ex);
                }
            }
        }

        static void ShowUpdateCheckFailedMessage(Exception ex)
        {
            MessageBox.Show("The Launcher Kit could not connect to the update service. Try again in a few minutes, or check https://launcherkit.sporecommunity.com/support for help.\n\nCurrent version: "+ CurrentVersion + "\n\n" + ex.ToString());
        }

        static void ShowUnrecognizedUpdateInfoVersionMessage()
        {
            MessageBox.Show("This update to the Spore ModAPI Launcher Kit must be downloaded manually.");
        }
    }
}