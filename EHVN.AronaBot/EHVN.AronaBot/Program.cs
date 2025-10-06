using EHVN.AronaBot.Commands;
using EHVN.AronaBot.Config;
using EHVN.AronaBot.Functions;
using EHVN.AronaBot.Functions.AI.CharacterAI;
using EHVN.AronaBot.Miscellaneous;
using EHVN.ZepLaoSharp;
using EHVN.ZepLaoSharp.Auth;
using EHVN.ZepLaoSharp.Events;
using EHVN.ZepLaoSharp.FFMpeg;
using EHVN.ZepLaoSharp.Logging;
using EHVN.ZepLaoSharp.Net.LongPolling;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace EHVN.AronaBot
{
    internal static class Program
    {
#pragma warning disable CS8618
        internal static ZaloClient client;
#pragma warning restore CS8618
        static LongPollingClientOptions options = new LongPollingClientOptions();
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
            DateTime vietnamTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "SE Asia Standard Time");
            if (vietnamTime.Hour < 6)
                Console.WriteLine("Waiting until 06:00 AM to start...");
            do
            {
                vietnamTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "SE Asia Standard Time");
                if (vietnamTime.Hour >= 6)
                    break;
                Thread.Sleep(1000 * 20);
            }
            while (true);
            startTime = DateTime.UtcNow;
            new Thread(CheckRestart).Start();
            InitializeClient();
            FFMpegUtils.ClearCache();
            if (!Utils.CanExecuteDirectly("ffmpeg"))
                FFMpegUtils.FFMpegPath = @"Tools\ffmpeg";
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
            try
            {
                string fileName = "yt-dlp.exe";
                if (!Utils.CanExecuteDirectly(fileName))
                    fileName = @"Tools\yt-dlp\yt-dlp.exe";
                Process.Start(new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = "-U",
                    UseShellExecute = false,
                });
            }
            catch { }
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
                    Thread.Sleep(1000 * 30);
                    Process.Start(new ProcessStartInfo(Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "") { UseShellExecute = true });
                    Environment.Exit(0);
                }
                Thread.Sleep(1000 * 20);
            }
        }

        static async Task EventListeners_Disconnected(ZaloClient client, DisconnectedEventArgs args)
        {
            File.WriteAllText(@"Data\lp_options.json", JsonSerializer.Serialize(options, SourceGenerationContext.Default.LongPollingClientOptions));
            await Task.Delay(60000);
            await client.ConnectAsync();
        }

        static async Task EventListeners_GroupMessageReceived(ZaloClient sender, GroupMessageReceivedEventArgs args)
        {
            //if (args.Author.DisplayName == "Nguyen Viet Quan")
            //{
            //    await args.GroupMessage.DeleteAsync();
            //    return;
            //}
            await CAIMain.GroupMessageReceived(sender, args);
        }

        static void InitializeClient()
        {
            try
            {
                options = JsonSerializer.Deserialize(File.ReadAllText(@"Data\lp_options.json"), SourceGenerationContext.Default.LongPollingClientOptions) ?? new LongPollingClientOptions();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to load lp_options.json, using default options. Error: " + ex.Message);
            }
            options.AutoMarkAsDelivered = true;
            options.FilterMode = ZepLaoSharp.Net.RealtimeClientOptions.ThreadFilterMode.Inclusive;
            options.FilterThreadIDs = [8566925697964157802];
            JsonNode node = JsonNode.Parse(File.ReadAllText(@"Data\credentials-pc.json")) ?? throw new Exception("Missing credentials-pc.json");
            clientBuilder = clientBuilder
                //.WithLogger(new DefaultLogger(LogLevel.Trace))
                .WithMaxTimeCache(TimeSpan.FromDays(1))
                .UseLongPolling(options)
                .WithCredential(new LoginCredentialBuilder()
                    .UsePCCredential(ZaloOperatingSystem.Windows)
                    .WithClientType(ZaloClientType.Windows)
                    .WithClientVersion(node["client_version"]?.GetValue<int>() ?? 0)
                    .WithComputerName(node["computer_name"]?.GetValue<string>() ?? "")
                    .WithIMEI(node["imei"]?.GetValue<string>() ?? "")
                    .WithToken(node["token"]?.GetValue<string>() ?? "")
                    .WithLanguage(ZaloLanguage.Vietnamese)
                    .Build()
                );
        }
    }
}