using System.IO;

namespace ModApi.Common
{
    public class StreamUtils
    {
        public delegate void StreamProgressEventHandler(object source, int percentage);

        public static void CopyStreamWithProgress(Stream inputStrem, Stream outputStream, object eventSource, StreamProgressEventHandler eventHandler)
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
                    eventHandler?.Invoke(eventSource, percentage);
                }
            }
        }
    }
}
