using System.Diagnostics;
using System.IO;
using EHVN.ZaloBot.Config;

namespace EHVN.ZaloBot.Miscellaneous
{
    internal class MyGraphics
    {
        internal static byte[] AddWatermark(string path)
        {
            Process? graphics = Process.Start(new ProcessStartInfo
            {
                FileName = "EHVN.ZaloBot.Graphics.exe",
                Arguments = $"add-watermark \"{path}\" \"{BotConfig.WritableConfig.Prefix}\"",
                RedirectStandardOutput = true,
            });
            if (graphics is null)
                return [];
            MemoryStream memoryStream = new MemoryStream();
            graphics.StandardOutput.BaseStream.CopyTo(memoryStream);
            graphics.WaitForExit();
            if (graphics.ExitCode != 0)
                return [];
            return memoryStream.ToArray();
        }

        internal static byte[] CreateCanvas(string bgUrl, string avatar1Url, string avatar2Url, string[] messages)
        {
            string messagesJoined = "";
            foreach (string message in messages)
                messagesJoined += '"' + message + "\" ";
            Process? graphics = Process.Start(new ProcessStartInfo
            {
                FileName = "EHVN.ZaloBot.Graphics.exe",
                Arguments = $"create-canvas \"{bgUrl}\" \"{avatar1Url}\" \"{avatar2Url}\" {messagesJoined}",
                RedirectStandardOutput = true,
            });
            if (graphics is null)
                return [];
            MemoryStream memoryStream = new MemoryStream();
            graphics.StandardOutput.BaseStream.CopyTo(memoryStream);
            graphics.WaitForExit();
            if (graphics.ExitCode != 0)
                return [];
            return memoryStream.ToArray();
        }
    }
}
