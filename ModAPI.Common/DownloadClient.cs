using ModApi.Common;
using ModAPI.Common.Update;
using System;
using System.IO;
using System.IO.Pipes;
using System.Net.Http;

namespace ModAPI.Common
{
    public class DownloadClient : IDisposable
    {
        private HttpClient httpClient = new HttpClient();
        private HttpRequestMessage httpRequestMessage = new HttpRequestMessage();
        private static readonly string httpUserAgent = "Spore-ModAPI-Launcher-Kit/" + UpdateManager.CurrentVersion.ToString();

        public event StreamUtils.StreamProgressEventHandler DownloadProgressChanged = null;

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
                StreamUtils.CopyStreamWithProgress(downloadStream, fileStream, this, DownloadProgressChanged);
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
                StreamUtils.CopyStreamWithProgress(downloadStream, memoryStream, this, DownloadProgressChanged);
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
