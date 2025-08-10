using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModAPI_Installers
{
    public enum GameVersionType
    {
        /// <summary>3.0.0.2818 (July 2009) installed from disc, with patch 5.1</summary>
        Disc,
        /// <summary>3.0.0.2818 (July 2009) installed from Origin, with patch 5.1, requires ModAPI Fix</summary>
        Origin,
        /// <summary>3.1.0.22 (March 2017) installed from Origin, requires ModAPI Fix</summary>
        Origin_Patched,
        /// <summary>3.1.0.29 (October 2024) installed from EA App or Origin, requires ModAPI Fix</summary>
        EA_Oct24,
        /// <summary>3.0.0.2818 (July 2009) installed from GOG or Steam, with patch 5.1</summary>
        Steam,
        /// <summary>3.1.0.22 (March 2017) installed from GOG or Steam</summary>
        Steam_Patched,
        /// <summary>3.1.0.29 (October 2024) installed from GOG</summary>
        GoG_Oct24,
        /// <summary>3.1.0.29 (October 2024) installed from Steam, has steamstub DRM</summary>
        Steam_Oct24,

        None
    }

    public static class GameVersion
    {
  

        public static int[] ExecutableSizes = { 
                                       /* DISC*/    24909584,
                                       /* ORIGIN */ 31347984,
                                       /* ORIGIN_P */ 24898224,
                                       /* EA_OCT24 */ 24906040,
                                       /* STEAM */  24888320,
                                       /* STEAM_P */24885248,
                                       /* GOG_OCT24 */24895536,
                                       /* STEAM_OCT24 */ 25066744};

        public static string[] VersionNames = { 
                                                  "disk", 
                                                  "steam_patched",  // origin uses the steam_patched one
                                                  "steam_patched",  // origin uses the steam_patched one
                                                  "steam_patched",  // EA app uses the steam_patched one
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
                                                  "SporeApp_ModAPIFix.exe",  // EA app uses a different one
                                                  "SporeApp.exe", 
                                                  "SporeApp.exe", 
                                                  "SporeApp.exe", 
                                                  "SporeApp.exe",
                                                  null
                                              };

        public static bool RequiresModAPIFix(GameVersionType versionType)
        {
            return versionType == GameVersionType.Origin || 
                    versionType == GameVersionType.Origin_Patched ||
                    versionType == GameVersionType.EA_Oct24;
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
            if (type == GameVersionType.Disc)
                return "SporeModAPI.disk.dll";
            else if ((type == GameVersionType.Origin)         || 
                     (type == GameVersionType.Origin_Patched) ||
                     (type == GameVersionType.EA_Oct24)       ||
                     (type == GameVersionType.Steam_Patched)  ||
                     (type == GameVersionType.GoG_Oct24)      ||
                     (type == GameVersionType.Steam_Oct24))
                return "SporeModAPI.march2017.dll";
            else
            {
                System.Windows.Forms.MessageBox.Show("Your current Spore game version is not compatible with this Launcher Kit version. If you downloaded the game from EA App, Steam, or GOG, please update to version 3.1.0.29 to proceed. If you're using a higher version of Spore, please see https://launcherkit.sporecommunity.com/support.", "Unsupported Game Version");
                System.Diagnostics.Process.GetCurrentProcess().Kill();
                return string.Empty;
            }
        }
    }
}
