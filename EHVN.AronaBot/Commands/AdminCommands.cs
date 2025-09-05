using EHVN.AronaBot.Config;
using EHVN.AronaBot.Miscellaneous;
using EHVN.ZepLaoSharp;
using EHVN.ZepLaoSharp.Commands;
using EHVN.ZepLaoSharp.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHVN.AronaBot.Commands
{
    internal class AdminCommands
    {
        internal static void Register(ZaloClient client)
        {
            var cmd = client.FindOrCreateCommandsExtension(new CommandConfiguration()
            {
                PrefixResolver = new PrefixResolver().ResolvePrefixAsync,
            });
            AdminCheck adminCheck = new AdminCheck();
            GroupCheck groupCheck = new GroupCheck();
            cmd.RegisterCommand(new CommandBuilder()
                .AddCheck(adminCheck)
                .WithCommand("admin")
                .WithName("Admin commands list")
                .WithDescription("Hiển thị danh sách lệnh quản trị viên")
                .WithHandler(ListAdminCommands)
            ); 
            cmd.RegisterCommand(new CommandBuilder()
                .AddCheck(adminCheck)
                .WithCommand("systeminfo")
                .WithName("System information")
                .WithDescription("Hiển thị thông tin hệ thống")
                .WithHandler(Systeminfo)
            );
            cmd.RegisterCommand(new CommandBuilder()
                .AddCheck(adminCheck)
                .WithCommand("admins")
                .WithName("Admin list")
                .WithDescription("Xem danh sách quản trị viên")
                .WithHandler(ListAdmins)
                );
            cmd.RegisterCommand(new CommandBuilder()
                .AddCheck(adminCheck)
                .WithCommand("ping")
                .WithName("Ping check")
                .WithDescription("Kiểm tra độ trễ")
                .WithHandler(Ping)
            );
            cmd.RegisterCommand(new CommandBuilder()
                .AddCheck(adminCheck)
                .AddCheck(groupCheck)
                .WithCommand("active")
                .WithName("Active current group")
                .WithDescription("Kích hoạt nhóm hiện tại để sử dụng lệnh thành viên")
                .WithHandler(ActivateCurrentGroup)
            );
            cmd.RegisterCommand(new CommandBuilder()
                .AddCheck(adminCheck)
                .AddCheck(groupCheck)
                .WithCommand("deactive")
                .WithName("Deactive current group")
                .WithDescription("Hủy kích hoạt nhóm hiện tại để sử dụng lệnh thành viên")
                .WithHandler(DeactivateCurrentGroup)
            );
            cmd.RegisterCommand(new CommandBuilder()
                .AddCheck(adminCheck)
                .AddCheck(groupCheck)
                .WithCommand("game-add")
                .WithName("Send game notification in current group")
                .WithDescription("Gửi thông báo game trong nhóm hiện tại")
                .WithHandler(AddCurrentGroupGameNotif)
            );
            cmd.RegisterCommand(new CommandBuilder()
                .AddCheck(adminCheck)
                .AddCheck(groupCheck)
                .WithCommand("game-remove")
                .WithName("Don't send game notification in current group")
                .WithDescription("Không gửi thông báo game trong nhóm hiện tại")
                .WithHandler(RemoveCurrentGroupGameNotif)
            );
        }

        private static async Task AddCurrentGroupGameNotif(CommandContext ctx)
        {
            if (!BotConfig.WritableConfig.DBONotifGroupIDs.Contains(ctx.Thread.ThreadID))
            {
                await ctx.Message.AddReactionAsync("/-ok");
                BotConfig.WritableConfig.DBONotifGroupIDs.Add(ctx.Thread.ThreadID);
                BotConfig.Save();
                await ctx.RespondAsync("Nhóm hiện tại đã được thêm vào danh sách nhận thông báo game.");
            }
            else
            {
                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                await ctx.RespondAsync("Nhóm hiện tại đã có trong danh sách nhận thông báo game.");
            }
        }

        private static async Task RemoveCurrentGroupGameNotif(CommandContext ctx)
        {
            if (BotConfig.WritableConfig.DBONotifGroupIDs.Contains(ctx.Thread.ThreadID))
            {
                await ctx.Message.AddReactionAsync("/-ok");
                BotConfig.WritableConfig.DBONotifGroupIDs.Remove(ctx.Thread.ThreadID);
                BotConfig.Save();
                await ctx.RespondAsync("Nhóm hiện tại sẽ không được nhận thông báo game nữa.");
            }
            else
            {
                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                await ctx.RespondAsync("Nhóm hiện tại không có trong danh sách nhận thông báo game.");
            }
        }

        private static async Task ActivateCurrentGroup(CommandContext ctx)
        {
            if (!BotConfig.WritableConfig.EnabledGroupIDs.Contains(ctx.Thread.ThreadID))
            {
                await ctx.Message.AddReactionAsync("/-ok");
                BotConfig.WritableConfig.EnabledGroupIDs.Add(ctx.Thread.ThreadID);
                BotConfig.Save();
                await ctx.RespondAsync("Nhóm hiện tại đã được kích hoạt quyền sử dụng lệnh thành viên.");
            }
            else
            {
                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                await ctx.RespondAsync("Nhóm hiện tại đã được kích hoạt trước đó.");
            }
        }

        private static async Task DeactivateCurrentGroup(CommandContext ctx)
        {
            if (BotConfig.WritableConfig.EnabledGroupIDs.Contains(ctx.Thread.ThreadID))
            {
                await ctx.Message.AddReactionAsync("/-ok");
                BotConfig.WritableConfig.EnabledGroupIDs.Remove(ctx.Thread.ThreadID);
                BotConfig.Save();
                await ctx.RespondAsync("Nhóm hiện tại đã bị hủy kích hoạt quyền sử dụng lệnh thành viên.");
            }
            else
            {
                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                await ctx.RespondAsync("Nhóm hiện tại không được kích hoạt trước đó.");
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
