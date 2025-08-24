using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Diagnostics;
using System.Text;

using System.IO;

using ModAPI_Installers;

namespace Spore_ModAPI_Easy_Uninstaller
{

    public static class EasyUninstaller
    {

        private static string SporeDataPath;
        private static string SporeDataEP1Path;
        private static string DllPath;

        private static InstalledMods Mods;

        private static UninstallerForm Form;


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
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
                Application.SetCompatibleTextRenderingDefault(false);

                Form = new UninstallerForm();

                LauncherSettings.Load();

                Mods = new InstalledMods();
                Mods.Load();
                ReloadMods();

                Application.Run(Form);
            }
        }

        public static void ReloadMods()
        {
            Mods.Load();
            Form.AddMods(Mods.ModConfigurations);
        }

        public static void UninstallMods(Dictionary<ModConfiguration, bool> mods)
        {
            List<ModConfiguration> successfulMods = new List<ModConfiguration>();

            try
            {
                foreach (var mod in mods)
                {
                    if (mod.Value)
                    {
                        ExecuteConfigurator(mod.Key, true);
                    }
                    else
                    {
                        RemoveModFiles(mod.Key);
                    }

                    Mods.RemoveMod(mod.Key);

                    successfulMods.Add(mod.Key);
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(CommonStrings.UnauthorizedAccess, Strings.CouldNotUninstall, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Strings.CouldNotUninstall, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (successfulMods.Count > 0)
            {
                Mods.Save();

                // reload before we show the message box
                EasyUninstaller.ReloadMods();

                var sb = new StringBuilder();
                foreach (ModConfiguration mod in successfulMods)
                {
                    sb.Append(mod);
                    sb.Append("\n");
                }

                MessageBox.Show(Strings.ModsWereUninstalled + "\n" + sb.ToString(), Strings.UninstallationSuccessful);
            }
        }

        private static void CheckSporeDataEP1Path(string modName)
        {
            if (SporeDataEP1Path == null)
            {
                string path = PathDialogs.ProcessGalacticAdventures();
                if (path == null && modName != null)
                {
                    throw new Exception(Strings.CouldNotUninstall + " \"" + modName + "\"\n" + CommonStrings.GalacticAdventuresNotFound);
                }
                else
                {
                    SporeDataEP1Path = SporePath.MoveToData(SporePath.Game.GalacticAdventures, SporePath.GetRealParent(path));
                }
            }
        }

        private static void CheckSporeDataPath(string modName)
        {
            if (SporeDataPath == null)
            {
                string path = PathDialogs.ProcessSpore();
                if (path == null && modName != null)
                {
                    throw new Exception(Strings.CouldNotUninstall + " \"" + modName + "\"\n" + CommonStrings.SporeNotFound);
                }
                else
                {
                    SporeDataPath = SporePath.MoveToData(SporePath.Game.Spore, SporePath.GetRealParent(path));
                }
            }
        }

        private static void CheckDllPath()
        {
            if (DllPath == null)
            {
                DllPath = Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString();
            }
        }

        private static string GetOutputPath(string pathType, string modName)
        {
            switch (pathType)
            {
                case "GalacticAdventures":
                    CheckSporeDataEP1Path(modName);
                    return SporeDataEP1Path;

                case "Spore":
                    CheckSporeDataPath(modName);
                    return SporeDataPath;

                case "None":
                    CheckDllPath();
                    return DllPath;

                default:
                    return null;
            }
        }

        // returns number of files with errors
        private static void RemoveModFiles(ModConfiguration mod)
        {
            foreach (InstalledFile file in mod.InstalledFiles)
            {
                string outputPath = GetOutputPath(file.PathType, mod.Name);

                if (outputPath != null)
                {
                    string outputFile = Path.Combine(outputPath, file.Name);
                    string outputFile2 = Path.Combine(outputPath, "mLibs", file.Name);
                    if (File.Exists(outputFile))
                        File.Delete(outputFile);
                    if (File.Exists(outputFile2)) //if ((file.Name.Contains("-disk") || file.Name.Contains("-steam") || file.Name.Contains("-steam_patched")))
                        File.Delete(outputFile2);
                }
            }
            
            string modConfigPath = Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString(), "ModConfigs", mod.Name);
            if (Directory.Exists(modConfigPath))
                Directory.Delete(modConfigPath, true);
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

        public static void ExecuteConfigurator(ModConfiguration mod, bool uninstall)
        {
            if (mod.ConfiguratorPath.ToLowerInvariant().EndsWith("xml"))
            {
                string path = Path.Combine(
                        Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).ToString()
                        , "Spore ModAPI Easy Installer.exe");
                string args = "\"" + mod.Name + "\"" + " true " + uninstall.ToString();
                //MessageBox.Show(path + "\n\n" + args, "process information");
                var process = Process.Start(path, args);
                process.WaitForExit();
            }
            else
            {
                if (!File.Exists(mod.ConfiguratorPath))
                {
                    throw new Exception(Strings.ConfiguratorDoesNotExist);
                }

                var process = Process.Start(new ProcessStartInfo()
                {
                    UseShellExecute = false,  // we need this to execute a temp file
                    FileName = mod.ConfiguratorPath,
                    Arguments = // we use null so they don't throw a warning (the configurator might not need them)
                    ConvertToArgument(mod.Name) + " " +
                    ConvertToArgument(GetOutputPath("None", null)) + " " +
                    ConvertToArgument(GetOutputPath("GalacticAdventures", null)) + " " +
                    ConvertToArgument(GetOutputPath("Spore", null))
                });
            }
        }
    }
}
