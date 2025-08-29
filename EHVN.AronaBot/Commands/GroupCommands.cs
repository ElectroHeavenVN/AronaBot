using EHVN.AronaBot.Config;
using EHVN.AronaBot.Miscellaneous;
using EHVN.ZepLaoSharp;
using EHVN.ZepLaoSharp.Commands;
using EHVN.ZepLaoSharp.Entities;
using EHVN.ZepLaoSharp.FFMpeg;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EHVN.AronaBot.Commands
{
    internal class GroupCommands
    {
        internal static void Register(ZaloClient client)
        {
            var cmd = client.FindOrCreateCommandsExtension(new CommandConfiguration()
            {
                PrefixResolver = new PrefixResolver().ResolvePrefixAsync,
            });
            GroupCheck groupCheck = new EnabledGroupsAndUsersCheck();
            cmd.RegisterCommand(new CommandBuilder()
                .AddCheck(groupCheck)
                .WithCommand("help")
                .WithName("Help")
                .WithDescription("Hiển thị danh sách lệnh thành viên")
                .WithHandler(Help)
            );
            cmd.RegisterCommand(new CommandBuilder("sptf")
                .WithName("Spotify")
                .AddCheck(groupCheck)
                .WithDescription("Download songs from Spotify")
                .AddParameter(new CommandParameterBuilder()
                    .WithName("link")
                    .WithDescription("Link to download")
                    .WithType<string>()
                    .WithDefaultValue("")
                    .TakeRemainingText()
                    .AsOptional()
                    )
                .WithHandler(OnSpotifyDownload));
        }


        static async Task Help(CommandContext ctx)
        {
            string p = ctx.Prefix ?? BotConfig.WritableConfig.Prefix;
            await ctx.Message.AddReactionAsync("/-ok");
            string cmds = "";
            foreach (Command c in ctx.Extension.Commands.Where(c => c.Checks.Any(check => check is GroupCheck)))
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
                Danh sách lệnh thành viên:

                {cmds}
                """
                .Replace("\r", "")
            );
        }

        static async Task OnSpotifyDownload(CommandContext ctx)
        {
            string link = ctx.Arguments[0] as string ?? "";
            if (string.IsNullOrEmpty(link) || (!link.Contains("spotify.com/track/") && !link.Contains("open.spotify.com/track/")))
            {
                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                await ctx.RespondAsync("Link bài hát Spotify không hợp lệ rồi thầy ạ!");
                return;
            }
            await ctx.Message.AddReactionAsync(new ZaloEmoji("⌛", 3490549));
            await ctx.RespondAsync(new ZaloMessageBuilder().WithContent("Thầy đợi em một chút ạ!").DisappearAfter(600000));
            string tempPath = Path.Combine(Path.GetTempPath(), "zotify", Utils.RandomString(10));
            if (!Directory.Exists(tempPath))
                Directory.CreateDirectory(tempPath);
            Process? zotify = Process.Start(new ProcessStartInfo
            {
                FileName = @"Tools\Zotify.exe",
                Arguments = $"--disable-song-archive true --save-credentials false --disable-directory-archives true --root-path \"{tempPath}\" --download-lyrics false --codec mp3 --output \"{{artist}} - {{song_name}}\" {link}",
                WorkingDirectory = tempPath,
                UseShellExecute = false
            });
            if (zotify is null)
            {
                await ctx.RespondAsync("Không thể khởi động Zotify.");
                return;
            }
            await zotify.WaitForExitAsync();
            if (zotify.ExitCode != 0)
            {
                await ctx.RespondAsync("Zotify đã gặp lỗi khi tải xuống bài hát.");
                return;
            }
            if (new DirectoryInfo(tempPath).GetFiles("*.mp3").Length == 0)
            {
                await ctx.RespondAsync("Không tìm thấy tệp âm thanh sau khi tải xuống.");
                return;
            }
            string filePath = new DirectoryInfo(tempPath).GetFiles("*.mp3").First().FullName;
            await ctx.Thread.SendMessageAsync(new ZaloMessageBuilder()
                .AddAttachment(ZaloAttachment.FromFile(filePath).AsRawFile())
                .AddAttachment(ZaloAttachment.FromFile(filePath).ConvertAudioToM4A())
                );
            await ctx.Message.RemoveAllReactionsAsync();
            await ctx.Message.AddReactionAsync("/-ok");
            Directory.Delete(tempPath, true);
        }
    }
}