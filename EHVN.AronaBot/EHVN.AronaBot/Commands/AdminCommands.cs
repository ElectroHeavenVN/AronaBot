using EHVN.AronaBot.Config;
using EHVN.AronaBot.Miscellaneous;
using EHVN.ZepLaoSharp;
using EHVN.ZepLaoSharp.Commands;
using EHVN.ZepLaoSharp.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EHVN.AronaBot.Commands
{
    internal class AdminCommands
    {
        internal static void Register(CommandsExtension cmd)
        {
            AdminCheck adminCheck = new AdminCheck();
            GroupCheck groupCheck = new GroupCheck();
            cmd.RegisterCommand(new CommandBuilder()
                .AddCheck(adminCheck)
                .WithCommand("admin")
                .WithDescription("Hiển thị danh sách lệnh quản trị viên")
                .WithHandler(ListAdminCommands)
            );
            cmd.RegisterCommand(new CommandBuilder()
                .AddCheck(adminCheck)
                .WithCommand("systeminfo")
                .WithDescription("Hiển thị thông tin hệ thống")
                .WithHandler(Systeminfo)
            );
            cmd.RegisterCommand(new CommandBuilder()
                .AddCheck(adminCheck)
                .WithCommand("admins")
                .WithDescription("Xem danh sách quản trị viên")
                .WithHandler(ListAdmins)
                );
            cmd.RegisterCommand(new CommandBuilder()
                .AddCheck(adminCheck)
                .WithCommand("ping")
                .WithDescription("Kiểm tra độ trễ")
                .WithHandler(Ping)
            );
            cmd.RegisterCommand(new CommandBuilder()
                .AddCheck(adminCheck)
                .AddCheck(groupCheck)
                .WithCommand("enable")
                .WithDescription("Kích hoạt lệnh thành viên trong nhóm hiện tại")
                .WithHandler(EnableCommandsCurrentGroup)
            );
            cmd.RegisterCommand(new CommandBuilder()
                .AddCheck(adminCheck)
                .AddCheck(groupCheck)
                .WithCommand("disable")
                .WithDescription("Huỷ kích hoạt lệnh thành viên trong nhóm hiện tại")
                .WithHandler(DisableCommandsCurrentGroup)
            );
            cmd.RegisterCommand(new CommandBuilder()
                .AddCheck(adminCheck)
                .AddCheck(groupCheck)
                .WithCommand("ai-enable")
                .WithDescription("Kích hoạt AI trong nhóm hiện tại")
                .WithHandler(EnableAICurrentGroup)
            );
            cmd.RegisterCommand(new CommandBuilder()
                .AddCheck(adminCheck)
                .AddCheck(groupCheck)
                .WithCommand("ai-disable")
                .WithDescription("Hủy kích hoạt AI trong nhóm hiện tại")
                .WithHandler(DisableAICurrentGroup)
            );
            cmd.RegisterCommand(new CommandBuilder()
                .AddCheck(adminCheck)
                .AddCheck(groupCheck)
                .WithCommand("game-add")
                .WithDescription("Gửi thông báo trong nhóm hiện tại")
                .WithHandler(AddCurrentGroupGameNotif)
            );
            cmd.RegisterCommand(new CommandBuilder()
                .AddCheck(adminCheck)
                .AddCheck(groupCheck)
                .WithCommand("game-remove")
                .WithDescription("Không gửi thông báo trong nhóm hiện tại")
                .WithHandler(RemoveCurrentGroupGameNotif)
            );
            cmd.RegisterCommand(new CommandBuilder()
                .AddCheck(adminCheck)
                .AddCheck(groupCheck)
                .WithCommand("disconnect")
                .WithDescription("Ngắt kết nối")
                .WithHandler(Disconnect)
            );
            cmd.RegisterCommand(new CommandBuilder()
                .AddCheck(adminCheck)
                .WithCommand("getlink")
                .AddAlias("gl")
                .WithDescription("Lấy link đính kèm từ tin nhắn được nhắc đến")
                .WithHandler(GetLink)
            );
            cmd.RegisterCommand(new CommandBuilder()
                .AddCheck(adminCheck)
                .AddCheck(groupCheck)
                .WithCommand("call")
                .WithDescription("Nháy máy gọi")
                .AddParameter(new CommandParameterBuilder()
                    .WithName("member")
                    .WithDescription("Member")
                    .WithType<ZaloMember?>()
                    .WithDefaultValue(null)
                    .AsOptional())
                .AddParameter(new CommandParameterBuilder()
                    .WithName("count")
                    .WithDescription("Count")
                    .WithType<int>()
                    .WithDefaultValue(1)
                    .AsOptional()
                ).WithHandler(DirectCall)
            );
            cmd.RegisterCommand(new CommandBuilder()
                .AddCheck(adminCheck)
                .AddCheck(groupCheck)
                .WithCommand("grcall")
                .WithDescription("Nháy máy gọi nhóm")
                .AddParameter(new CommandParameterBuilder()
                    .WithName("member")
                    .WithDescription("Member")
                    .WithType<ZaloMember?>()
                    .WithDefaultValue(null)
                    .AsOptional())
                .AddParameter(new CommandParameterBuilder()
                    .WithName("count")
                    .WithDescription("Count")
                    .WithType<int>()
                    .WithDefaultValue(1)
                    .AsOptional()
                ).WithHandler(GroupCall)
            );
        }

        static async Task GetLink(CommandContext ctx)
        {
            if (ctx.Message.Quote is null)
            {
                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                await ctx.RespondAsync("Thầy vui lòng nhắc đến tin nhắn có link đính kèm để sử dụng lệnh này ạ!", TimeSpan.FromMinutes(10));
                return;
            }
            string link = "";
            if (ctx.Message.Quote.Content is ZaloEmbeddedLinkContent linkContent)
            {
                link = linkContent.URL;
            }
            else if (ctx.Message.Quote.Content is ZaloImageContent imgContent)
            {
                if (ctx.Message.Quote.Content is ZaloStickerImageContent stickerContent)
                {
                    link = stickerContent.WebpUrl;
                }
                if (string.IsNullOrEmpty(link))
                    link = imgContent.HDUrl;
                if (string.IsNullOrEmpty(link))
                    link = imgContent.SmallUrl;
                if (string.IsNullOrEmpty(link))
                    link = imgContent.ImageUrl;
                if (string.IsNullOrEmpty(link))
                    link = imgContent.ThumbnailUrl;
            }
            else if (ctx.Message.Quote.Content is ZaloVideoContent videoContent)
            {
                link = videoContent.VideoUrl;
            }
            else if (ctx.Message.Quote.Content is ZaloVoiceContent voiceContent)
            {
                link = voiceContent.VoiceUrl;
            }
            else if (ctx.Message.Quote.Content is ZaloContactCardContent contactCardContent)
            {
                link = contactCardContent.QRCodeUrl;
            }
            else if (ctx.Message.Quote.Content is ZaloFileContent fileContent)
            {
                link = fileContent.Url;
            }
            if (string.IsNullOrEmpty(link))
            {
                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                await ctx.RespondAsync("Tin nhắn được nhắc đến không có đính kèm thầy ạ!", TimeSpan.FromMinutes(15));
                return;
            }
            await ctx.Message.AddReactionAsync("/-ok");
            await ctx.RespondAsync("Link: " + link, TimeSpan.FromMinutes(15));
        }

        private static async Task Disconnect(CommandContext ctx)
        {
            await ctx.Message.AddReactionAsync("/-ok");
            await ctx.Client.DisconnectAsync();
        }

        private static async Task AddCurrentGroupGameNotif(CommandContext ctx)
        {
            if (!BotConfig.WritableConfig.DBONotifGroupIDs.Contains(ctx.Thread.ThreadID))
            {
                await ctx.Message.AddReactionAsync("/-ok");
                BotConfig.WritableConfig.DBONotifGroupIDs.Add(ctx.Thread.ThreadID);
                BotConfig.Save();
            }
            else
            {
                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                await ctx.RespondAsync("Nhóm hiện tại đã có trong danh sách nhận thông báo rồi thầy ạ!", TimeSpan.FromMinutes(15));
            }
        }

        private static async Task RemoveCurrentGroupGameNotif(CommandContext ctx)
        {
            if (BotConfig.WritableConfig.DBONotifGroupIDs.Contains(ctx.Thread.ThreadID))
            {
                await ctx.Message.AddReactionAsync("/-ok");
                BotConfig.WritableConfig.DBONotifGroupIDs.Remove(ctx.Thread.ThreadID);
                BotConfig.Save();
            }
            else
            {
                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                await ctx.RespondAsync("Nhóm hiện tại chưa có trong danh sách nhận thông báo rồi thầy ạ!", TimeSpan.FromMinutes(15));
            }
        }

        private static async Task EnableCommandsCurrentGroup(CommandContext ctx)
        {
            if (!BotConfig.WritableConfig.CommandEnabledGroupIDs.Contains(ctx.Thread.ThreadID))
            {
                await ctx.Message.AddReactionAsync("/-ok");
                BotConfig.WritableConfig.CommandEnabledGroupIDs.Add(ctx.Thread.ThreadID);
                BotConfig.Save();
            }
            else
            {
                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                await ctx.RespondAsync("Nhóm hiện tại đã được kích hoạt lệnh thành viên rồi thầy ạ!", TimeSpan.FromMinutes(15));
            }
        }

        private static async Task DisableCommandsCurrentGroup(CommandContext ctx)
        {
            if (BotConfig.WritableConfig.CommandEnabledGroupIDs.Contains(ctx.Thread.ThreadID))
            {
                await ctx.Message.AddReactionAsync("/-ok");
                BotConfig.WritableConfig.CommandEnabledGroupIDs.Remove(ctx.Thread.ThreadID);
                BotConfig.Save();
            }
            else
            {
                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                await ctx.RespondAsync("Nhóm hiện tại chưa được kích hoạt lệnh thành viên thầy ạ!", TimeSpan.FromMinutes(15));
            }
        }

        private static async Task EnableAICurrentGroup(CommandContext ctx)
        {
            if (ctx.Group!.Type == ZaloGroupType.Community)
            {
                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                await ctx.RespondAsync("Chức năng AI không khả dụng trong nhóm cộng đồng thầy ạ!", TimeSpan.FromMinutes(15));
                return;
            }
            if (!BotConfig.WritableConfig.CharacterAIEnabledGroupIDs.Contains(ctx.Thread.ThreadID))
            {
                await ctx.Message.AddReactionAsync("/-ok");
                BotConfig.WritableConfig.CharacterAIEnabledGroupIDs.Add(ctx.Thread.ThreadID);
                BotConfig.Save();
            }
            else
            {
                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                await ctx.RespondAsync("Nhóm hiện tại đã được kích hoạt AI rồi thầy ạ!", TimeSpan.FromMinutes(15));
            }
        }

        private static async Task DisableAICurrentGroup(CommandContext ctx)
        {
            if (BotConfig.WritableConfig.CharacterAIEnabledGroupIDs.Contains(ctx.Thread.ThreadID))
            {
                await ctx.Message.AddReactionAsync("/-ok");
                BotConfig.WritableConfig.CharacterAIEnabledGroupIDs.Remove(ctx.Thread.ThreadID);
                BotConfig.Save();
            }
            else
            {
                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                await ctx.RespondAsync("Nhóm hiện tại chưa được kích hoạt AI thầy ạ!", TimeSpan.FromMinutes(15));
            }
        }

        static async Task ListAdminCommands(CommandContext ctx)
        {
            string p = ctx.Prefix ?? BotConfig.WritableConfig.Prefix;
            await ctx.Message.AddReactionAsync("/-ok");
            string cmds = "";
            foreach (Command c in ctx.Extension.Commands.Where(c => c.Checks.Any(check => check is AdminCheck)))
            {
                //string cmdName = c.Name ?? "";
                string command = c.TextCommand;
                string description = c.Description ?? "";
                //cmds += cmdName + '\n' + description + "\nLệnh: " + p + command;
                cmds += p + command;
                string parameters = "";
                foreach (var param in c.Parameters)
                {
                    bool isMention = param.Type == typeof(ZaloMember) || param.Type == typeof(ZaloUser) || param.Type == typeof(ZaloMention);
                    if (param.IsOptional)
                        parameters += " [" + (isMention ? "@" : "") + param.Name + "]";
                    else
                        parameters += " <" + (isMention ? "@" : "") + param.Name + ">";
                }
                if (!string.IsNullOrEmpty(parameters))
                    cmds += parameters;
                cmds += ": " + description;
                //cmds += "\nCác tham số:\n";
                //foreach (var param in c.Parameters)
                //{
                //    string paramName = param.Name ?? "";
                //    string paramDesc = param.Description ?? "";
                //    cmds += $"- {paramName}: {paramDesc}\n";
                //}
                cmds += "\n";
            }
            await ctx.RespondAsync(
                $"""
                Danh sách lệnh quản trị viên:

                {cmds}
                """
                .Replace("\r", "")
            );
        }

        static async Task Systeminfo(CommandContext ctx)
        {
            await ctx.Message.AddReactionAsync("/-ok");
            string systemInfo = Formatter.FontSizeLarge(Formatter.Bold("Thông tin hệ thống:")) + '\n' + SystemInfo.Get();
            string curentProcessInfo = ProcessInfo.Get(Process.GetCurrentProcess());
            systemInfo += '\n' + Formatter.FontSizeLarge(Formatter.Bold("Thông tin tiến trình:")) + '\n' + curentProcessInfo;
            systemInfo += '\n' + Formatter.FontSizeMedium(Formatter.Bold("Phiên bản thư viện:\n")) + "ZepLaoSharp v" + ZaloClient.VersionString;
            await ctx.RespondAsync(systemInfo);
        }

        static async Task Ping(CommandContext ctx)
        {
            await ctx.Message.AddReactionAsync("/-ok");
            long delay = (long)(DateTime.UtcNow - ctx.Message.Timestamp).TotalMilliseconds;
            await ctx.RespondAsync($"Pong!\nDelay: {delay}ms");
        }

        static async Task DirectCall(CommandContext ctx)
        {
            ZaloMember? member = null;
            if (ctx.Arguments.Count > 0)
                member = (ZaloMember?)ctx.Arguments[0];
            if (member is null || member.IsCurrent)
            {
                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                return;
            }
            int count = (int)ctx.Arguments[1]!;
            if (count <= 0 || count > 20)
            {
                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                return;
            }
            await ctx.Message.AddReactionAsync("/-ok");
            for (int i = 0; i < count; i++)
            {
                try
                {
                    await member.SendMissedCallAsync(i % 2 == 0 ? ZaloCallRequestType.Voice : ZaloCallRequestType.Video);
                    await Task.Delay(2000);
                }
                catch (Exception ex)
                {
                    await ctx.RespondAsync("Lỗi khi gọi: " + ex.Message);
                    return;
                }
            }
            await ctx.RespondAsync($"Em đã nháy máy {member.Mention} {count} lần rồi thầy ạ!");
        }

        static async Task GroupCall(CommandContext ctx)
        {
            static async Task SendMissedCallAsync(ZaloClient client, ZaloGroup group, List<long> memberIDs, ZaloCallRequestType callType = ZaloCallRequestType.Video, CancellationToken cancellationToken = default)
            {
                int callID = Random.Shared.Next(900000000, 2000000000);
                var info = await client!.APIClient.RequestGroupCallAsync(group.ID, "", group.AvatarLinkFull, memberIDs.ToArray(), callID, Math.Max(memberIDs.Count, 8), callType, cancellationToken);
                await client.APIClient.BroadcastGroupCallAsync(info.HostCallID, callID, 1, 0, 1, cancellationToken);
                var server = info.Params?.CallSetting.Servers[Random.Shared.Next(info.Params?.CallSetting.Servers.Count ?? 0)];
                string sessionID = info.Params?.CallSetting.SessionID ?? "";
                await client.APIClient.RequestGroupCallAsync(group.ID, "", group.AvatarLinkFull, memberIDs.ToArray(), info.HostCallID, server?.RtcpAddressV4 ?? "", server?.RtpAddressV4 ?? "", server?.RtcpAddressV6 ?? "", server?.RtpAddressV6 ?? "", sessionID, callID, Math.Max(memberIDs.Count, 8), cancellationToken: cancellationToken);
                await client.APIClient.SendGroupCallPingAsync(info.HostCallID, callID, 2, 0, 0, cancellationToken);
                await Task.Delay(5000, cancellationToken);
                await client.APIClient.CancelGroupCallAsync(group.ID, info.HostCallID, callID, cancellationToken);
                await client.APIClient.FinishGroupCallAsync(server?.RtpAddressV4 ?? "", sessionID, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), memberIDs.ToArray(), info.HostCallID, callID, cancellationToken);
            }

            ZaloMember? member = null;
            if (ctx.Arguments.Count > 0)
                member = (ZaloMember?)ctx.Arguments[0];
            if (member is null || member.IsCurrent)
            {
                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                return;
            }
            int count = (int)ctx.Arguments[1]!;
            if (count <= 0 || count > 20)
            {
                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                return;
            }
            await ctx.Message.AddReactionAsync("/-ok");
            for (int i = 0; i < count; i++)
            {
                try
                {
                    //await ctx.Group.SendMissedCallAsync([member]);
                    await SendMissedCallAsync(ctx.Client, ctx.Group!, [member.ID]);
                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    await ctx.RespondAsync("Lỗi khi gọi: " + ex.Message);
                    return;
                }
            }
            await ctx.RespondAsync($"Em đã nháy máy gọi nhóm với {member.Mention} {count} lần rồi thầy ạ!");
        }

        static async Task ListAdmins(CommandContext ctx)
        {
            await ctx.Message.AddReactionAsync("/-ok");
            string content = "Danh sách quản trị viên:\n";
            await ctx.Client.GetUsersAsync(BotConfig.GetAllAdminIDs().ToArray(), []);
            foreach (long id in BotConfig.GetAllAdminIDs())
            {
                ZaloUser user = await ctx.Client.GetUserAsync(id);
                if (user is not null)
                    content += user.DisplayName + " - " + id + "\n";
            }
            await ctx.RespondAsync(content.Trim('\n'));
        }
    }
}
