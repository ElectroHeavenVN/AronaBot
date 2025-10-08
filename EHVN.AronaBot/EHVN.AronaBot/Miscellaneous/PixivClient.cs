using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace EHVN.AronaBot.Miscellaneous
{
    internal static class PixivClient
    {
        internal static bool GetImage(string refreshToken, long id, out string title, out string caption, out List<Stream> imageStreams)
        {
            imageStreams = [];
            title = "";
            caption = "";
            try
            {
                Process? pixivClient = Process.Start(new ProcessStartInfo
                {
                    FileName = "EHVN.AronaBot.PixivClient.exe",
                    Arguments = $"{refreshToken} {id}",
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                });
                if (pixivClient is null)
                    return false;
                BinaryReader binaryReader = new BinaryReader(pixivClient.StandardOutput.BaseStream);
                int imageCount = binaryReader.ReadInt32();
                if (imageCount <= 0)
                    return false;
                for (int i = 0; i < imageCount; i++)
                {
                    long imageLength = binaryReader.ReadInt64();
                    if (imageLength <= 0)
                        return false;
                    MemoryStream imgStream = new MemoryStream();
                    byte[] buffer = new byte[81920];
                    long totalRead = 0;
                    while (totalRead < imageLength)
                    {
                        int toRead = (int)Math.Min(buffer.Length, imageLength - totalRead);
                        int read = binaryReader.Read(buffer, 0, toRead);
                        if (read <= 0)
                            break;
                        imgStream.Write(buffer, 0, read);
                        totalRead += read;
                    }
                    if (totalRead != imageLength)
                        return false;
                    imgStream.Position = 0;
                    imageStreams.Add(imgStream);
                    //File.WriteAllBytes($"pixiv_{id}_{i}.jpg", imgStream.ToArray());
                }
                title = binaryReader.ReadString();
                caption = binaryReader.ReadString();
                pixivClient.WaitForExit();
                return pixivClient.ExitCode == 0 && imageStreams.Count > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
