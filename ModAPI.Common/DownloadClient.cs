using System;
using System.IO;
using System.Net.Http;
using ModAPI.Common.Update;

namespace ModAPI.Common
{
    public class DownloadClient : IDisposable
    {
        private HttpClient httpClient = new HttpClient();
        private HttpRequestMessage httpRequestMessage = new HttpRequestMessage();
        private static readonly string httpUserAgent = "Spore-ModAPI-Launcher-Kit/" + UpdateManager.CurrentVersion.ToString();

        public delegate void DownloadClientEventHandler(object source, int percentage);
        public event DownloadClientEventHandler DownloadProgressChanged = null;

        private void copyStreamWithProgress(Stream inputStrem, Stream outputStream)
        {
            long streamLength = inputStrem.Length;
            byte[] buffer = new byte[4096];
            long totalBytesRead = 0;
            int bytesRead;
            int percentageDownloaded = 0;
            int percentage;

            while ((bytesRead = inputStrem.Read(buffer, 0, buffer.Length)) > 0)
            {
                outputStream.Write(buffer, 0, bytesRead);
                totalBytesRead += bytesRead;

                // only trigger event when percentage has changed
                percentage = (int)((double)totalBytesRead / (double)streamLength * 100.0);
                if (percentageDownloaded != percentage)
                {
                    percentageDownloaded = percentage;
                    DownloadProgressChanged?.Invoke(this, percentage);
                }
            }
        }

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

        public string DownloadToString()
        {
            var response = httpClient.SendAsync(httpRequestMessage).Result;

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Received unsuccessful status code: {(int)response.StatusCode} {response.StatusCode}");
            }

            return response.Content.ReadAsStringAsync().Result;
        }

        public void DownloadToFile(string file)
        {
            var response = httpClient.SendAsync(httpRequestMessage).Result;

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Received unsuccessful status code: {(int)response.StatusCode} {response.StatusCode}");
            }

            using (var downloadStream = response.Content.ReadAsStreamAsync().Result)
            using (var fileStream = new FileStream(file, FileMode.Create))
            {
                copyStreamWithProgress(downloadStream, fileStream);
            }
        }

        public MemoryStream DownloadToMemory()
        {
            var response = httpClient.SendAsync(httpRequestMessage).Result;

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Received unsuccessful status code: {(int)response.StatusCode} {response.StatusCode}");
            }

            MemoryStream memoryStream;
            using (var downloadStream = response.Content.ReadAsStreamAsync().Result)
            {
                memoryStream = new MemoryStream((int)downloadStream.Length);
                copyStreamWithProgress(downloadStream, memoryStream);
            }

            return memoryStream;
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
                DownloadProgressChanged = null;
            }
        }
    }
}
