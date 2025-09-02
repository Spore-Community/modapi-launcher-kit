using System;
using System.Collections.Generic;

using System.IO;

using System.Xml;

namespace ModAPI.Common
{
    public static class LauncherSettings
    {
        private static string FileName = "LauncherSettings.config";

        private static Dictionary<string, string> _dictionary = new Dictionary<string, string>();

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
