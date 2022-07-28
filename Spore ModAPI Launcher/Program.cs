using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;
using System.Threading;

using System.IO;
using System.IO.Compression;

using ModAPI_Installers;
using ModApi.UpdateManager;
using System.Xml;
using System.Reflection;

namespace SporeModAPI_Launcher
{

    class Program
    {
        public static IntPtr processHandle = IntPtr.Zero;
        private static bool ERROR_TESTING = File.Exists(Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString(), "debug.txt"));

        static string ModAPIFixDownloadURL = "http://davoonline.com/sporemodder/emd4600/SporeApp_ModAPIFix.zip";
        static string ModApiHelpThreadURL = "http://davoonline.com/phpBB3/viewtopic.php?f=108&t=6300";
        static string DarkInjectionPageURL = "http://davoonline.com/sporemodder/rob55rod/DarkInjection/";

        private string SporebinPath;
        private string ExecutablePath;
        private string SteamPath;
        private GameVersionType _executableType;

        // Used for executing Spore and injecting DLLs
        private STARTUPINFO StartupInfo = new STARTUPINFO();
        private PROCESS_INFORMATION ProcessInfo = new PROCESS_INFORMATION();

        public static int CurrentError = 0;


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
            if (proceed) {
            ModApi.UpdateManager.UpdateManager.CheckForUpdates();
            System.Windows.Forms.Application.EnableVisualStyles();
            /*string infoFilePath = Path.Combine(ModApi.UpdateManager.UpdateManager.AppDataPath, "path.info");
            if (Directory.Exists(ModApi.UpdateManager.UpdateManager.AppDataPath))
            {
                try
                {
                    DeleteFolder(ModApi.UpdateManager.UpdateManager.AppDataPath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
            if (!Directory.Exists(ModApi.UpdateManager.UpdateManager.AppDataPath))
                Directory.CreateDirectory(ModApi.UpdateManager.UpdateManager.AppDataPath);
            if (!File.Exists(infoFilePath))
                File.WriteAllText(infoFilePath, Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString());*/
            LauncherSettings.Load();

            if (Program.IsInstalledDarkInjectionCompatible())
                new Program().Execute();
            else
            {
                MessageBox.Show(Strings.IncompatibleDarkInjection.Replace(@"\n", "\n"), Strings.IncompatibleMod, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                Process.Start(DarkInjectionPageURL);
            } } else Application.Exit();
        }

        static void DeleteFolder(string path)
        {
            foreach (string s in Directory.EnumerateFiles(path))
                File.Delete(s);
            foreach (string s in Directory.EnumerateDirectories(path))
                DeleteFolder(s);
            Directory.Delete(path);
        }

        public static void ERROR_TESTING_MSG(string message)
        {
            if (ERROR_TESTING)
            {
                MessageBox.Show(message, "Error testing");
            }
        }

        void Execute()
        {
            try
            {
                // Steam users need to do something different
                if (!SporePath.SporeIsInstalledOnSteam())
                {
                    if (LauncherSettings.ForcedGalacticAdventuresSporeAppPath != null)
                        this.ExecutablePath = LauncherSettings.ForcedGalacticAdventuresSporeAppPath;

                    if (LauncherSettings.ForcedSporebinEP1Path == null)
                    {
                        this.ProcessSporebinPath();


                        ERROR_TESTING_MSG("1. SporebinEP1 path: " + this.SporebinPath);
                    }
                    else
                    {
                        SporebinPath = LauncherSettings.ForcedSporebinEP1Path;
                        ERROR_TESTING_MSG("1. SporebinEP1 path is overridden");
                    }

                    // use the default path for now (we might have to use a different one for Origin)

                    if (LauncherSettings.ForcedGalacticAdventuresSporeAppPath == null)
                    {
                        this.ExecutablePath = this.SporebinPath + "SporeApp.exe";

                        this.ProcessExecutableType();

                        ERROR_TESTING_MSG("2. Executable type: " + this._executableType);

                        if (this._executableType == GameVersionType.None)
                        {
                            // don't execute the game if the user closed the dialog
                            return;
                        }
                    }
                    else
                    {
                        ERROR_TESTING_MSG("2. Executable type is overridden.");
                        this._executableType = LauncherSettings.GameVersion;
                    }

                    // get the correct executable path
                    if (LauncherSettings.ForcedGalacticAdventuresSporeAppPath == null)
                    {
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
                    }

                    ERROR_TESTING_MSG("3. Real executable path: " + this.ExecutablePath);

                    // we must also check if the steam_api.dll doesn't exist (it's required for Origin users)
                    if (GameVersion.RequiresModAPIFix(this._executableType) && !File.Exists(this.SporebinPath + "steam_api.dll"))
                    {
                        if (!HandleOriginUsers())
                        {
                            return;
                        }
                    }

                    string dllEnding = GameVersion.VersionNames[(int)this._executableType];

                    ERROR_TESTING_MSG("4. DLL suffix: " + dllEnding);

                    if (dllEnding == null)
                    {
                        MessageBox.Show(Strings.VersionNotDetected, CommonStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    InjectNormalSporeProcess(dllEnding);

                }
                else
                {
                    InjectSteamSporeProcess();
                }

                int lastError = System.Runtime.InteropServices.Marshal.GetLastWin32Error();

                if ((Program.CurrentError != 0) && (Program.CurrentError != 18) && (Program.CurrentError != 87) && (Program.CurrentError != lastError))
                {
                    try
                    {
                        ThrowWin32Exception("Something went wrong", Program.CurrentError);
                    }
                    catch (Exception ex)
                    {
                        ShowError(ex);
                    }
                }
                
                if ((lastError != 0) && (lastError != 18))
                    ThrowWin32Exception("Something went wrong", lastError);
            }
            catch (Exception ex)
            {
                ShowError(ex);
                return;
            }
        }
        
        public static void ShowError(Exception ex)
        {
            MessageBox.Show(Strings.GalacticAdventuresNotExecuted + "\n" + ModApiHelpThreadURL + "\n\n" + ex.GetType() + "\n\n" + ex.Message + "\n\n" + ex.StackTrace, CommonStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            if (ex is System.ComponentModel.Win32Exception)
            {
                var exc = ex as System.ComponentModel.Win32Exception;
                //"NativeErrorCode: " + exc.NativeErrorCode.ToString() + "\n" + 
                MessageBox.Show("ErrorCode: " + exc.ErrorCode.ToString() + "\n" +
                    "NativeErrorCode: " + exc.NativeErrorCode.ToString() + "\n" +
                    "HResult: " + exc.HResult.ToString() + "\n", "Additional Win32Exception Error Info");

                if (exc.InnerException != null)
                {
                    MessageBox.Show("ErrorCode: " + exc.InnerException.GetType() + "\n\n" + exc.InnerException.Message + "\n\n" + exc.InnerException.StackTrace, "Win32Exception InnerException Error Info");
                }
            }
            Process.Start(ModApiHelpThreadURL);
        }

        void InjectDLLs(string dllEnding, List<string> dllExceptions)
        { 
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
            Injector.InjectDLL(this.ProcessInfo, coreDllOutPath);
            //string legacyModApiDllOutPath = Path.Combine
            Injector.InjectDLL(this.ProcessInfo, Path.GetFullPath(MODAPI_DLL));

            Program.CurrentError = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            foreach (string s in Directory.EnumerateFiles(mFolder)
                                    .Where(x => !Path.GetFileName(x).Equals(coreDllName, StringComparison.OrdinalIgnoreCase) && 
                                                x.ToLowerInvariant().EndsWith(".dll") &&
                                                !dllExceptions.Contains(Path.GetFileName(x)))
                    )
            {
                ERROR_TESTING_MSG("Now injecting: " + s);
                Injector.InjectDLL(this.ProcessInfo, s);
            }

            foreach (var file in baseFolder
                                    .EnumerateFiles("*" + dllEnding + ".dll")
                                    .Where(x => x.Name != MODAPI_DLL && !dllExceptions.Contains(x.Name))
                    )
            {
                ERROR_TESTING_MSG("5.* Preparing " + file.Name);
                // the ModAPI dll should already be loaded
                if (file.Name != MODAPI_DLL)
                {
                    ERROR_TESTING_MSG("5.* Injecting " + file.Name);
                    Injector.InjectDLL(this.ProcessInfo, file.FullName);
                    Program.CurrentError = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                }
                else
                    ERROR_TESTING_MSG("5.* " + file.Name + " was already injected!");
            }

        }

        void InjectNormalSporeProcess(string dllEnding)
        {
            var dllExceptions = CheckOutdatedCoreDlls();

            CreateSporeProcess();

            InjectDLLs(dllEnding, dllExceptions);

            ResumeSporeProcess();
        }

        private const int WAIT_ABANDONED = 0x00000080;

        // Steam spore needs special treatment: the game will close if not executed through Steam
        void InjectSteamSporeProcess()
        {
            var dllExceptions = CheckOutdatedCoreDlls();

            string steamPath = ProcessSteamPath();
            string sporeAppName = "SporeApp";
            steamPath = Path.Combine(steamPath, "Steam.exe");

            System.Diagnostics.Process.Start(steamPath, "-applaunch " + SporePath.GalacticAdventuresSteamID.ToString());

            //Thread.Sleep(50);
            
            Process[] processes = Process.GetProcessesByName(sporeAppName);
            while (processes.Length == 0)
            {
                // Just wait and try again
                //Thread.Sleep(50);
                //continue;
                processes = Process.GetProcessesByName(sporeAppName);
            }
            /*else
            {*/

            var process = processes[0];

            var pOpenThreads = new List<IntPtr>();
            // now pause all threads so we can inject; it's the equivalent to running in suspended state
            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = NativeMethods.OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);
                if (pOpenThread != IntPtr.Zero) //((pOpenThread != IntPtr.Zero) && (Injector.WaitForSingleObject(pOpenThread, 0) != WAIT_ABANDONED))
                {
                    NativeMethods.SuspendThread(pOpenThread); //     r/therewasanattempt
                    pOpenThreads.Add(pOpenThread);
                }
            }
            this.ProcessInfo.dwProcessId = (uint)process.Id;
            this.ProcessInfo.hProcess = process.Handle; //NativeMethods.OpenProcess(NativeMethods.AccessRequired, false, ProcessInfo.dwProcessId);
            processHandle = this.ProcessInfo.hProcess;
            // We must detect the executable type now
            /*this.ProcessExecutableType();
            if (this.ExecutableType == GameVersionType.None)
            {
                // don't execute the game if the user closed the dialog
                return;
            }
            // get the corerct executable path
            if (LauncherSettings.ForcedGalacticAdventuresSporeAppPath == null)
                this.ExecutablePath = process.MainModule.FileName;

            this.ProcessExecutableType();*/
            _executableType = GameVersionType.Steam_Patched;
            string dllEnding = GameVersion.VersionNames[(int)_executableType]; //string dllEnding = GameVersion.VersionNames[(int)this.ExecutableType];

            InjectDLLs(dllEnding, dllExceptions);

            // Resume the process again
            foreach (IntPtr pOpenThread in pOpenThreads)
            {
                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                uint suspendCount = 0;
                do
                {
                    suspendCount = NativeMethods.ResumeThread(pOpenThread);
                } while (suspendCount > 0);

                NativeMethods.CloseHandle(pOpenThread);

            }

            return;
            //}
            //}
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

            string currentSporebinPath = string.Empty;
            if (LauncherSettings.ForcedSporebinEP1Path != null)
                currentSporebinPath = LauncherSettings.ForcedSporebinEP1Path;
            else
                currentSporebinPath = this.SporebinPath;

            ERROR_TESTING_MSG("currentSporebinPath: " + currentSporebinPath + "\nExecutablePath: " + ExecutablePath);

            if (!NativeMethods.CreateProcess(null, "\"" + this.ExecutablePath + "\" " + sb.ToString(),
                    IntPtr.Zero, IntPtr.Zero, false, ProcessCreationFlags.CREATE_SUSPENDED, IntPtr.Zero, currentSporebinPath, ref this.StartupInfo, out this.ProcessInfo))
            {
                //throw new InjectException(Strings.ProcessNotStarted);
                int lastError = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                System.Windows.Forms.MessageBox.Show("Error: " + lastError.ToString(), Strings.ProcessNotStarted);
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

        string ProcessSteamPath()
        {
            string path = PathDialogs.ProcessSteam();

            if (path == null || !Directory.Exists(path))
            {

                throw new InjectException(CommonStrings.SteamNotFound);
            }

            this.SteamPath = path;

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
                var dialog = new DownloadDialog(Strings.DownloadFixTitle);
                dialog.DownloadURL = ModAPIFixDownloadURL;
                dialog.FileName = "SporeApp_ModAPIFix.zip";
                dialog.DownloadCompletedText = Strings.ExtractingFiles;
                dialog.DownloadCompletedHandler = (sender, args) =>
                {
                    string temporaryFile = Path.GetTempFileName();
                    File.WriteAllBytes(temporaryFile, args.Result);

                    result = ExtractFixFiles(temporaryFile, outputPath);

                    File.Delete(temporaryFile);

                    if (result)
                    {
                        // only ask if the download succeded
                        var dialogResult = MessageBox.Show(Strings.DownloadComplete, Strings.DownloadCompleteTitle, MessageBoxButtons.YesNo);
                        result = dialogResult == DialogResult.Yes;
                    }
                };

                dialog.ShowDialog();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            return result;
        }

        // -- UTILITY METHODS -- //

        static bool ExtractFixFiles(string zipFile, string outputPath)
        {
            try
            {
                // ZipFile.ExtractToDirectory(zipFile, outputPath);
                using (ZipArchive archive = ZipFile.Open(zipFile, ZipArchiveMode.Read))
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
                System.Windows.Forms.MessageBox.Show("Error: " + error.ToString(), info);
                throw new System.ComponentModel.Win32Exception(error);
            }
        }

        static bool IsInstalledDarkInjectionCompatible()
        {
            string di230 = "di_9_r_beta2-3-0".ToLowerInvariant();
            try
            {
                bool returnValue = true;
                InstalledMods mods = new InstalledMods();
                mods.Load();
                ERROR_TESTING_MSG("BEGIN MOD CONFIGURATION NAMES");
                foreach (ModConfiguration configuration in mods.ModConfigurations)
                {
                    ERROR_TESTING_MSG(configuration.Name);
                    if (configuration.Name.ToLowerInvariant().Contains(di230))
                    {
                        returnValue = false;
                        break;
                    }
                }
                ERROR_TESTING_MSG("END MOD CONFIGURATION NAMES");
                return returnValue;
            }
            catch (Exception ex)
            {
                ERROR_TESTING_MSG("FAILED TO INSPECT MOD CONFIGURATION NAMES\n" + ex);
                return true;
            }
        }

        /// <summary>
        /// Checks that the current core DLLs version is high enough for all installed mods.
        /// It will show an error dialog for those mods that won't be loaded becaues they
        /// require a higher version.
        /// Returns a list of all .dll files that MUST NOT be loaded.
        /// </summary>
        /// <returns></returns>
        static List<string> CheckOutdatedCoreDlls()
        {
            var disabledDlls = new List<string>();

            string modConfigsPath = Path.Combine(
                Directory.GetParent(Assembly.GetEntryAssembly().Location).ToString(), 
                "ModConfigs");

            var disabledMods = new List<string>();

            var coreDllsVersion = UpdateManager.CurrentDllsBuild;
            foreach (var modFolder in Directory.GetDirectories(modConfigsPath))
            {
                var xmlPath = Path.Combine(modFolder, "ModInfo.xml");
                if (File.Exists(xmlPath))
                {
                    try
                    {
                        var document = new XmlDocument();
                        document.Load(xmlPath);
                        var modNode = document.SelectSingleNode("/mod");

                        if (modNode != null &&
                            modNode.Attributes["dllsBuild"] != null &&
                            Version.TryParse(modNode.Attributes["dllsBuild"].Value, out Version requiredDllsVersion) &&
                            requiredDllsVersion > coreDllsVersion)
                        {
                            disabledMods.Add(GetModDisplayName(modNode));

                            foreach (var file in GetModFiles(modNode))
                            {
                                if (file.EndsWith(".dll"))
                                {
                                    disabledDlls.Add(Path.GetFileName(file));
                                }
                            }
                        }
                    }
                    catch { }
                }
            }

            if (disabledMods.Any())
            {
                var sb = new StringBuilder();
                sb.Append("The following mods cannot be loaded because they require a greater version of the ModAPI Core DLLs. " +
                    "Please, restart the launcher and allow it to update.\n");
                foreach (var mod in disabledMods)
                {
                    sb.Append(" - ");
                    sb.Append(mod);
                    sb.Append("\n");
                }

                MessageBox.Show(sb.ToString(), "Error: some mods could not be loaded");
            }

            return disabledDlls;
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
