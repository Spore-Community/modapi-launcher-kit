using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModApi.UpdateManager
{
    public class DownloadClient : IDisposable
    {
        private HttpClient httpClient = new HttpClient();
        private HttpRequestMessage httpRequestMessage = new HttpRequestMessage();
        private static string httpUserAgent = "Spore-ModAPI-Launcher-Kit/" + UpdateManager.CurrentVersion.ToString();


        public delegate void DownloadClientEventHandler(object source, int percentage);
        public event DownloadClientEventHandler DownloadProgressChanged;

        public DownloadClient(string url)
        {
            httpClient.Timeout = TimeSpan.FromMinutes(5);
            httpRequestMessage.RequestUri = new Uri(url);
            httpRequestMessage.Headers.Add("User-Agent", httpUserAgent);
        }

        public void SetTimeout(TimeSpan timeout)
        {
            httpClient.Timeout = timeout;
        }

        public void AddHeader(string key, string value)
        {
            httpRequestMessage.Headers.Add(key, value);
        }

        public string DownloadString()
        {
            var response = httpClient.SendAsync(httpRequestMessage).Result;

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Received unsuccessful status code: {(int)response.StatusCode} {response.StatusCode}");
            }

            return response.Content.ReadAsStringAsync().Result;
        }

        public void DownloadFile(string file)
        {
            var response = httpClient.SendAsync(httpRequestMessage).Result;

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Received unsuccessful status code: {(int)response.StatusCode} {response.StatusCode}");
            }

            using (var downloadStream = response.Content.ReadAsStreamAsync().Result)
            using (var fileStream = new FileStream(file, FileMode.Create))
            {
                long streamLength = downloadStream.Length;
                long totalBytesRead = 0;
                byte[] buffer = new byte[1024];
                int bufferLength = 0;
                int percentageDownloaded = 0;
                int percentage = 0;

                while ((bufferLength = downloadStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fileStream.Write(buffer, 0, bufferLength);

                    // only trigger event when percentage has changed
                    percentage = (int)((double)totalBytesRead / (double)streamLength * 100.0);
                    if (percentageDownloaded != percentage)
                    {
                        percentageDownloaded = percentage;
                        DownloadProgressChanged?.Invoke(this, percentage);
                    }

                    totalBytesRead += bufferLength;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                httpClient.Dispose();
                httpRequestMessage.Dispose();

                httpClient = null;
                httpRequestMessage = null;
            }
        }
    }
}
