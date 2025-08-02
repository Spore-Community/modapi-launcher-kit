using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModAPI_Installers
{
    public enum GameVersionType
    {
        Disk,
        Origin,
        Origin_Patched,
        Steam,
        Steam_Patched,
        GoG_Oct24,
        Steam_Oct24,

        None
    }

    public static class GameVersion
    {
  

        public static int[] ExecutableSizes = { 
                                       /* DISK*/    24909584,
                                       /* ORIGIN */ 31347984,
                                       /* ORIGIN_P */ 24898224,
                                       /* STEAM */  24888320,
                                       /* STEAM_P */24885248,
                                       /* GOG_OCT24 */24895536,
                                       /* STEAM_OCT24 */ 25066744};

        public static string[] VersionNames = { 
                                                  "disk", 
                                                  "steam_patched",  // origin uses the steam_patched one
                                                  "steam_patched",  // origin uses the steam_patched one
                                                  "steam", 
                                                  "steam_patched", 
                                                  "steam_patched",  // in GoG executable, addresses did not change in October 2024 update
                                                  "steam_patched",  // addresses did not change in October 2024 update, but the executable was protected with SteamDRM
                                                  null
                                              };

        // Origin users download an alternative executable, so it uses a different name
        public static string[] ExecutableNames = { 
                                                  "SporeApp.exe", 
                                                  "SporeApp_ModAPIFix.exe",  // origin uses a different one
                                                  "SporeApp_ModAPIFix.exe",  // origin uses a different one
                                                  "SporeApp.exe", 
                                                  "SporeApp.exe", 
                                                  "SporeApp.exe", 
                                                  "SporeApp.exe",
                                                  null
                                              };

        public static bool RequiresModAPIFix(GameVersionType versionType)
        {
            return versionType == GameVersionType.Origin || versionType == GameVersionType.Origin_Patched;
        }

        public static GameVersionType DetectVersion(string path)
        {
            if (path == null)
            {
                return GameVersionType.None;
            }
            var length = new System.IO.FileInfo(path).Length;

            for (int i = 0; i < ExecutableSizes.Length; i++)
            {
                if (length == ExecutableSizes[i])
                {
                    return (GameVersionType)i;
                }
            }

            return GameVersionType.None;
        }

        public static string GetNewDLLName(GameVersionType type)
        {
            if (type == GameVersionType.Disk)
                return "SporeModAPI.disk.dll";
            else if ((type == GameVersionType.Origin) || (type == GameVersionType.Origin_Patched) || (type == GameVersionType.Steam_Patched) || (type == GameVersionType.GoG_Oct24) || (type == GameVersionType.Steam_Oct24))
                return "SporeModAPI.march2017.dll";
            else
            {
                System.Windows.Forms.MessageBox.Show("If you're using the Steam version of Spore or the GOG version of Spore, please update to version 3.1.0.22 to proceed. If you're using Origin Spore and you see this message, or if you're already using a higher version of Spore, please inform rob55rod or emd4600 immediately.", "Unsupported Game Version");
                System.Diagnostics.Process.GetCurrentProcess().Kill();
                return string.Empty;
            }
        }
    }
}
