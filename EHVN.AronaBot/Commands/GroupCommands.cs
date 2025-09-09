using EHVN.AronaBot.Config;
using EHVN.AronaBot.Miscellaneous;
using EHVN.ZepLaoSharp;
using EHVN.ZepLaoSharp.Commands;
using EHVN.ZepLaoSharp.Entities;
using EHVN.ZepLaoSharp.FFMpeg;
using SoundCloudExplode;
using SoundCloudExplode.Exceptions;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ZingMP3Explode;

namespace EHVN.AronaBot.Commands
{
    internal static partial class GroupCommands
    {
        [GeneratedRegex("^((https?:)?\\/\\/)?((?<type>www|m|music)\\.)?((youtube\\.com|youtu\\.be))(\\/([\\w\\-]+\\?v=|embed\\/|v\\/|shorts\\/)?)(?<id>[\\w\\-]+)(\\S+)?$", RegexOptions.Compiled)]
        private static partial Regex GetRegexMatchYTLink();
        [GeneratedRegex("^(((spotify:|https?:\\/\\/)[a-z]*\\.?spotify\\.com(\\/embed)?\\/track\\/))(?<id>.[^\\?\\n]*)(\\?.*)?$", RegexOptions.Compiled)]
        private static partial Regex GetRegexMatchSpotifyLink();
        [GeneratedRegex("^(https?:\\/\\/)?((((m|on)\\.)?soundcloud\\.com)|(snd\\.sc))\\/([\\w-]*)\\/?([\\w-]*)\\??.*$", RegexOptions.Compiled)]
        private static partial Regex GetRegexMatchSoundCloudLink();
        [GeneratedRegex(@"zingmp3\.vn\/(bai-hat|video-clip)\/(.*\/?)(Z[A-Z0-9]{7})\.html", RegexOptions.Compiled)]
        private static partial Regex GetRegexMatchZingMP3Link();

        static bool isDownloading = false;

        static SoundCloudClient scClient = new SoundCloudClient(BotConfig.ReadonlyConfig.SoundCloudClientID);
        static ZingMP3Client? zClient;

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
            cmd.RegisterCommand(new CommandBuilder("scl")
                .WithName("SoundCloud")
                .AddCheck(groupCheck)
                .WithDescription("Download songs from SoundCloud")
                .AddParameter(new CommandParameterBuilder()
                    .WithName("link")
                    .WithDescription("Link to download")
                    .WithType<string>()
                    .WithDefaultValue("")
                    .TakeRemainingText()
                    .AsOptional()
                    )
                .WithHandler(OnSoundCloudDownload));
            cmd.RegisterCommand(new CommandBuilder("zmp3")
                .WithName("SoundCloud")
                .AddCheck(groupCheck)
                .WithDescription("Download songs from Zing MP3")
                .AddParameter(new CommandParameterBuilder()
                    .WithName("link")
                    .WithDescription("Link to download")
                    .WithType<string>()
                    .WithDefaultValue("")
                    .TakeRemainingText()
                    .AsOptional()
                    )
                .WithHandler(OnZingMP3Download));
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
                    using ZaloAttachment video = AttachmentUtils.FromVideoFile(filePath).WithGroupMediaTitle(Path.GetFileNameWithoutExtension(filePath).Replace($" [{id}]", ""));
                    await ctx.Thread.SendMessagesAsync(new ZaloMessageBuilder()
                        .GroupMediaMessages()
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
                    Arguments = $"--disable-song-archive true --save-credentials false --disable-directory-archives true --root-path \"{tempPath}\" --download-lyrics false --codec mp3 --output \"{{artist}} - {{song_name}}\" https://open.spotify.com/track/{match.Groups["id"].Value}",
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

        static async Task OnSoundCloudDownload(CommandContext ctx)
        {
            if (isDownloading)
            {
                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                await ctx.RespondAsync("Đang có phiên tải nội dung rồi, thầy vui lòng thử lại sau ạ!", TimeSpan.FromMinutes(1));
                return;
            }
            string link = ctx.Arguments[0] as string ?? "";
            Match match = GetRegexMatchSoundCloudLink().Match(link);
            if (!match.Success || link.Contains("/you/"))
            {
                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                await ctx.RespondAsync("Link nhạc SoundCloud không hợp lệ rồi thầy ạ!");
                return;
            }
            isDownloading = true;
            await ctx.Message.AddReactionAsync(new ZaloEmoji("⌛", 3490549));
            await ctx.RespondAsync("Thầy đợi em một chút ạ!", TimeSpan.FromMinutes(1));
            try
            {
                var track = await scClient.Tracks.GetAsync(link);
                if (track is null)
                {
                    await ctx.Message.RemoveAllReactionsAsync();
                    await ctx.RespondAsync("Không tìm thấy bài hát trên SoundCloud.", TimeSpan.FromMinutes(1));
                    return;
                }
                if (track.Duration > 1000 * 60 * 30 || track.FullDuration > 1000 * 60 * 30)
                {
                    await ctx.Message.RemoveAllReactionsAsync();
                    await ctx.RespondAsync("Bài hát quá dài để tải xuống.", TimeSpan.FromMinutes(1));
                    return;
                }
                string title = track.Title ?? "";
                string artist = track.User?.Username ?? "Unknown Artist";
                string downloadLink = await scClient.Tracks.GetDownloadUrlAsync(track) ?? "";
                if (string.IsNullOrEmpty(downloadLink))
                {
                    await ctx.Message.RemoveAllReactionsAsync();
                    await ctx.RespondAsync("Không thể tải xuống bài hát.", TimeSpan.FromMinutes(1));
                    return;
                }
                HttpClient httpClient = new HttpClient();
                Stream nStream = await httpClient.GetStreamAsync(downloadLink);
                MemoryStream memStream = new MemoryStream();
                MemoryStream memStream2 = new MemoryStream();
                await nStream.CopyToAsync(memStream);
                memStream.Position = 0;
                await memStream.CopyToAsync(memStream2);
                memStream2.Position = 0;
                nStream.Dispose();
                //memStream(s) should be disposed by ZaloAttachment
                using ZaloAttachment voice = ZaloAttachment.FromData($"{artist} - {title}.mp3", memStream).ConvertAudioToM4A();
                using ZaloAttachment file = ZaloAttachment.FromData($"{artist} - {title}.mp3", memStream2).AsRawFile();
                await ctx.Thread.SendMessagesAsync(new ZaloMessageBuilder()
                    .AddAttachment(voice)
                    .AddAttachment(file)
                    );
                await ctx.Message.RemoveAllReactionsAsync();
                await ctx.Message.AddReactionAsync("/-ok");
            }
            catch (TrackUnavailableException)
            {
                await ctx.Message.RemoveAllReactionsAsync();
                await ctx.RespondAsync("Bài hát không khả dụng để tải xuống.", TimeSpan.FromMinutes(1));
                return;
            }
            finally
            {
                isDownloading = false;
            }
        }

        static async Task OnZingMP3Download(CommandContext ctx)
        {
            if (zClient is null)
            {
                zClient = new ZingMP3Client();
                await zClient.InitializeAsync();
            }
            if (isDownloading)
            {
                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                await ctx.RespondAsync("Đang có phiên tải nội dung rồi, thầy vui lòng thử lại sau ạ!", TimeSpan.FromMinutes(1));
                return;
            }
            string link = ctx.Arguments[0] as string ?? "";
            Match match = GetRegexMatchZingMP3Link().Match(link);
            if (!match.Success || link.Contains("/you/"))
            {
                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                await ctx.RespondAsync("Link nhạc Zing MP3 không hợp lệ rồi thầy ạ!");
                return;
            }
            isDownloading = true;
            await ctx.Message.AddReactionAsync(new ZaloEmoji("⌛", 3490549));
            await ctx.RespondAsync("Thầy đợi em một chút ạ!", TimeSpan.FromMinutes(1));
            string tempPath = Path.Combine(Path.GetTempPath(), "zmp3", Utils.RandomString(10));
            if (!Directory.Exists(tempPath))
                Directory.CreateDirectory(tempPath);
            try
            {
                string type = match.Groups[1].Value;
                string id = match.Groups[3].Value;
                if (type == "bai-hat")
                {
                    var song = await zClient.Songs.GetAsync(link);
                    if (song.Duration > 60 * 30)
                    {
                        await ctx.Message.RemoveAllReactionsAsync();
                        await ctx.RespondAsync("Bài hát quá dài để tải xuống.", TimeSpan.FromMinutes(1));
                        return;
                    }
                    string title = song.Title;
                    string artist = song.AllArtistsNames;
                    string downloadLink = await zClient.Songs.GetAudioStreamUrlAsync(link);
                    if (string.IsNullOrEmpty(downloadLink))
                    {
                        await ctx.Message.RemoveAllReactionsAsync();
                        await ctx.RespondAsync("Không thể tải xuống bài hát.", TimeSpan.FromMinutes(1));
                        return;
                    }
                    HttpClient httpClient = new HttpClient();
                    Stream nStream = await httpClient.GetStreamAsync(downloadLink);
                    MemoryStream memStream = new MemoryStream();
                    MemoryStream memStream2 = new MemoryStream();
                    await nStream.CopyToAsync(memStream);
                    memStream.Position = 0;
                    await memStream.CopyToAsync(memStream2);
                    memStream2.Position = 0;
                    nStream.Dispose();
                    using ZaloAttachment voice = ZaloAttachment.FromData($"{artist} - {title}.mp3", memStream).ConvertAudioToM4A();
                    using ZaloAttachment file = ZaloAttachment.FromData($"{artist} - {title}.mp3", memStream2).AsRawFile();
                    await ctx.Thread.SendMessagesAsync(new ZaloMessageBuilder()
                        .AddAttachment(voice)
                        .AddAttachment(file)
                        );
                    await ctx.Message.RemoveAllReactionsAsync();
                    await ctx.Message.AddReactionAsync("/-ok");
                }
                else if (type == "video-clip")
                {
                    var video = await zClient.Videos.GetAsync(link);
                    if (video.Duration > 60 * 30)
                    {
                        await ctx.Message.RemoveAllReactionsAsync();
                        await ctx.RespondAsync("Video quá dài để tải xuống.", TimeSpan.FromMinutes(1));
                        return;
                    }
                    string title = video.Title;
                    string artist = video.AllArtistsNames;
                    string downloadLink = video.VideoStream.GetBestHLS();
                    if (string.IsNullOrEmpty(downloadLink))
                    {
                        await ctx.Message.RemoveAllReactionsAsync();
                        await ctx.RespondAsync("Không tìm thấy liên kết tải xuống video.", TimeSpan.FromMinutes(1));
                        return;
                    }

                    Process? yt_dlp = Process.Start(new ProcessStartInfo
                    {
                        FileName = @"Tools\yt-dlp\yt-dlp.exe",
                        Arguments = $"--remux-video mp4 --embed-metadata --no-mtime --paths {tempPath} -- {downloadLink}",
                        UseShellExecute = false,
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
                        await ctx.RespondAsync("yt-dlp đã gặp lỗi khi tải xuống MV.", TimeSpan.FromMinutes(1));
                        return;
                    }
                    if (new DirectoryInfo(tempPath).GetFiles().Length == 0)
                    {
                        await ctx.Message.RemoveAllReactionsAsync();
                        await ctx.RespondAsync("Không tìm thấy tệp sau khi tải xuống.", TimeSpan.FromMinutes(1));
                        return;
                    }
                    string filePath = new DirectoryInfo(tempPath).GetFiles().First().FullName;
                    using ZaloAttachment videoAtt = AttachmentUtils.FromVideoFile(filePath).WithGroupMediaTitle($"{video.AllArtistsNames} - {video.Title}");
                    await ctx.Thread.SendMessagesAsync(new ZaloMessageBuilder()
                        .GroupMediaMessages()
                        .AddAttachment(videoAtt)
                        );
                    await ctx.Message.RemoveAllReactionsAsync();
                    await ctx.Message.AddReactionAsync("/-ok");
                }
                else
                {
                    await ctx.Message.RemoveAllReactionsAsync();
                    await ctx.RespondAsync("Link không hợp lệ.", TimeSpan.FromMinutes(1));
                    return;
                }
            }
            finally
            {
                isDownloading = false;
                Directory.Delete(tempPath, true);
            }
        }
    }
}