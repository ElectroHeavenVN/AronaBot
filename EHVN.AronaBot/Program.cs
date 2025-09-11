using EHVN.AronaBot.Commands;
using EHVN.AronaBot.Config;
using EHVN.AronaBot.Functions;
using EHVN.AronaBot.Functions.AI;
using EHVN.ZepLaoSharp;
using EHVN.ZepLaoSharp.Auth;
using EHVN.ZepLaoSharp.Entities;
using EHVN.ZepLaoSharp.Events;
using EHVN.ZepLaoSharp.FFMpeg;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace EHVN.AronaBot
{
    internal class Program
    {
#pragma warning disable CS8618
        internal static ZaloClient client;
#pragma warning restore CS8618
        static ZaloClientBuilder clientBuilder = new ZaloClientBuilder();
        internal static DateTime startTime;
        static Mutex mutex = new Mutex(true, "EHVN.AronaBot");

        static async Task Main(string[] args)
        {
            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                Console.WriteLine("Another instance is already running. Exiting...");
                return;
            }
            if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Zotify")))
                Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Zotify"));
            if (!File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Zotify", "credentials.json")))
            {
                JsonObject obj = new()
                {
                    ["username"] = BotConfig.ReadonlyConfig.SpotifyUsername,
                    ["type"] = "AUTHENTICATION_STORED_SPOTIFY_CREDENTIALS",
                    ["credentials"] = BotConfig.ReadonlyConfig.SpotifyToken,
                };
                File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Zotify", "credentials.json"), obj.ToJsonString());
            }
            //DateTime vietnamTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "SE Asia Standard Time");
            //if (vietnamTime.Hour < 6)
            //    Console.WriteLine("Waiting until 06:00 AM to start...");
            //do
            //{
            //    vietnamTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "SE Asia Standard Time");
            //    if (vietnamTime.Hour >= 6)
            //        break;
            //    Thread.Sleep(1000 * 20);
            //}
            //while (true);

            startTime = DateTime.UtcNow;
            new Thread(CheckRestart).Start();
            InitializeClient();
            FFMpegUtils.ClearCache();
#if !DEBUG
            FFMpegUtils.FFMpegPath = "Tools\\ffmpeg";
#endif
            client = clientBuilder.Build();
            await client.ConnectAsync();
            Console.WriteLine("Logged in as: " + client.CurrentUser.DisplayName);
            AdminCommands.Register(client);
            GroupCommands.Register(client);
            client.EventListeners.Disconnected += EventListeners_Disconnected;
            client.EventListeners.GroupMessageReceived += EventListeners_GroupMessageReceived;
            //DBOWorldChat.Initialize();
            await Task.Delay(Timeout.Infinite);
        }

        static void UpdateYTDlp()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = @"Tools\yt-dlp\yt-dlp.exe",
                Arguments = "-U",
                UseShellExecute = false,
            });
        }

        static async void CheckRestart()
        {
            while (true)
            {
                DateTime vietnamTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "SE Asia Standard Time");
                if (vietnamTime.Minute == 0 && vietnamTime.Hour > 0 && vietnamTime.Hour < 6)
                {
                    UpdateYTDlp();
                    await client.DisconnectAsync();
                    Thread.Sleep(1000 * 60);
                    Process.Start(new ProcessStartInfo(Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "") { UseShellExecute = true });
                    Environment.Exit(0);
                }
                Thread.Sleep(1000 * 20);
            }
        }

        static async Task EventListeners_Disconnected(ZaloClient client, GatewayDisconnectedEventArgs args)
        {
            await Task.Delay(60000);
            await client.ConnectAsync();
        }

        static async Task EventListeners_GroupMessageReceived(ZaloClient sender, GroupMessageReceivedEventArgs args)
        {
            await ChatAI.GroupMessageReceived(sender, args);
        }

        static void InitializeClient()
        {
            JsonNode node = JsonNode.Parse(File.ReadAllText(@"Data\credentials-pc.json")) ?? throw new Exception("Missing credentials-pc.json");
            clientBuilder = clientBuilder
                .WithMaxTimeCache(TimeSpan.FromHours(3))
                .WithCredential(new LoginCredentialBuilder()
                    .UsePCCredential(ZepLaoSharp.Auth.OperatingSystem.Windows)
                    .WithAPIType(node["api_type"]?.GetValue<int>() ?? 0)
                    .WithAPIVersion(node["client_version"]?.GetValue<int>() ?? 0)
                    .WithComputerName(node["computer_name"]?.GetValue<string>() ?? "")
                    .WithIMEI(node["imei"]?.GetValue<string>() ?? "")
                    .WithToken(node["token"]?.GetValue<string>() ?? "")
                    .WithLanguage(Language.Vietnamese)
                    .Build()
                    );
            //ZaloLogger.SetLogger(new DefaultLogger(LogLevel.Trace));
        }
    }
}