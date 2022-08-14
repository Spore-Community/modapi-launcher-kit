using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Threading;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.ComponentModel;

using System.Diagnostics;

using ModAPI_Installers;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Xml;
using ModApi.UpdateManager;

namespace Spore_ModAPI_Easy_Installer
{
    static class EasyInstaller
    {
        enum FileChooserType
        {
            File,
            Directory
        }

        enum FileType
        {
            None,  // none of the supported types
            EXE,
            DLL,
            SporeMod,  // just a .zip renamed to .sporemod
            Package,
            Spore_Package // a .package that goes to the Spore data folder instead of the EP1 one
        }

        enum ResultType
        {
            Success,
            InstallerExecuted,  // we have found a custom installer, close Easy installer and execute that one
            UnsupportedFile,
            GalacticAdventuresNotFound,
            UnauthorizedAccess,
            InvalidPath,
            ModNotInstalled
        }
        //// Show a file chooser and returns the path selected. It can ask for files or directories.
        //static string ShowFileChooser(FileChooserType type, string title, string filter);

        //// Returns the path of the file that must be installed. It can get it from the command line or from a file chooser dialog.
        //static string GetInputPath();
        ///* -- if no arguments have been provided to the .exe, call ShowFileChooser(FileChooserType.Directory) */

        //// Determines which folder the file goes to depending on its type
        //static string GetOutputPath(FileType type);

        //// Determines what kind of file is the argument given based on the extension
        //static FileType GetFileType(string fileName);

        // 

        public static InstalledMods ModList = new InstalledMods();
        static ProgressWindow ProgressDialog = new ProgressWindow(Strings.InstallingModTitle);
        public static Form Form1;// = new Form();
        public static string outcome = string.Empty;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            if (!Permissions.IsAdministrator())
            {
                ModApi.UpdateManager.UpdateManager.CheckForUpdates();
                Permissions.RerunAsAdministrator();
            }
            else
            {
                Application.EnableVisualStyles();
                //fix for that one guy with a weird GPU, for whom all WPF UI breaks when shown unless software rendering is enabled
                if (File.Exists(Environment.ExpandEnvironmentVariables(@"%appdata%\Spore ModAPI Launcher\WpfUseSoftwareRendering.info")))
                    System.Windows.Media.RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
                LauncherSettings.Load();
                ModList.Load();

                /*Application.ApplicationExit += (sneder, args) => {
                    ModList.Save();
                    Thread thread = new Thread(() =>
                    {
                        var win = ModInstalledWindow.GetDialog(outcome, Strings.InstallationCompleted);
                        win.Closed += (snedre, rags) => Process.GetCurrentProcess().Kill();
                        win.Show();
                        Application.Run();
                    });
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    thread.Join();
                };*/






                var cmdArgs = Environment.GetCommandLineArgs();
                if ((cmdArgs.Length == 3) && bool.TryParse(cmdArgs[2], out bool configResult))
                {
                    string modName = cmdArgs[1];
                    //MessageBox.Show(modName, "modName");

                    if (configResult)
                    {
                        Thread thread = GetXmlInstaller(modName, configResult, true, out XmlInstallerWindow win);
                        thread.Join();
                    }
                }
                else
                {
                    // 1st: We get the input path of the mod we are going to install
                    string[] inputPaths = GetInputPaths(); string[] errorStrings = new string[inputPaths.Length];
                    if (inputPaths.Length < 1) return;

                    List<ResultType> results = new List<ResultType>();
                    // 2nd: Check what kind of input we got, and proceed to install it
                    for (int i = 0; i < inputPaths.Length; i++)
                    {
                        string inputPath = inputPaths[i];
                        FileType fileType = GetFileType(Path.GetFileName(inputPath));
                        string modName = Path.GetFileNameWithoutExtension(inputPath);
                        ResultType result = ResultType.UnsupportedFile;
                        

                        try
                        {
                            switch (fileType)
                            {
                                case FileType.Package:
                                    // install the package normally
                                    result = InstallPackage(inputPath, modName);
                                    // add to installed mods list
                                    ModList.AddMod(modName).AddFile(Path.GetFileName(inputPath), SporePath.Game.GalacticAdventures);
                                    break;

                                case FileType.SporeMod:
                                    // first, check if there is an installer
                                    // if not, put every file in the ZIP to the corresponding place
                                    // and add to installed mods list
                                    result = InstallSporemod(inputPath, modName);
                                    break;

                                default:
                                    result = ResultType.UnsupportedFile;
                                    break;
                            }
                            results.Add(result);
                        }
                        catch (Exception ex)
                        {
                            errorStrings[i] = ex.Message; results.Add(ResultType.UnsupportedFile);
                            MessageBox.Show(ex.ToString() + "\n" + ex.StackTrace, "AAA");
                        }
                    }
                    for (int i = 0; i < results.Count; i++) //foreach (ResultType type in results)
                    {
                        //int index = results.IndexOf(type);
                        ResultType type = results[i];
                        outcome += GetResultText(type, Path.GetFileNameWithoutExtension(inputPaths[i]), errorStrings[i]) + "\n";
                    }
                    WaitForExit();
                }
            }
        }
        static string[] GetInputPaths()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            // If the file path was specified as an argument (i.e. dragged to the installer)
            if (arguments.Length > 1)
            { List<string> paths = new List<string>();
                for (int i = 1; i < arguments.Length; i++) //return arguments[1];
                {
                    if (File.Exists(arguments[i]))
                        paths.Add(arguments[i]);
                } return paths.ToArray();
            } else // No file specified, show the user a dialog to choose it
            {
                return ShowFileChooser(FileChooserType.File, Strings.FileChooserTitle,
                    Strings.FileChooserFilter, 4);
            }
        }
        static string[] ShowFileChooser(FileChooserType type, string title, string filter, int filterIndex)
        {
            string[] paths = new string[0];
            Thread thread = new Thread(() =>
            {
                if (type == FileChooserType.File)
                {
                    var dialog = new OpenFileDialog()
                    {
                        Title = title,
                        Filter = filter,
                        FilterIndex = filterIndex,
                        Multiselect = true
                    };
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        paths = dialog.FileNames;
                    }
                }
                else
                {
                    var dialog = new FolderBrowserDialog();
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        paths = new string[] { dialog.SelectedPath };
                    }
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            return paths;
        }

        static string ShowDirectoryChooser()
        {string[] dirs = ShowFileChooser(FileChooserType.Directory, null, null, 0);
            if (dirs.Length > 0) return dirs[0]; else return null;
        }

        static FileType GetFileType(string fileName)
        {
            if (fileName == null) return FileType.None;

            if (fileName.ToLowerInvariant().EndsWith(".package"))
            {
                // check if it's Spore or GA
                string[] splits;
                if (fileName.Contains('\\'))
                {
                    splits = fileName.Split('\\');
                }
                else
                {
                    splits = fileName.Split('/');
                }

                if (splits.Length > 1)
                {
                    // check the folder that contains the file
                    if (splits[splits.Length - 2].ToUpper() == "SPORE")
                    {
                        return FileType.Spore_Package;
                    }
                }

                // default to GA Data
                return FileType.Package;
            }
            /*else if (fileName.EndsWith(".exe"))
            {
                return FileType.EXE;
            }*/
            else if (fileName.EndsWith(".dll"))
            {
                return FileType.DLL;
            }
            else if (fileName.ToLowerInvariant().EndsWith(".sporemod"))
            {
                return FileType.SporeMod;
            }
            else
            {
                return FileType.None;
            }
        }

        static string GetOutputPath(FileType type)
        {
            switch (type)
            {
                case FileType.DLL:
                    return Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString();

                case FileType.Package:
                    return GetGADataPath();

                case FileType.Spore_Package:
                    return GetSporeDataPath();

                default:
                    return null;
            }
        }

        // this takes a "pathType" from an InstalledFile
        static string GetOutputPath(string pathType)
        {
            switch (pathType)
            {
                case "GalacticAdventures":
                    return GetOutputPath(FileType.Package);

                case "Spore":
                    return GetOutputPath(FileType.Spore_Package);

                case "None":
                    // we use "None" for dlls
                    return GetOutputPath(FileType.DLL);

                default:
                    return null;
            }
        }

        static string GetGADataPath()
        {
            string path = PathDialogs.ProcessGalacticAdventures();

            if (path != null)
            {
                // now we have the path to SporebinEP1; move it to Data
                path = SporePath.MoveToData(SporePath.Game.GalacticAdventures, SporePath.GetRealParent(path));
            }

            return path;
        }

        static string GetSporeDataPath()
        {
            string path = PathDialogs.ProcessSpore();

            if (path != null)
            {
                // now we have the path to Sporebin; move it to Data
                path = SporePath.MoveToData(SporePath.Game.Spore, SporePath.GetRealParent(path));
            }

            return path;
        }

        static void TryCloseProgressDialog()
        {
            while (!ProgressDialog.IsDisposed)
            {
                try
                {
                    ProgressDialog.Close();
                }
                catch
                {
                    Thread.Sleep(250);
                }
            }
        }


        static ResultType InstallPackage(string inputFile, string modName)
        {
            ResultType result = ResultType.Success;
            Exception ex = null;

            string outputPath = GetOutputPath(FileType.Package);
            if (outputPath == null)
            {
                return ResultType.GalacticAdventuresNotFound;
            }

            try
            {
                // File.Copy(inputFile, Path.Combine(new string[] { outputPath, Path.GetFileName(inputFile) }), true);

                ProgressDialog.SetDescriptionText(Strings.ModIsInstalling1 + modName + Strings.ModIsInstalling2);

                string fileName = Path.GetFileName(inputFile);

                var client = new WebClient();

                bool isFinished = false;

                client.DownloadFileCompleted += (object sender, AsyncCompletedEventArgs args) =>
                    {
                        isFinished = true;
                        // if the transfer is too fast, it tries to close the dialog before it even loaded. So keep trying until we have it
                        TryCloseProgressDialog();

                        if (args.Error != null)
                        {
                            if (args.Error.GetType() == typeof(WebException))
                            {
                                result = ResultType.UnauthorizedAccess;
                                ex = ((WebException)args.Error).InnerException;
                            }
                            else
                            {
                                ex = args.Error;
                            }
                        }
                    };

                client.DownloadProgressChanged += (object sender, DownloadProgressChangedEventArgs args) =>
                {
                    if (!isFinished)
                    {
                        try
                        {
                            ProgressDialog.SetProgress(args.ProgressPercentage);
                            ProgressDialog.SetProgressText(Strings.CopyingFile + " \"" + fileName + "\" (" + (args.BytesReceived / 1000) + " / " + (args.TotalBytesToReceive / 1000) + " KBs)");
                        }
                        catch { }
                    }
                };


                ShowProgressDialog(client, inputFile, Path.Combine(new string[] { outputPath, Path.GetFileName(inputFile) }));

                
                if (ex != null)
                {
                    throw ex;
                }

                return result;
            }
            catch (UnauthorizedAccessException)
            {
                return ResultType.UnauthorizedAccess;
            }
        }

        static SporePath.Game GetGameFromFileType(FileType type)
        {
            switch (type)
            {
                case FileType.Package:
                    return SporePath.Game.GalacticAdventures;

                case FileType.Spore_Package:
                    return SporePath.Game.Spore;

                default:
                    return SporePath.Game.None;
            }
        }

        // Installs the files and adds them to the list in the ModConfiguration (so they can be removed if something goes wrong)
        private static ResultType ExtractSporemodZip(string inputFile, ModConfiguration mod)
        {
            //TODO check if it contains an installer
            string modName = Path.GetFileNameWithoutExtension(inputFile);
            using (ZipArchive archive = ZipFile.Open(inputFile, ZipArchiveMode.Read))
            {
                int numEntries = archive.Entries.Count;
                int entriesExtracted = 0;
                string configsPath = Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString(), "ModConfigs");
                if (!Directory.Exists(configsPath))
                    Directory.CreateDirectory(configsPath);
                string modPath = Path.Combine(configsPath, modName);
                if (!Directory.Exists(modPath))
                    Directory.CreateDirectory(modPath);


                foreach (var entry in archive.Entries)
                {
                    ProgressDialog.SetProgressText(Strings.ExtractingFile + " \"" + entry.Name + "\" (" + entriesExtracted + " / " + numEntries + " files).");

                    // we use the FullName because we also might check the folder that contains that file
                    var type = GetFileType(entry.FullName);
                    string outputPath = GetOutputPath(type);
                    if (entry.FullName.Contains("."))
                    {
                        mod.AddFile(entry.Name, GetGameFromFileType(type));
                        string configOutPath = Path.Combine(modPath, entry.Name);
                        DebugShowMessageBox(configOutPath, "configOutPath");
                        entry.ExtractToFile(configOutPath, true);
                        if (outputPath != null)
                        {
                            string fileOutPath = Path.Combine(outputPath, entry.Name);
                            DebugShowMessageBox(fileOutPath, "fileOutPath");
                            File.Copy(configOutPath, fileOutPath);
                        }
                    }
                    ProgressDialog.SetProgress((int)((entriesExtracted / (float) numEntries) * 100.0f));

                    entriesExtracted++;
                }
            }

            return ResultType.Success;
        }

        private static string ConvertToArgument(string path)
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

        [DllImport("user32.dll", ExactSpelling = true)]
        static extern bool IsWindow(IntPtr hWnd);

        private static bool CheckModCoreDllsVersion(ZipArchiveEntry xmlEntry)
        {
            try
            {
                using (var stream = xmlEntry.Open())
                {
                    var document = new XmlDocument();
                    document.Load(stream);

                    if (!UpdateManager.HasValidDllsVersion(document))
                    {
                        return false;
                    }
                }
            }
            catch {
            }
            return true;
        }

        static ResultType TryExecuteInstaller(string inputFile, string modName)
        {
            string tempFile = null;

            using (ZipArchive archive = ZipFile.Open(inputFile, ZipArchiveMode.Read))
            {
                var entry = archive.GetEntry("Installer.exe");
                var xmlEntry = archive.GetEntry("ModInfo.xml");


                if (xmlEntry != null)
                {
                    if (!CheckModCoreDllsVersion(xmlEntry))
                    {
                        MessageBox.Show($"\"{modName}\"{Strings.UnsupportedDllVersion}", 
                            Strings.UnsupportedDllVersionTitle);
                        return ResultType.ModNotInstalled;
                    }

                    /*XmlDocument Document = new XmlDocument();
                    archive.GetEntry("ModInfo.xml").Open();
                    modName*/
                    string modPath = Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString(), "ModConfigs", modName);
                    if (Directory.Exists(modPath))
                        DeleteFolder(modPath);

                    Directory.CreateDirectory(modPath);

                    string themeInfoPath = Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString(), "LoadTheme.info");
                    if (File.Exists(themeInfoPath))
                        File.Delete(themeInfoPath);
                    string revealInstallerInfoPath = Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString(), "RevealXmlInstaller.info");
                    if (File.Exists(revealInstallerInfoPath))
                        File.Delete(revealInstallerInfoPath);

                    /*XmlInstallerWindow win = */
                    //bool installCancelled = false;
                    Thread installerThread = GetXmlInstaller(modName, false, true, out XmlInstallerWindow win);
                    //_showXmlInstaller = true;//Thread showThread = new Thread(() => xmlWin.Show());// xmlWin.Dispatcher.Invoke(new Action(() => xmlWin.Show())));
                    /*showThread.SetApartmentState(ApartmentState.STA);
                    showThread.Start();
                    showThread.Join();*/
                    //if (archive.GetEntry("Theme.xaml"))
                    foreach (var fileEntry in archive.Entries)
                    {
                        fileEntry.ExtractToFile(Path.Combine(modPath, fileEntry.Name), true);
                        /*if (Path.GetFileName(fileEntry.Name).ToLowerInvariant() == "theme.xaml")
                            File.WriteAllText(themeInfoPath, string.Empty);*/
                    }
                    //win.InstallCancelled += (sneder, args) => installCancelled = true;
                    File.WriteAllText(revealInstallerInfoPath, string.Empty);
                    /*if (win != null)
                        win.Closed += (sneder, args) =>
                        {installCancelled = win.cancelled;
                            installerThread.Abort();
                        };
                    else
                        MessageBox.Show("win == null");*/
                    //RevealXmlInstaller = true; //Thread revealThread = new Thread(() => xmlWin.RevealInstaller()); //xmlWin.Dispatcher.Invoke(new Action(() => xmlWin.RevealInstaller())));
                    //revealThread.SetApartmentState(ApartmentState.STA); revealThread.Start(); xmlWin.Closed += (sneder, args) => System.Windows.Application.Current.Shutdown(); revealThread.Join();
                    installerThread.Join();
                    
                    if (!XmlInstallerCancellation.Cancellation[modName.Trim('"')])
                        return ResultType.InstallerExecuted;
                    else
                        return ResultType.ModNotInstalled;
                }
                else if (entry != null)
                {
                    return ResultType.UnsupportedFile;
                    /*tempFile = Path.GetTempFileName();
                    try
                    {
                        entry.ExtractToFile(tempFile, true);
                    }
                    catch (Exception ex)
                    {
                        // delete the temp file and propagate the exception
                        File.Delete(tempFile);
                        throw ex;
                    }

                    string validInstaller = "!!!!!2017_DI_9_r_Beta2-1-6";
                    bool modNameValid = modName == validInstaller;
                    bool modInstallerSizeValid = new FileInfo(tempFile).Length == 13023232;
                    //MessageBox.Show("modName: " + modName + "\ninputFile: " + inputFile + "\n\nmodNameValid: " + modNameValid + "\nmodInstallerSizeValid: " + modInstallerSizeValid);

                    if (modNameValid && modInstallerSizeValid)
                    {
                        var startInfo = new ProcessStartInfo()
                        {
                            UseShellExecute = false,  // we need this to execute a temp file
                            FileName = tempFile,
                            Arguments =
                                ConvertToArgument(inputFile) + " " +
                                ConvertToArgument(GetOutputPath(FileType.DLL)) + " " +
                                ConvertToArgument(GetOutputPath(FileType.Package)) + " " +
                                ConvertToArgument(GetOutputPath(FileType.Spore_Package))
                        };

                        var process = Process.Start(startInfo);
                        process.WaitForExit();

                        File.Delete(tempFile);

                        return ResultType.InstallerExecuted;
                    }
                    else
                    {
                        MessageBox.Show("For security reasons, EXE custom installers have been disabled. If you believe this mod is safe, inform its developer of the issue, and tell them to look into \"XML Custom Installers\".");
                        return ResultType.UnsupportedFile;
                    }*/
                }
                else
                {
                    return ResultType.Success;
                }

                /*
                    // there's no exe custom installer, check for an xml one
                    if (xmlEntry == null)
                    {
                        // there's no custom installer of any kind
                        return ResultType.Success;
                    }*/
            }
        }

        public static void DeleteFolder(string path)
        {
            foreach (string s in Directory.EnumerateFiles(path))
                File.Delete(s);

            foreach (string s in Directory.EnumerateDirectories(path))
                DeleteFolder(s);

            Directory.Delete(path);
        }

        //static bool _showXmlInstaller = false;
        public static bool RevealXmlInstaller = false;

        //[STAThread]
        static Thread GetXmlInstaller(string modName, bool configure, bool show, out XmlInstallerWindow win)
        {
            XmlInstallerWindow xmlWin = null;
            Thread thread = new Thread(() =>
            {
                //MessageBox.Show(modName, "modName");
                xmlWin = new XmlInstallerWindow(modName, configure);
                if (show)// && (!xmlWin.IsVisible))
                {
                    xmlWin.ShowDialog(); //.Show();
                    //Application.Run(xmlWin);
                    //Application.Run();
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            //thread.Join();
            win = xmlWin;
            
            
            return thread;
        }

        static ResultType InstallSporemod(string inputFile, string modName)
        {
            var result = TryExecuteInstaller(inputFile, modName);

            if ((result == ResultType.InstallerExecuted) || (result == ResultType.ModNotInstalled))
            {
                // the custom installer executed, we don't need to do anything else
                return ResultType.InstallerExecuted;
            }
            else if (result == ResultType.Success)
            {
                // the custom installer just didn't exist, extract as ZIP file

                var mod = ModList.AddMod(modName);

                ShowProgressDialog();
                // ProgressDialog.Show();
                ProgressDialog.SetDescriptionText(Strings.ModIsInstalling1 + modName + Strings.ModIsInstalling2);

                try
                {
                    ExtractSporemodZip(inputFile, mod);
                    //ModList.Save();

                    TryCloseProgressDialog();
                    return ResultType.Success;
                }
                catch (UnauthorizedAccessException)
                {
                    // remove all the files we added (so the mod is not only partially installed)
                    RemoveModFiles(mod);
                    ModList.RemoveMod(mod);

                    TryCloseProgressDialog();
                    return ResultType.UnauthorizedAccess;
                }
                catch (IOException)
                {
                    // remove all the files we added (so the mod is not only partially installed)
                    RemoveModFiles(mod);
                    ModList.RemoveMod(mod);

                    TryCloseProgressDialog();
                    return ResultType.InvalidPath;
                }
                catch (Exception ex)
                {
                    // remove all the files we added (so the mod is not only partially installed)
                    RemoveModFiles(mod);
                    ModList.RemoveMod(mod);

                    TryCloseProgressDialog();
                    // just propagate the exception
                    throw ex;
                }
            }
            else
            {
                // the Installer existed but there was a problem
                return result;
            }
            
        }

        static void RemoveModFiles(ModConfiguration mod)
        {
            foreach (InstalledFile file in mod.InstalledFiles)
            {
                string outputPath = GetOutputPath(file.PathType);

                if (outputPath != null)
                {
                    try
                    {
                        File.Delete(Path.Combine(outputPath, file.Name));
                    }
                    catch
                    {
                        // just continue
                    }
                }
            }
        }


        static string GetErrorMessage(ResultType errorType)
        {
            switch (errorType)
            {
                case ResultType.UnsupportedFile: return Strings.ErrorUnsupportedFile;
                case ResultType.GalacticAdventuresNotFound: return CommonStrings.GalacticAdventuresNotFound;
                case ResultType.UnauthorizedAccess: return CommonStrings.UnauthorizedAccess;
                case ResultType.InvalidPath: return CommonStrings.InvalidPath;

                default:
                    return null;
            }
        }


        // Progress dialog

        static void ShowProgressDialog(WebClient client, string input, string output)
        {
            Exception exception = null;

            Thread thread = new Thread(() =>
            {
                try
                {
                    client.DownloadFileAsync(new Uri(input), output);

                    if (!ProgressDialog.IsDisposed)
                    {
                        ProgressDialog.ShowDialog();
                    }

                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (exception != null)
            {
                throw exception;
            }
        }

        // this one does not block the thread
        static void ShowProgressDialog()
        {
            if (ProgressDialog.IsDisposed)
                ProgressDialog = new ProgressWindow(Strings.InstallingModTitle);
            Thread thread = new Thread(() =>
            {
                ProgressDialog.ShowDialog();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            //thread.Join();
        }
        static string GetResultText(ResultType result, string modName, string errorString)
        {
            if (result == ResultType.Success)
            {
                // show message to the user
                return Strings.ModInstalled1 + modName + Strings.ModInstalled2;
            }
            else if (result == ResultType.InstallerExecuted)
                return string.Empty;
            else if (result == ResultType.ModNotInstalled)
            {
                return Strings.ModInstalled1 + modName + Strings.InstallationCancelled;
            }
            else
            {
                if (errorString == null)
                {
                    errorString = GetErrorMessage(result);
                }
                // show message to the user
                //MessageBox.Show(Strings.ModNotInstalled1 + modName + Strings.ModNotInstalled2 + " " + errorString, Strings.InstallationCancelled);
                return Strings.ModNotInstalled1 + modName + Strings.ModNotInstalled2 + " " + errorString;
            }
        }

        static void WaitForExit()
        {
            //MessageBox.Show("WAITING FOR EXIT");
            /*if (XmlInstallerWindow.installerWindows.Count == 0)
            {
                ShowExitMessageBox();
            }
            else
            {*/
            int counter = 0;
            System.Timers.Timer timer = new System.Timers.Timer(10);
            timer.Elapsed += (sneder, args) =>
            {
                if ((counter > 5) && (XmlInstallerWindow.installerWindows.Count == 0))
                {
                    timer.Stop();
                    ModList.Save();
                    ShowExitMessageBox();
                }
                else
                    counter++;
            };
            timer.Start();
            Application.Run();
            //}
        }

        static void ShowExitMessageBox()
        {
            Thread thread = new Thread(() =>
            {
                //ModList.Save();
                var win = ModInstalledWindow.GetDialog("Your selected mods are done installing.\n" + outcome, Strings.InstallationCompleted);
                win.Closed += (snedre, rags) => Process.GetCurrentProcess().Kill();
                win.ShowDialog();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        private static bool ErrorTesting = File.Exists(Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString(), "debug.txt"));
        static void DebugShowMessageBox(string text, string caption)
        {
            if (ErrorTesting)
                MessageBox.Show(text, caption);
        }
    }
}
