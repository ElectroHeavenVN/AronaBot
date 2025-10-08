using System;
using System.Diagnostics;
using System.IO;

namespace EHVN.AronaBot.Miscellaneous
{
    internal class MyGraphics
    {
        public static Stream CreateCanvas(string bgPath, string bgUrl, string avatar1Url, string avatar2Url, params string[] messages)
        {
            if (bgUrl == "https://cover-talk.zadn.vn/default")
                bgUrl = "";
            string messagesJoined = "";
            foreach (string message in messages)
                messagesJoined += '"' + message + "\" ";
            string args = $"create-canvas \"{bgPath}\" \"{bgUrl}\" \"{avatar1Url}\" \"{avatar2Url}\" {messagesJoined}";
            return ExecuteGraphics(args);
        }

        static Stream ExecuteGraphics(string args)
        {
            string fileName = "EHVN.AronaBot.Graphics.exe";
            for (int i = 0; i < 5; i++)
            {
                Console.WriteLine("Executing: " + fileName + " " + args);
                Process? graphics = Process.Start(new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = args,
                    RedirectStandardOutput = true,
                });
                if (graphics is null)
                    continue;
                MemoryStream memoryStream = new MemoryStream();
                graphics.StandardOutput.BaseStream.CopyTo(memoryStream);
                graphics.WaitForExit();
                if (graphics.ExitCode != 0)
                    continue;
                memoryStream.Position = 0;
                return memoryStream;
            }
            return new MemoryStream();
        }
    }
}
