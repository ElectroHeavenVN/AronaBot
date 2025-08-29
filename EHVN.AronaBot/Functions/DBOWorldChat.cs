using EHVN.DataNRO.Interfaces;
using EHVN.DataNRO.TeaMobi;
using EHVN.AronaBot.Config;
using EHVN.ZepLaoSharp;
using EHVN.ZepLaoSharp.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace EHVN.AronaBot.Functions
{
    internal static partial class DBOWorldChat
    {
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        const long TTL = 1000 * 60 * 30; //30 minutes

        //pessi0calo vừa đánh quái may mắn nhận được 1 trang bị Set kích hoạt
        //bakugou vừa đánh quái may mắn nhận được 1 trang bị Set kích hoạt Set Cađic M
        [GeneratedRegex("^(?:.*?) vừa đánh quái may mắn nhận được 1 trang bị (Set kích hoạt(?: .*)?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        private static partial Regex RegexExtractInfo();

        //bakugou [Super 1]
        [GeneratedRegex("^(.*?) \\[(?:(.*?)(?: sao)?)\\]$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        private static partial Regex RegexExtractServer();

        internal static void Initialize()
        {
            var session = new TeaMobiSession(BotConfig.ReadonlyConfig.DBO.ServerAddress, BotConfig.ReadonlyConfig.DBO.ServerPort);
            session.MessageReceiver.EventListeners.ServerChatReceived += async (name, msg) =>
            {
                if (!msg.StartsWith("|5|[HT] "))
                {
                    Console.WriteLine($"[{session.Host}:{session.Port}] Server chat ignored:\r\n" + msg);
                    return;
                }
                Console.WriteLine($"[{session.Host}:{session.Port}] Sending server chat to groups:\r\n" + msg);
                await SendMessageToGroupsAsync(name, msg.Substring(8));
            };

            session.MessageReceiver.EventListeners.ServerNotificationReceived += (msg) =>
            {
                Console.WriteLine($"[{session.Host}:{session.Port}] Server notification received:\r\n" + msg);
            };
            session.MessageReceiver.EventListeners.DialogMessageReceived += (msg) =>
            {
                Console.WriteLine($"[{session.Host}:{session.Port}] Dialog message received:\r\n" + msg);
            };
            session.MessageReceiver.EventListeners.ServerMessageReceived += (msg) =>
            {
                Console.WriteLine($"[{session.Host}:{session.Port}] Server message received:\r\n" + msg);
            };
            session.MessageReceiver.EventListeners.ServerAlertReceived += (msg) =>
            {
                Console.WriteLine($"[{session.Host}:{session.Port}] Server alert received:\r\n" + msg);
            };
            session.MessageReceiver.EventListeners.GameNotificationReceived += (msg) =>
            {
                Console.WriteLine($"[{session.Host}:{session.Port}] Game notification received:\r\n" + msg);
            };
            session.MessageReceiver.EventListeners.UnknownMessageReceived += (msg) =>
            {
                Console.WriteLine($"[{session.Host}:{session.Port}] Unknown message received:\r\n" + msg);
            };
            _ = LoginAndKeepAliveAsync(session).ConfigureAwait(false);
        }

        //TODO: improve keep-alive logic, handle daily maintenance, disconnections, etc.
        static async Task LoginAndKeepAliveAsync(ISession session)
        {
            await session.ConnectAsync();
            IMessageWriter writer = session.MessageWriter;
            writer.SetClientType();
            await Task.Delay(2000);
            writer.ImageSource();
            await Task.Delay(1000);
            writer.Login(BotConfig.ReadonlyConfig.DBO.Account, BotConfig.ReadonlyConfig.DBO.Password, 0);
            await Task.Delay(3000);
            writer.ClientOk();
            writer.FinishUpdate();
            await Task.Delay(1000);
            writer.FinishLoadMap();
            await Task.Delay(30000);
            while (true)
            {
                writer.RequestChangeZone(session.Player.Location?.zoneId ?? 0);
                await Task.Delay((1000 + Random.Shared.Next(-200, 201)) * 30);
            }
        }

        internal static async Task SendMessageToGroupsAsync(string name, string msg)
        {
            Match match = RegexExtractServer().Match(name);
            if (!match.Success)
                return;
            name = match.Groups[1].Value;
            string server = "Server " + match.Groups[2].Value;
            match = RegexExtractInfo().Match(msg);
            if (!match.Success)
                return;
            string type = match.Groups[1].Value;
            if (type != "Set kích hoạt")
                type = type.Substring(14);

            msg = Formatter.Bold($"Sensei {Formatter.ColorRed(name)} ở {Formatter.ColorYellow(server)} vừa may mắn nhận được 1 trang bị {Formatter.ColorGreen(type)} do liêm khiết!");

            await semaphoreSlim.WaitAsync();
            try
            {
                List<ZaloGroup> groups = await Program.client.GetGroupsAsync(BotConfig.WritableConfig.DBONotifGroupIDs.ToArray());
                if (groups.Count == 0)
                    return;
                ZaloGroup group = groups.FirstOrDefault()!;
                ZaloMessage message = (await group.SendMessageAsync(new ZaloMessageBuilder().WithContent(msg).DisappearAfter(TTL)))[0];
                if (groups.Count == 1)
                    return;
                await Task.Delay(1000);
                await message.ForwardAsync(groups.Skip(1).OfType<ZaloThread>().ToList(), TTL);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message to groups: {ex.Message}");
            }
            finally
            {
                await Task.Delay(1000 * 5);
                semaphoreSlim.Release();
            }
        }
    }
}
