using EHVN.AronaBot.Config;
using EHVN.AronaBot.Miscellaneous;
using EHVN.ZepLaoSharp;
using EHVN.ZepLaoSharp.Commands;
using EHVN.ZepLaoSharp.Entities;
using EHVN.ZepLaoSharp.FFMpeg;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EHVN.AronaBot.Commands
{
    internal static partial class GroupCommands
    {
        [GeneratedRegex("^((https?:)?\\/\\/)?((?<type>www|m|music)\\.)?((youtube\\.com|youtu\\.be))(\\/([\\w\\-]+\\?v=|embed\\/|v\\/|shorts\\/)?)(?<id>[\\w\\-]+)(\\S+)?$", RegexOptions.Compiled)]
        private static partial Regex GetRegexMatchYTLink();
        [GeneratedRegex("^(?:(?:(?:spotify:|https?:\\/\\/)[a-z]*\\.?spotify\\.com(?:\\/embed)?\\/track\\/))(.[^\\?\\n]*)(\\?.*)?$", RegexOptions.Compiled)]
        private static partial Regex GetRegexMatchSpotifyLink();

        static bool isDownloading = false;

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
            cmd.RegisterCommand(new CommandBuilder("yt")
                .WithName("YouTube")
                .AddCheck(groupCheck)
                .WithDescription("Download videos from YouTube")
                .AddParameter(new CommandParameterBuilder()
                    .WithName("link")
                    .WithDescription("Link to download")
                    .WithType<string>()
                    .WithDefaultValue("")
                    .TakeRemainingText()
                    .AsOptional()
                    )
                .WithHandler(OnYouTubeDownload));
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

        static async Task OnYouTubeDownload(CommandContext ctx)
        {
            if (isDownloading)
            {
                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                await ctx.RespondAsync("Đang có phiên tải nội dung rồi, thầy vui lòng thử lại sau ạ!", TimeSpan.FromMinutes(1));
                return;
            }
            string link = ctx.Arguments[0] as string ?? "";
            Match match = GetRegexMatchYTLink().Match(link);
            if (!match.Success)
            {
                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                await ctx.RespondAsync("Link video YouTube không hợp lệ rồi thầy ạ!");
                return;
            }
            isDownloading = true;
            await ctx.Message.AddReactionAsync(new ZaloEmoji("⌛", 3490549));
            await ctx.RespondAsync("Thầy đợi em một chút ạ!", TimeSpan.FromMinutes(1));
            string tempPath = Path.Combine(Path.GetTempPath(), "yt-dlp", Utils.RandomString(10));
            if (!Directory.Exists(tempPath))
                Directory.CreateDirectory(tempPath);
            try
            {
                bool isMusic = match.Groups["type"].Value == "music";
                string id = match.Groups["id"].Value;
                string extArg = "-f \"bestvideo+bestaudio\" -S res:1080 --remux-video mp4 --embed-thumbnail --embed-metadata --no-mtime";
                if (isMusic)
                    extArg = "-f bestaudio --embed-thumbnail --embed-metadata --no-mtime --extract-audio --audio-quality 0";
                Process? yt_dlp = Process.Start(new ProcessStartInfo
                {
                    FileName = @"Tools\yt-dlp\yt-dlp.exe",
                    Arguments = $"{extArg} --paths {tempPath} --force-overwrites --match-filter \"duration<1800\" -- {id}",
                    WorkingDirectory = tempPath,
                    UseShellExecute = false
                });
                if (yt_dlp is null)
                {
                    await ctx.Message.RemoveAllReactionsAsync();
                    await ctx.RespondAsync("Không thể khởi động yt-dlp.", TimeSpan.FromMinutes(1));
                    return;
                }
                await yt_dlp.WaitForExitAsync();
                if (yt_dlp.ExitCode != 0)
                {
                    await ctx.Message.RemoveAllReactionsAsync();
                    await ctx.RespondAsync("yt-dlp đã gặp lỗi khi tải xuống video.", TimeSpan.FromMinutes(1));
                    return;
                }
                if (new DirectoryInfo(tempPath).GetFiles().Length == 0)
                {
                    await ctx.Message.RemoveAllReactionsAsync();
                    await ctx.RespondAsync("Không tìm thấy tệp sau khi tải xuống.", TimeSpan.FromMinutes(1));
                    return;
                }
                string filePath = new DirectoryInfo(tempPath).GetFiles().First().FullName;
                if (!filePath.Contains(id))
                {
                    await ctx.Message.RemoveAllReactionsAsync();
                    await ctx.RespondAsync("Không tìm thấy tệp đúng sau khi tải xuống.", TimeSpan.FromMinutes(1));
                    return;
                }
                if (isMusic)
                {
                    string newFilePath = Path.Combine(tempPath, Path.GetFileNameWithoutExtension(filePath) + ".mp3");
                    Process? ffmpeg = Process.Start(new ProcessStartInfo
                    {
                        FileName = Path.Combine(FFMpegUtils.FFMpegPath, "ffmpeg.exe"),
                        Arguments = $"-i \"{filePath}\" -map 0 -id3v2_version 3 -write_id3v1 1 -map_metadata 0:s:a:0 -b:a 256k \"{newFilePath}\" -y",
                        WorkingDirectory = tempPath,
                        UseShellExecute = false,
                    });
                    if (ffmpeg is null)
                    {
                        await ctx.Message.RemoveAllReactionsAsync();
                        await ctx.RespondAsync("Không thể khởi động ffmpeg.", TimeSpan.FromMinutes(1));
                        return;
                    }
                    await ffmpeg.WaitForExitAsync();
                    if (ffmpeg.ExitCode != 0 || !File.Exists(newFilePath))
                    {
                        await ctx.Message.RemoveAllReactionsAsync();
                        await ctx.RespondAsync("ffmpeg đã gặp lỗi khi chuyển đổi tệp âm thanh.", TimeSpan.FromMinutes(1));
                        return;
                    }
                    using ZaloAttachment voice = ZaloAttachment.FromFile(filePath).ConvertAudioToM4A();
                    using ZaloAttachment file = ZaloAttachment.FromFile(newFilePath).AsRawFile();
                    await ctx.Thread.SendMessagesAsync(new ZaloMessageBuilder()
                        .AddAttachment(voice)
                        .AddAttachment(file)
                        );
                    await ctx.Message.RemoveAllReactionsAsync();
                    await ctx.Message.AddReactionAsync("/-ok");
                }
                else
                {
                    using ZaloAttachment video = AttachmentUtils.FromVideoFile(filePath);
                    await ctx.Thread.SendMessagesAsync(new ZaloMessageBuilder()
                        .WithContent(Path.GetFileNameWithoutExtension(filePath).Replace($" [{id}]", ""))
                        .AddAttachment(video)
                        );
                    await ctx.Message.RemoveAllReactionsAsync();
                    await ctx.Message.AddReactionAsync("/-ok");
                }
            }
            finally
            {
                isDownloading = false;
                Directory.Delete(tempPath, true);
            }
        }

        static async Task OnSpotifyDownload(CommandContext ctx)
        {
            if (isDownloading)
            {
                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                await ctx.RespondAsync("Đang có phiên tải nội dung rồi, thầy vui lòng thử lại sau ạ!", TimeSpan.FromMinutes(1));
                return;
            }
            string link = ctx.Arguments[0] as string ?? "";
            Match match = GetRegexMatchSpotifyLink().Match(link);
            if (!match.Success)
            {
                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                await ctx.RespondAsync("Link bài hát Spotify không hợp lệ rồi thầy ạ!");
                return;
            }
            isDownloading = true;
            await ctx.Message.AddReactionAsync(new ZaloEmoji("⌛", 3490549));
            await ctx.RespondAsync("Thầy đợi em một chút ạ!", TimeSpan.FromMinutes(1));
            string tempPath = Path.Combine(Path.GetTempPath(), "zotify", Utils.RandomString(10));
            if (!Directory.Exists(tempPath))
                Directory.CreateDirectory(tempPath);
            try
            {
                Process? zotify = Process.Start(new ProcessStartInfo
                {
                    FileName = @"Tools\zotify\Zotify.exe",
                    Arguments = $"--disable-song-archive true --save-credentials false --disable-directory-archives true --root-path \"{tempPath}\" --download-lyrics false --codec mp3 --output \"{{artist}} - {{song_name}}\" https://open.spotify.com/track/{match.Groups[1].Value}",
                    WorkingDirectory = tempPath,
                    UseShellExecute = false
                });
                if (zotify is null)
                {
                    await ctx.Message.RemoveAllReactionsAsync();
                    await ctx.RespondAsync("Không thể khởi động Zotify.", TimeSpan.FromMinutes(1));
                    return;
                }
                await zotify.WaitForExitAsync();
                if (zotify.ExitCode != 0)
                {
                    await ctx.Message.RemoveAllReactionsAsync();
                    await ctx.RespondAsync("Zotify đã gặp lỗi khi tải xuống bài hát.", TimeSpan.FromMinutes(1));
                    return;
                }
                if (new DirectoryInfo(tempPath).GetFiles("*.mp3").Length == 0)
                {
                    await ctx.Message.RemoveAllReactionsAsync();
                    await ctx.RespondAsync("Không tìm thấy tệp âm thanh sau khi tải xuống.", TimeSpan.FromMinutes(1));
                    return;
                }
                string filePath = new DirectoryInfo(tempPath).GetFiles("*.mp3").First().FullName;
                using ZaloAttachment voice = ZaloAttachment.FromFile(filePath).ConvertAudioToM4A();
                using ZaloAttachment file = ZaloAttachment.FromFile(filePath).AsRawFile();
                await ctx.Thread.SendMessagesAsync(new ZaloMessageBuilder()
                    .AddAttachment(voice)
                    .AddAttachment(file)
                    );
                await ctx.Message.RemoveAllReactionsAsync();
                await ctx.Message.AddReactionAsync("/-ok");
            }
            finally
            {
                Directory.Delete(tempPath, true);
                isDownloading = false;
            }
        }
    }
}