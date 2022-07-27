using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Win32;

using System.IO;

namespace ModAPI_Installers
{
    public static class SporePath
    {
        public enum Game
        {
            None,
            GalacticAdventures,
            Spore,
            CreepyAndCute
        }
        public static string[] RegistryValues = { "InstallLoc", "Install Dir" };

        public static string[] RegistryKeys = { 
                                             "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Electronic Arts\\SPORE_EP1",
                                             "HKEY_LOCAL_MACHINE\\SOFTWARE\\Electronic Arts\\SPORE_EP1"
                                         };

        public static string[] SporeRegistryKeys = { 
                                             "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Electronic Arts\\SPORE",
                                             "HKEY_LOCAL_MACHINE\\SOFTWARE\\Electronic Arts\\SPORE"
                                                    };

        public static string[] CCRegistryKeys = { 
                                             "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Electronic Arts\\SPORE(TM) Creepy & Cute Parts Pack",
                                             "HKEY_LOCAL_MACHINE\\SOFTWARE\\Electronic Arts\\SPORE(TM) Creepy & Cute Parts Pack"
                                         };


        public static string RegistryDataDir = "DataDir";  // Steam/GoG users don't have InstallLoc nor Install Dir


        // Some things for Steam
        public static string[] SteamRegistryKeys = {
                                             "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Valve\\Steam",
                                             "HKEY_LOCAL_MACHINE\\SOFTWARE\\Valve\\Steam"
                                         };

        public static string SteamRegistryValue = "InstallPath";

        public static string SteamAppsKey = @"HKEY_CURRENT_USER\Software\Valve\Steam\Apps\";

        public static int GalacticAdventuresSteamID = 24720;


        // remove "" if necessary
        private static string FixPath(string path)
        {
            if (path.StartsWith("\""))
            {
                return path.Substring(1, path.Length - 2);
            }
            else
            {
                return path;
            }
        }

        public static bool SporeIsInstalledOnSteam()
        {
            object result = Registry.GetValue(SteamAppsKey + GalacticAdventuresSteamID.ToString(), "Installed", 0);
            // returns null if the key does not exist, or default value if the key existed but the value did not
            return result == null ? false : ((int)result == 0 ? false : true);
        }

        public static string GetFromRegistry(Game game)
        {
            if (game == Game.GalacticAdventures)
            {
                return GetFromRegistry(RegistryKeys);
            }
            else if (game == Game.Spore)
            {
                return GetFromRegistry(SporeRegistryKeys);
            }
            else
            {
                return GetFromRegistry(CCRegistryKeys);
            }
        }

        public static string GetFromRegistry(string[] keys)
        {

            string result = null;

            foreach (string key in keys)
            {
                foreach (string value in RegistryValues)
                {
                    result = (string)Registry.GetValue(key, value, null);
                    if (result != null)
                    {

                        return FixPath(result);
                    }
                }
            }

            // not found? try with DataDir; some users only have that one
            foreach (string key in RegistryKeys)
            {
                result = (string)Registry.GetValue(key, RegistryDataDir, null);
                if (result != null)
                {

                    return FixPath(result);
                }
            }

            return null;
        }


        public static string GetFromRegistry(string[] keys, string[] values)
        {
            string result = null;

            foreach (string key in keys)
            {
                foreach (string value in values)
                {
                    result = (string)Registry.GetValue(key, value, null);
                    if (result != null)
                    {

                        return FixPath(result);
                    }
                }
            }

            return null;
        }

        // If a path ends in \\, Directory.GetParent willl return the same folder!!
        public static string GetRealParent(string path)
        {
            if (path.EndsWith("\\"))
            {
                path = Directory.GetParent(path).ToString();
            }

            return Directory.GetParent(path).ToString();
        }

        // This method returns the path to the folder that contains the executable
        public static string MoveToSporebinEP1(string path, bool recursive = true)
        {
            if (!path.EndsWith("\\"))
            {
                path += "\\";
            }

            if (File.Exists(path + "SporeApp.exe"))
            {
                return path;
            }

            if (Directory.Exists(path + "SporebinEP1"))
            {
                return path + "SporebinEP1\\";
            }

            if (recursive)
            {
                // check if the user selected another folder (for example, "Data")
                return MoveToSporebinEP1(GetRealParent(path), false);
            }

            return null;
        }

        // This method returns the path to the folder that contains the executable
        public static string MoveToSporebin(string path, bool recursive = true)
        {
            if (!path.EndsWith("\\"))
            {
                path += "\\";
            }

            if (File.Exists(path + "SporeApp.exe"))
            {
                return path;
            }

            if (Directory.Exists(path + "Sporebin"))
            {
                return path + "Sporebin\\";
            }

            if (recursive)
            {
                // check if the user selected another folder (for example, "Data")
                return MoveToSporebin(GetRealParent(path), false);
            }

            return null;
        }

        public static string MoveToSteam(string path, bool recursive = true)
        {
            if (!path.EndsWith("\\"))
            {
                path += "\\";
            }

            if (File.Exists(path + "Steam.exe"))
            {
                return path;
            }

            if (Directory.Exists(path + "Steam"))
            {
                return path + "Steam\\";
            }

            if (recursive)
            {
                // check if the user selected another folder (for example, "steamapps")
                return MoveToSteam(GetRealParent(path), false);
            }

            return null;
        }

        public static string MoveToData(Game game, string installationDirectory)
        {
            if (game == Game.Spore)
            {
                if (LauncherSettings.ForcedCoreSporeDataPath != null)
                    return LauncherSettings.ForcedCoreSporeDataPath;
                else
                    return Path.Combine(new string[] {installationDirectory, "Data"});
            }
            else if (game == Game.GalacticAdventures)
            {
                if (LauncherSettings.ForcedGalacticAdventuresDataPath != null)
                    return LauncherSettings.ForcedGalacticAdventuresDataPath;
                else
                {
                    // Steam and GoG uses DataEP1
                    string outputPath = Path.Combine(new string[] { installationDirectory, "DataEP1" });
                    if (Directory.Exists(outputPath))
                    {
                        return outputPath;
                    }
                    else
                    {
                        return Path.Combine(new string[] { installationDirectory, "Data" });
                    }
                }
            }
            else
            {
                // Creepy and Cute uses the installation path itself
                return installationDirectory;
            }
        }
    }
}
