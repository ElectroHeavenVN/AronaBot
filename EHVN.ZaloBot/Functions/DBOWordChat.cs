using EHVN.DataNRO.Interfaces;
using EHVN.DataNRO.TeaMobi;
using EHVN.ZaloBot.Config;
using EHVN.ZepLaoSharp;
using EHVN.ZepLaoSharp.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EHVN.ZaloBot.Functions
{
    internal static class DBOWordChat
    {
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        static readonly string[] BLACKLIST = [
            "Đã tiêu diệt được", 
            "180 phút",
            "vừa xuất hiện tại",
            ];

        static readonly string[] INCLUDES = [
            "|5|[HT]"
            ];

        const long TTL = 1000 * 60 * 30; //30 minutes

        internal static void Initialize()
        {
            //register events
            var session = new TeaMobiSession("dragon1.teamobi.com", 14445);
            session.MessageReceiver.EventListeners.ServerChatReceived += async (name, msg) =>
            {
                //if (BLACKLIST.Any(x => msg.Contains(x)))
                //{
                //    Console.WriteLine($"[{session.Host}:{session.Port}] Chat ignored:\r\n" + msg);
                //    return;
                //}
                if (!INCLUDES.Any(x => msg.Contains(x)))
                {
                    Console.WriteLine($"[{session.Host}:{session.Port}] Chat ignored:\r\n" + msg);
                    return;
                }
                await SendMessageToGroupsAsync(name, msg);
            };
            session.MessageReceiver.EventListeners.ServerNotificationReceived += async (msg) =>
            {
                //if (BLACKLIST.Any(x => msg.Contains(x)))
                //{
                //    Console.WriteLine($"[{session.Host}:{session.Port}] Notification ignored:\r\n" + msg);
                //    return;
                //}
                if (!INCLUDES.Any(x => msg.Contains(x)))
                {
                    Console.WriteLine($"[{session.Host}:{session.Port}] Notification ignored:\r\n" + msg);
                    return;
                }
                await SendMessageToGroupsAsync("", msg);
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

        static async Task LoginAndKeepAliveAsync(ISession session)
        {
            await session.ConnectAsync();
            IMessageWriter writer = session.MessageWriter;
            writer.SetClientType();
            await Task.Delay(2000);
            writer.ImageSource();
            await Task.Delay(1000);
            writer.Login(BotConfig.ReadonlyConfig.NROAccount, BotConfig.ReadonlyConfig.NROPassword, 0);
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
            await semaphoreSlim.WaitAsync();
            msg = msg.Replace("|5|[HT] ", "");
            try
            {
                List<ZaloGroup> groups = await Program.client.GetGroupsAsync(BotConfig.WritableConfig.EnabledGroupIDs.ToArray());
                if (groups.Count == 0)
                    return;
                ZaloGroup group = groups.FirstOrDefault()!;
                if (!string.IsNullOrEmpty(name))
                    msg = $"{Formatter.FontSizeLarge(Formatter.Bold(Formatter.ColorGreen(name)))}\n{Formatter.ColorYellow(msg)}";
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
