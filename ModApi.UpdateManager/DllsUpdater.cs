using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ModApi.UpdateManager
{
    internal class DllsUpdater
    {
        private static readonly string GITHUB_API_URL = "https://api.github.com";

        public class GithubReleaseAsset
        {
            public string name;
            public string browser_download_url;
        }

        public class GithubRelease
        {
            public string tag_name;
            public string html_url;
            public GithubReleaseAsset[] assets;
        }

        private static string GithubRequestGET(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Method = "GET";
            request.Accept = "application/vnd.github.v3+json";
            request.UserAgent = UpdateManager.HttpUserAgent;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        private static GithubRelease GetLatestGithubRelease(string repoUser, string repoName)
        {
            string data = GithubRequestGET(GITHUB_API_URL + "/repos/" + repoUser + "/" + repoName + "/releases/latest");

            var errors = new List<string>();
            var settings = new JsonSerializerSettings();
            settings.Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
            {
                errors.Add(args.ErrorContext.Error.Message);
                args.ErrorContext.Handled = true;
            };
            if (errors.Any())
            {
                Console.Error.WriteLine("Found " + errors.Count + " errors while parsing JSON for " + repoUser + "/" + repoName);
                foreach (var error in errors)
                {
                    Console.Error.WriteLine(error);
                }
            }
            return JsonConvert.DeserializeObject<GithubRelease>(data);
        }

        /// <summary>
        /// Parses a version from Github, which is something like "v1.3.3" or "1.3.3"
        /// </summary>
        /// <returns></returns>
        private static Version ParseGithubVersion(string str)
        {
            if (str.StartsWith("v")) return new Version(str.Substring(1));
            else return new Version(str);
        }

        /// <summary>
        /// Returns whether there is an update available for the core ModAPI DLLs.
        /// </summary>
        /// <returns></returns>
        public static bool HasDllsUpdate(out GithubRelease release)
        {
            // Necessary to stablish SSL connection with Github API
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            release = GetLatestGithubRelease("emd4600", "Spore-ModAPI");
            var updateVersion = ParseGithubVersion(release.tag_name);

            return updateVersion > UpdateManager.CurrentDllsBuild;
        }

        static readonly string[] DLL_NAMES = { "SporeModAPI.disk.dll", "SporeModAPI.march2017.dll", "SporeModAPI.lib" };

        public class UpdateProgressEventArgs : EventArgs
        {
            public float Progress { get; set; }
        }

        /// <summary>
        /// How much of the progress is spent on download (the rest on copying the files)
        /// </summary>
        static readonly float DOWNLOAD_PROGRESS = 0.6f;

        /// <summary>
        /// Downloads the update from the given Github release and applies it, extracting all the necessary files.
        /// A progress handler can be passed to react when the operation progress (in the range [0, 100]) changes.
        /// Throws an InvalidOperationException if the update is not valid.
        /// </summary>
        /// <param name="release"></param>
        /// <param name="progressHandler"></param>
        public static void UpdateDlls(GithubRelease release, Action<int> progressHandler)
        {
            var asset = Array.Find(release.assets, a => a.name.ToLower() == "sporemodapidlls.zip");
            if (asset == null)
            {
                throw new InvalidOperationException("Invalid update: no 'SporeModAPIdlls.zip' asset");
            }
            using (var client = new WebClient())
            {
                client.Headers.Add("User-Agent", UpdateManager.HttpUserAgent);
                client.DownloadProgressChanged += (s, e) =>
                {
                    if (progressHandler != null) 
                        progressHandler((int)(e.ProgressPercentage * DOWNLOAD_PROGRESS));
                };

                string zipName = Path.GetTempFileName();
                client.DownloadFile(asset.browser_download_url, zipName);

                using (var zip = ZipFile.Open(zipName, ZipArchiveMode.Read))
                {
                    int filesExtracted = 0;
                    foreach (string name in DLL_NAMES)
                    {
                        var entry = zip.Entries.FirstOrDefault(x => x.Name == name);
                        if (entry == null)
                        {
                            throw new InvalidOperationException("Invalid update: missing " + name + " in zip file");
                        }
                        string coreLibsPath = Path.Combine(Directory.GetParent(Assembly.GetEntryAssembly().Location).ToString(), "coreLibs");
                        string outPath = Path.Combine(coreLibsPath, name);
                        entry.ExtractToFile(outPath, true);
                        GrantAccessFile(outPath);
                        ++filesExtracted;

                        double progress = DOWNLOAD_PROGRESS + filesExtracted * (1.0f - DOWNLOAD_PROGRESS) / (float)(DLL_NAMES.Length);
                        if (progressHandler != null) 
                            progressHandler((int)(progress * 100.0));
                    }
                }

                File.Delete(zipName);
            }
        }

        /// <summary>
		/// If the current application is running as Administrator, attempts to grant full access to a specific file.
		/// </summary>
		/// <param name="filePath"></param>
		/// <returns></returns>
		private static bool GrantAccessFile(string filePath)
        {
            if (IsAdministrator() && File.Exists(filePath))
            {
                //var security = File.GetAccessControl(filePath);
                var sec = new FileSecurity(filePath, AccessControlSections.All);
                sec.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                                                             FileSystemRights.FullControl, InheritanceFlags.None,
                                                             PropagationFlags.NoPropagateInherit, AccessControlType.Allow));

                return true;
            }
            return false;
        }

        private static bool IsAdministrator()
        {
            if (Environment.OSVersion.Version.Major <= 5)
                return true;
            else
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
    }
}
