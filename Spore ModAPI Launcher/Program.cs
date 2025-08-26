using EnvDTE80;
using ModAPI.Common;
using ModAPI.Common.Dialog;
using ModAPI.Common.Update;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace SporeModAPI_Launcher
{

    class Program
    {
        public static IntPtr processHandle = IntPtr.Zero;

        private const string ModAPIFixDownloadURL = "https://davoonline.com/sporemodder/emd4600/SporeApp_ModAPIFix.zip";
        private const string ModApiHelpThreadURL = "https://launcherkit.sporecommunity.com/support";

        private string SporebinPath;
        private string ExecutablePath;
        private GameVersionType _executableType;

        // Used for executing Spore and injecting DLLs
        private NativeTypes.STARTUPINFO StartupInfo;
        private NativeTypes.PROCESS_INFORMATION ProcessInfo;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            bool proceed = true;
            if (Permissions.IsAdministrator())
            {
                proceed = false;
                if (MessageBox.Show("For security reasons, explicitly running the Spore ModAPI Launcher as Administrator (by right-clicking and selecting \"Run as Administrator\") is not recommended. Doing so will also prevent you from being able to load creations into Spore by dragging their PNGs into the game window. Are you sure you want to proceed?", String.Empty, MessageBoxButtons.YesNo) == DialogResult.Yes)
                    proceed = true;
            }

            if (proceed)
            {
                UpdateManager.CheckForUpdates();
                Application.EnableVisualStyles();
                LauncherSettings.Load();

                // ensure we find Spore & GA as early as possible
                if (PathDialogs.ProcessSpore() == null ||
                    PathDialogs.ProcessGalacticAdventures() == null)
                {
                    return;
                }

                new Program().Execute();
            }
            else
            {
                Application.Exit();
            }
        }

        void Execute()
        {
            try
            {
                // We try a new approach for Steam users.
                // Before, we used Steam to launch the game and tried to find the new process and inject it.
                // However, when the injection happens the game already executed a bit, so mods fail.
                // Instead, we create a steam_appid.txt that allows us to execute SporeApp.exe directly                
                SporebinPath = PathDialogs.ProcessGalacticAdventures();

                // use the default path for now (we might have to use a different one for Origin)
                this.ExecutablePath = this.SporebinPath + "SporeApp.exe";
                this.ProcessExecutableType();

                if (this._executableType == GameVersionType.None)
                {
                    // don't execute the game if the user closed the dialog
                    return;
                }

                // get the correct executable path
                this.ExecutablePath = this.SporebinPath + GameVersion.ExecutableNames[(int)this._executableType];
                if (!File.Exists(this.ExecutablePath))
                {
                    // the file might only not exist in Origin (since Origin users will use a different executable compatible with ModAPI)
                    if (GameVersion.RequiresModAPIFix(this._executableType))
                    {
                        if (!HandleOriginUsers())
                        {
                            return;
                        }
                    }
                    else
                    {
                        throw new Exception(CommonStrings.GalacticAdventuresNotFound);
                    }
                }
                

                // we must also check if the steam_api.dll doesn't exist (it's required for Origin users)
                if (GameVersion.RequiresModAPIFix(this._executableType) && !File.Exists(this.SporebinPath + "steam_api.dll"))
                {
                    if (!HandleOriginUsers())
                    {
                        return;
                    }
                }

                if (SporePath.SporeIsInstalledOnSteam())
                {
                    string steamAppIdPath = Path.Combine(this.SporebinPath, "steam_appid.txt");
                    //we have to use Spore GAs appid now, due to steam DRM :(
                    if (!File.Exists(steamAppIdPath) || File.ReadAllText(steamAppIdPath) != "24720")
                    {
                        try
                        {
                            File.WriteAllText(steamAppIdPath, "24720");
                        }
                        catch
                        {
                            MessageBox.Show(Strings.CannotApplySteamFix.Replace("$PATH$", this.SporebinPath), Strings.CannotApplySteamFixTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }

                string dllEnding = GameVersion.VersionNames[(int)this._executableType];
                if (dllEnding == null)
                {
                    MessageBox.Show(Strings.VersionNotDetected, CommonStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                InjectSporeProcess(dllEnding);

                int lastError = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                
                if (lastError != 0 && lastError != 18)
                    ThrowWin32Exception("Something went wrong", lastError);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }
        
        public void ShowError(Exception ex)
        {
            string versionInfo = "Launcher Kit version: " + UpdateManager.CurrentVersion + "\nModAPI DLLs version: " + UpdateManager.CurrentDllsBuild + "\nLauncher Kit path: " + Assembly.GetEntryAssembly().Location;
            if(this.ExecutablePath != null && File.Exists(this.ExecutablePath))
            {
                versionInfo += "\n\nSpore version: " + FileVersionInfo.GetVersionInfo(this.ExecutablePath).FileVersion + " - " + this._executableType + "\nSpore path: " + this.ExecutablePath;
            }
            MessageBox.Show(Strings.GalacticAdventuresNotExecuted + "\n" + ModApiHelpThreadURL + "\n\n" + ex.GetType() + "\n\n" + ex.Message + "\n\n" + ex.StackTrace + "\n\n" + versionInfo, CommonStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            if (ex is System.ComponentModel.Win32Exception)
            {
                var exc = ex as System.ComponentModel.Win32Exception;
                MessageBox.Show("ErrorCode: " + exc.ErrorCode + "\n" +
                    "NativeErrorCode: " + exc.NativeErrorCode + "\n" +
                    "HResult: " + exc.HResult + "\n", "Additional Win32Exception Error Info");

                if (exc.InnerException != null)
                {
                    MessageBox.Show("ErrorCode: " + exc.InnerException.GetType() + "\n\n" + exc.InnerException.Message + "\n\n" + exc.InnerException.StackTrace, "Win32Exception InnerException Error Info");
                }
            }
        }

        List<string> GetDLLsToInject(string dllEnding)
        {
            List<string> dlls = new List<string>();

            //coreLibs and mLibs
            var baseFolder = new FileInfo(Assembly.GetEntryAssembly().Location).Directory;
            var coreFolder = Path.Combine(baseFolder.FullName, "coreLibs");
            var mFolder    = Path.Combine(baseFolder.FullName, "mLibs");
            if (!Directory.Exists(mFolder))
                Directory.CreateDirectory(mFolder);

            string libName = "SporeModAPI.lib";
            string MODAPI_DLL = "SporeModAPI-" + dllEnding + ".dll";
            string coreDllName = GameVersion.GetNewDLLName(_executableType);
            string coreDllOutPath = Path.Combine(mFolder, "SporeModAPI.dll");

            File.Copy(Path.Combine(coreFolder, libName), Path.Combine(mFolder, libName), true);
            File.Copy(Path.Combine(coreFolder, coreDllName), coreDllOutPath, true);

            dlls.Add(coreDllOutPath);
            dlls.Add(Path.GetFullPath(MODAPI_DLL));

            foreach (string s in Directory.EnumerateFiles(mFolder)
                .Where(x => !Path.GetFileName(x).Equals(coreDllName, StringComparison.OrdinalIgnoreCase) && 
                    x.ToLowerInvariant().EndsWith(".dll") &&
                    !dlls.Contains(x)))
            {
                dlls.Add(s);
            }

            foreach (var file in baseFolder.EnumerateFiles("*" + dllEnding + ".dll")
                .Where(x => x.Name != MODAPI_DLL))
            {
                // the ModAPI dll should already be loaded
                if (file.Name != MODAPI_DLL)
                {
                    dlls.Add(file.FullName);
                }
            }


            return dlls;
        }

        void InjectSporeProcess(string dllEnding)
        {
            CreateSporeProcess();

            try
            {
                const string MOD_API_DLL_INJECTOR = "ModAPI.DLLInjector.dll";
                IntPtr hDLLInjectorHandle = Injector.InjectDLL(this.ProcessInfo, Path.GetFullPath(MOD_API_DLL_INJECTOR));

                List<string> dlls = GetDLLsToInject(dllEnding);

                Injector.SetInjectionData(this.ProcessInfo, hDLLInjectorHandle, dllEnding == "disk", dlls);

                if (NativeMethods.IsDebuggerPresent())
                {
                    DTE2 dte = DebugHelper.GetActiveDebugger();
                    if (dte != null)
                    {
                        foreach (var proc in dte.Debugger.LocalProcesses.Cast<EnvDTE.Process>().Where(proc => proc.ProcessID == ProcessInfo.dwProcessId))
                        {
                            ResumeSporeProcess();
                            proc.Attach();
                            return;
                        }
                    }
                }

                ResumeSporeProcess();
            }
            catch (Exception e)
            {
                // always terminate suspended Spore process on failure
                NativeMethods.TerminateProcess(this.ProcessInfo.hProcess, 0);
                throw e;
            }
        }

        void CreateSporeProcess()
        {
            var sb = new StringBuilder();
            int i = 0;
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                // the first argument is the path
                if (i != 0)
                {
                    sb.Append(arg);
                    sb.Append(" ");
                }
                i++;
            }

            if (!NativeMethods.CreateProcess(null, "\"" + this.ExecutablePath + "\" " + sb,
                    IntPtr.Zero, IntPtr.Zero, false, NativeTypes.ProcessCreationFlags.CREATE_SUSPENDED, IntPtr.Zero, this.SporebinPath, ref this.StartupInfo, out this.ProcessInfo))
            {
                //throw new InjectException(Strings.ProcessNotStarted);
                int lastError = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                System.Windows.Forms.MessageBox.Show("Error: " + lastError, Strings.ProcessNotStarted);
                throw new System.ComponentModel.Win32Exception(lastError);
            }
        }

        void ResumeSporeProcess()
        {
            if (NativeMethods.ResumeThread(this.ProcessInfo.hThread) != 1)
            {
                /*throw new InjectException(Strings.ProcessNotResumed);*/
                ThrowWin32Exception(Strings.ProcessNotResumed);
            }
        }

        string ProcessSporebinPath()
        {
            string path = PathDialogs.ProcessGalacticAdventures();

            if (path == null || !Directory.Exists(path))
            {

                throw new InjectException(CommonStrings.GalacticAdventuresNotFound);
            }

            this.SporebinPath = path;

            return path;
        }

        GameVersionType ProcessExecutableType()
        {
            GameVersionType executableType = GameVersion.DetectVersion(this.ExecutablePath);

            // for debugging purposes
            //executableType = GameVersionType.None;

            if (executableType == GameVersionType.None)
            {
                if (LauncherSettings.GameVersion != GameVersionType.None)
                {
                    executableType = LauncherSettings.GameVersion;
                }
                else
                {
                    executableType = ShowVersionSelectorDialog();

                    // The detection should work fine unless you have the wrong version, so tell the user                        
                    MessageBox.Show(Strings.MightNotWork, Strings.MightNotWorkTitle,
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                }
            }

            this._executableType = executableType;

            return executableType;
        }

        bool HandleOriginUsers()
        {
            if (MessageBox.Show(Strings.DownloadOriginFix, Strings.FileNeeded, MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                return ShowDownloadFixDialog(this.SporebinPath);
            }
            else
            {
                // don't execute the game
                return false;
            }
        }

        // -- DIALOGS -- //


        static GameVersionType ShowVersionSelectorDialog()
        {
            GameVersionType gameVersion = GameVersionType.None;
            Thread thread = new Thread(() =>
            {
                var dialog = new GameVersionSelector();
                dialog.ShowDialog();

                gameVersion = dialog.SelectedVersion;
                LauncherSettings.GameVersion = gameVersion;
                LauncherSettings.Save();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            return gameVersion;
        }

        static bool ShowDownloadFixDialog(string outputPath)
        {
            bool result = false;

            Thread thread = new Thread(() =>
            {
                var dialog = new ProgressDialog(Strings.DownloadFixTitle, Strings.DownloadFixTitle, (s, e) =>
                {
                    try
                    {
                        MemoryStream memoryStream;

                        using (var downloadClient = new DownloadClient(ModAPIFixDownloadURL))
                        {
                            downloadClient.DownloadProgressChanged += (_, progress) =>
                            {
                                (s as BackgroundWorker).ReportProgress(progress);
                            };

                            downloadClient.SetTimeout(TimeSpan.FromMinutes(5));
                            memoryStream = downloadClient.DownloadToMemory();
                        }

                        result = ExtractFixFiles(memoryStream, outputPath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString(), "Failed to download or extract ModAPI Fix");
                        result = false;
                    }
                });

                dialog.ShowDialog();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            return result;
        }

        // -- UTILITY METHODS -- //

        static bool ExtractFixFiles(MemoryStream zipStream, string outputPath)
        {
            try
            {
                using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
                {
                    foreach (var entry in archive.Entries)
                    {
                        entry.ExtractToFile(outputPath + entry.Name, true);
                    }
                }
                return true;
            }
            catch (System.UnauthorizedAccessException)
            {
                MessageBox.Show(CommonStrings.UnauthorizedAccess, CommonStrings.UnauthorizedAccessTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public static void ThrowWin32Exception(string info)
        {
            ThrowWin32Exception(info, System.Runtime.InteropServices.Marshal.GetLastWin32Error());
        }

        public static void ThrowWin32Exception(string info, int error)
        {
            if ((error != 0) && (error != 18))
            {
                System.Windows.Forms.MessageBox.Show("Error: " + error, info);
                throw new System.ComponentModel.Win32Exception(error);
            }
        }

        private static string GetModDisplayName(XmlNode modNode)
        {
            return modNode.Attributes["displayName"]?.Value ?? modNode.Attributes["unique"]?.Value;
        }

        private static List<string> GetModFiles(XmlNode componentNode)
        {
            var modFiles = new List<string>();
            foreach (XmlNode child in componentNode.ChildNodes)
            {
                switch (child.Name.ToLower())
                {
                    case "component":
                    case "prerequisite":
                    case "compatfile":
                        modFiles.AddRange(child.InnerText.Split('?'));
                        break;
                    case "componentgroup":
                        modFiles.AddRange(GetModFiles(child));
                        break;
                }
            }
            return modFiles;
        }
    }
}
