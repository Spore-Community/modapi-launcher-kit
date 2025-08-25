using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using System.Threading;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.ComponentModel;

using System.Diagnostics;

using ModAPI.Common;
using System.Windows.Interop;
using System.Xml;
using ModAPI.Common.Update;
using ModAPI.Common.Types;

namespace Spore_ModAPI_Easy_Installer
{
    public static class EasyInstaller
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
        public static string outcome = string.Empty;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            if (!Permissions.IsAdministrator())
            {
                UpdateManager.CheckForUpdates();
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

                var cmdArgs = Environment.GetCommandLineArgs();
                if ((cmdArgs.Length == 4) && bool.TryParse(cmdArgs[2], out bool configResult) && bool.TryParse(cmdArgs[3], out bool uninstall))
                {
                    string modName = cmdArgs[1];

                    if (configResult)
                    {
                        Thread thread = GetXmlInstaller(modName, configResult, uninstall, true, out XmlInstallerWindow win);
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
                        }
                    }
                    for (int i = 0; i < results.Count; i++) //foreach (ResultType type in results)
                    {
                        //int index = results.IndexOf(type);
                        ResultType type = results[i];
                        outcome += GetResultText(type, Path.GetFileNameWithoutExtension(inputPaths[i]), errorStrings[i]) + "\n";
                    }

                    ModList.Save();
                    ShowExitMessageBox();
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

                        entry.ExtractToFile(configOutPath, true);
                        if (outputPath != null)
                        {
                            string fileOutPath = Path.Combine(outputPath, entry.Name);

                            File.Copy(configOutPath, fileOutPath);
                        }
                    }
                    ProgressDialog.SetProgress((int)((entriesExtracted / (float) numEntries) * 100.0f));

                    entriesExtracted++;
                }
            }

            return ResultType.Success;
        }

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

                    string modPath = Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString(), "ModConfigs", modName);
                    if (Directory.Exists(modPath))
                        DeleteFolder(modPath);

                    Directory.CreateDirectory(modPath);


                    Thread installerThread = GetXmlInstaller(modName, false, false, true, out XmlInstallerWindow win);

                    foreach (var fileEntry in archive.Entries)
                    {
                        fileEntry.ExtractToFile(Path.Combine(modPath, fileEntry.Name), true);
                    }

                    win.SignalRevealInstaller();

                    installerThread.Join();

                    if (!XmlInstallerCancellation.Cancellation[modName.Trim('"')])
                        return win.GetResult();
                    else
                        return ResultType.ModNotInstalled;
                }
                else if (entry != null)
                {
                    return ResultType.UnsupportedFile;
                }
                else
                {
                    return ResultType.NoInstallerFound;
                }
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

        static Thread GetXmlInstaller(string modName, bool configure, bool uninstall, bool show, out XmlInstallerWindow win)
        {
            XmlInstallerWindow xmlWin = null;
            Thread thread = new Thread(() =>
            {
                xmlWin = new XmlInstallerWindow(modName, configure, uninstall);
                if (show)
                {
                    xmlWin.ShowDialog();
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            // wait until xmlWin has been set
            while (xmlWin == null)
            {
                Thread.Sleep(10);
            }

            win = xmlWin;
            return thread;
        }

        static ResultType InstallSporemod(string inputFile, string modName)
        {
            var result = TryExecuteInstaller(inputFile, modName);

            if (result == ResultType.NoInstallerFound)
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
        }
        static string GetResultText(ResultType result, string modName, string errorString)
        {
            if (result == ResultType.Success)
            {
                // show message to the user
                return Strings.ModInstalled1 + modName + Strings.ModInstalled2;
            }
            else if (result == ResultType.ModNotInstalled)
            {
                return Strings.ModInstalled1 + modName + Strings.CancelledInstallation;
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

        static void ShowExitMessageBox()
        {
            Thread thread = new Thread(() =>
            {
                var win = ModInstalledWindow.GetDialog(outcome, Strings.InstallationCompleted); //"Your selected mods are done installing.\n" + 
                win.Closed += (snedre, rags) => Process.GetCurrentProcess().Kill();
                win.ShowDialog();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }
    }
}
