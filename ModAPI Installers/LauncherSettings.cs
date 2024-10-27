using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

using System.Xml;

namespace ModAPI_Installers
{
    public static class LauncherSettings
    {
        private static string FileName = "LauncherSettings.config";

        private static Dictionary<string, string> _dictionary = new Dictionary<string, string>();

        public static string ForcedCoreSporeDataPath
        {
            get
            {
                string value = null;
                _dictionary.TryGetValue("ForcedCoreSporeDataPath", out value);
                return value;
            }
        }

        /*public static string ForcedSporebinPath
        {
            get
            {
                string value = null;
                _dictionary.TryGetValue("ForcedSporebinPath", out value);
                return value;
            }
        }*/

        public static string ForcedGalacticAdventuresDataPath
        {
            get
            {
                string value = null;
                _dictionary.TryGetValue("ForcedGalacticAdventuresDataPath", out value);
                return value;
            }
        }

        public static string ForcedSporebinEP1Path
        {
            get
            {
                string value = null;
                _dictionary.TryGetValue("ForcedSporebinEP1Path", out value);
                return value;
            }
        }

        public static string ForcedGalacticAdventuresSporeAppPath
        {
            get
            {
                string value = null;
                _dictionary.TryGetValue("ForcedGalacticAdventuresSporeAppPath", out value);
                return value;
            }
        }

        public static string GamePath
        {
            get
            {
                string value = null;
                _dictionary.TryGetValue("GamePath", out value);
                return value;
            }
            set
            {
                _dictionary["GamePath"] = value;
            }
        }

        public static string SporeGamePath
        {
            get
            {
                string value = null;
                _dictionary.TryGetValue("SporeGamePath", out value);
                return value;
            }
            set
            {
                _dictionary["SporeGamePath"] = value;
            }
        }

        public static string SteamPath
        {
            get
            {
                string value = null;
                _dictionary.TryGetValue("SteamPath", out value);
                return value;
            }
            set
            {
                _dictionary["SteamPath"] = value;
            }
        }

        public static bool ForceGamePath
        {
            get
            {
                string value = null;
                bool result = false;
                _dictionary.TryGetValue("ForceGamePath", out value);
                if (value == null) result = false;
                else result = Boolean.TryParse(value, out result);
                return result;
            }
            set
            {
                _dictionary["ForceGamePath"] = value ? "true" : "false";
            }
        }

        public static GameVersionType GameVersion
        {
            get
            {
                string value = null;
                _dictionary.TryGetValue("GameVersion", out value);
                if (value == null)
                {
                    return GameVersionType.None;
                }
                else
                {
                    return (GameVersionType) Enum.Parse(typeof(GameVersionType), value);
                }
            }
            set
            {
                _dictionary["GameVersion"] = value.ToString();
            }
        }

        public static bool ForceOldSteamMethod
        {
            get
            {
                string value;
                _dictionary.TryGetValue("ForceOldSteamMethod", out value);
                if (value == null)
                {
                    return false;
                }
                else
                {
                    return bool.Parse(value);
                }
            }
            set
            {
                _dictionary["ForceOldSteamMethod"] = value.ToString();
            }
        }

        private static string GetConfigPath()
        {
            var programPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            return Path.Combine(new string[] {programPath, FileName});
        }


        public static bool Load()
        {
            return Load(GetConfigPath());
        }

        public static bool Load(string path)
        {
            if (File.Exists(FileName))
            {
                using (var reader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    string xmlIn = reader.ReadToEnd();

                    var document = new XmlDocument();
                    document.LoadXml(xmlIn);

                    foreach (XmlNode node in document.ChildNodes)
                    {
                        if (node.Name == "Settings")
                        {
                            foreach (XmlNode settingNode in node.ChildNodes)
                            {
                                _dictionary[settingNode.Name] = settingNode.InnerText;
                            }
                        }
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public static void Save(string path)
        {
            var document = new XmlDocument();

            var mainNode = document.CreateElement("Settings");
            document.AppendChild(mainNode);

            foreach (KeyValuePair<string, string> entry in _dictionary)
            {
                var node = document.CreateElement(entry.Key);
                node.InnerText = entry.Value;

                mainNode.AppendChild(node);
            }

            document.Save(path);
        }

        public static void Save()
        {
            Save(GetConfigPath());
        }
    }
}
