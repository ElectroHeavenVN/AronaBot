using EHVN.AronaBot.Config;
using EHVN.AronaBot.Miscellaneous;
using EHVN.ZepLaoSharp;
using EHVN.ZepLaoSharp.Commands;
using EHVN.ZepLaoSharp.Entities;
using EHVN.ZepLaoSharp.FFMpeg;
using GTranslate.Translators;
using SoundCloudExplode;
using SoundCloudExplode.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
        static HttpClient httpClient = new HttpClient(new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.All
        });
        static SemaphoreSlim cmdSemaphore = new SemaphoreSlim(1, 1);
        static SoundCloudClient? scClient;
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
                .WithName("Zing MP3")
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
            cmd.RegisterCommand(new CommandBuilder("tts")
                .WithName("TextToSpeech")
                .AddCheck(groupCheck)
                .WithDescription("Convert text to speech")
                .AddParameter(new CommandParameterBuilder()
                    .WithName("text")
                    .WithDescription("Text to convert")
                    .WithType<string>()
                    .TakeRemainingText()
                    )
                .WithHandler(OnTTS));

            cmd.RegisterCommand(new CommandBuilder()
                .AddCheck(groupCheck)
                .WithCommand("stk")
                .WithName("Make Sticker")
                .WithDescription("Tạo sticker từ ảnh hoặc video")
                .WithHandler(MakeSticker)
            );
        }

        static async Task Cooldown()
        {
            await Task.Delay(10000);
            cmdSemaphore.Release();
        }

        static async Task Help(CommandContext ctx)
        {
            if (!cmdSemaphore.Wait(0))
                return;
            try
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
            finally
            {
                await Cooldown();
            }
        }

        static async Task OnYouTubeDownload(CommandContext ctx)
        {
            if (!cmdSemaphore.Wait(0))
                return;
            try
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
                    string fileName = "yt-dlp.exe";
                    if (!Utils.CanExecuteDirectly(fileName))
                        fileName = @"Tools\yt-dlp\yt-dlp.exe";
                    Process? yt_dlp = Process.Start(new ProcessStartInfo
                    {
                        FileName = fileName,
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
            finally
            {
                await Cooldown();
            }
        }

        static async Task OnSpotifyDownload(CommandContext ctx)
        {
            if (!cmdSemaphore.Wait(0))
                return;
            try
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
                    string fileName = "Zotify.exe";
                    if (!Utils.CanExecuteDirectly(fileName))
                        fileName = @"Tools\zotify\Zotify.exe";
                    Process? zotify = Process.Start(new ProcessStartInfo
                    {
                        FileName = fileName,
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
            finally
            {
                await Cooldown();
            }
        }

        static async Task OnSoundCloudDownload(CommandContext ctx)
        {
            if (!cmdSemaphore.Wait(0))
                return;
            try
            {
                if (scClient is null)
                {
                    scClient = new SoundCloudClient(BotConfig.ReadonlyConfig.SoundCloudClientID, httpClient);
                }
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
                    MemoryStream memStream = new MemoryStream();
                    using (Stream nStream = await httpClient.GetStreamAsync(downloadLink))
                    {
                        await nStream.CopyToAsync(memStream);
                    }
                    memStream.Position = 0;
                    MemoryStream memStream2 = new MemoryStream();
                    await memStream.CopyToAsync(memStream2);
                    memStream2.Position = 0;
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
            finally
            {
                await Cooldown();
            }
        }

        static async Task OnZingMP3Download(CommandContext ctx)
        {
            if (!cmdSemaphore.Wait(0))
                return;
            try
            {
                if (zClient is null)
                {
                    zClient = new ZingMP3Client(httpClient);
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
                        MemoryStream memStream = new MemoryStream();
                        MemoryStream memStream2 = new MemoryStream();
                        using (Stream nStream = await httpClient.GetStreamAsync(downloadLink))
                        {
                            await nStream.CopyToAsync(memStream);
                        }
                        memStream.Position = 0;
                        await memStream.CopyToAsync(memStream2);
                        memStream2.Position = 0;
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
                        string fileName = "yt-dlp.exe";
                        if (!Utils.CanExecuteDirectly(fileName))
                            fileName = @"Tools\yt-dlp\yt-dlp.exe";
                        Process? yt_dlp = Process.Start(new ProcessStartInfo
                        {
                            FileName = fileName,
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
            finally
            {
                await Cooldown();
            }
        }

        static async Task OnTTS(CommandContext ctx)
        {
            if (!cmdSemaphore.Wait(0))
                return;
            try
            {
                GoogleTranslator2 translator = new GoogleTranslator2(httpClient);
                string text = (string)(ctx.Arguments[0] ?? "");
                if (text.Length > 500)
                {
                    await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                    await ctx.RespondAsync("Văn bản quá dài, thầy vui lòng giới hạn trong 500 ký tự ạ!");
                    return;
                }
                await ctx.Message.RemoveAllReactionsAsync();
                await ctx.Message.AddReactionAsync("/-ok");
                var lang = await translator.DetectLanguageAsync(text);
                MemoryStream audioStream = new MemoryStream();
                using (Stream stream = await translator.TextToSpeechAsync(text, lang.ISO6391))
                {
                    await stream.CopyToAsync(audioStream);
                }
                audioStream.Position = 0;
                using ZaloAttachment voice = ZaloAttachment.FromData("tts.m4a", audioStream).ConvertAudioToM4A();
                await ctx.Thread.SendMessageAsync(voice);
            }
            finally
            {
                await Cooldown();
            }
        }

        static async Task MakeSticker(CommandContext ctx)
        {
            if (!cmdSemaphore.Wait(0))
                return;
            try
            {
                string contentUrl = "";
                string thumbnailUrl = "";
                long fileSize = 0;
                int width = 0, height = 0;
                TimeSpan videoDuration = TimeSpan.Zero;
                bool isVideo = false;
                bool isFile = false;
                string ext = "";
                bool isGIF = false;
                IZaloMessageContent? content = ctx.Message.Content;
                if (ctx.Message.Quote?.Content is not null)
                    content = ctx.Message.Quote.Content;

                if (content is ZaloImageContent imgContent)
                {
                    if (imgContent.Action == "recommend.gif")
                        isGIF = true;
                    thumbnailUrl = imgContent.ThumbnailUrl;
                    width = imgContent.Width;
                    height = imgContent.Height;
                    fileSize = imgContent.FileSize;
                    contentUrl = imgContent.HDUrl;
                    if (string.IsNullOrEmpty(contentUrl))
                        contentUrl = imgContent.ImageUrl;
                    if (string.IsNullOrEmpty(contentUrl))
                        contentUrl = imgContent.SmallUrl;
                }
                else if (content is ZaloVideoContent videoContent)
                {
                    thumbnailUrl = videoContent.ThumbnailUrl;
                    width = videoContent.Width;
                    height = videoContent.Height;
                    contentUrl = videoContent.VideoUrl;
                    videoDuration = videoContent.Duration;
                    fileSize = videoContent.FileSize;
                    isVideo = true;
                }
                else if (content is ZaloFileContent fileContent && fileContent.FileType == ZaloFileType.File)
                {
                    isFile = true;
                    contentUrl = fileContent.Url;
                    fileSize = fileContent.FileSize;
                    ext = fileContent.FileExtension;
                    if (fileContent.FileExtension == "mp4")
                        isVideo = true;
                    if (fileContent.FileExtension == "gif")
                        isGIF = true;
                }
                if (isVideo && !isFile && videoDuration.TotalSeconds > 10 && !BotConfig.GetAllAdminIDs().Contains(ctx.User.ID))
                {
                    await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                    await ctx.Message.ReplyAsync("Chỉ có quản trị viên mới có quyền tạo sticker từ video dài hơn 10 giây!");
                    return;
                }
                if (isFile)
                {
                    await ctx.Message.AddReactionAsync(new ZaloEmoji("⌛", 3490549));
                    _ = Task.Run(async () =>
                    {
                        using MemoryStream memStream = new MemoryStream();
                        using (Stream content = await httpClient.GetStreamAsync(contentUrl))
                        {
                            await content.CopyToAsync(memStream);
                        }
                        memStream.Position = 0;
                        if (isVideo && (!Utils.TryGetVideoMetadata(ext, memStream, out width, out height, out long duration) || duration > 10000) && !BotConfig.GetAllAdminIDs().Contains(ctx.User.ID))
                        {
                            await ctx.Message.RemoveAllReactionsAsync();
                            await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                            await ctx.Message.ReplyAsync("Chỉ có quản trị viên mới có quyền tạo sticker từ video dài hơn 10 giây!");
                            return;
                        }
                        else if (isVideo)
                        {
                            using ZaloAttachment attachment = AttachmentUtils.FromVideoData(memStream).ConvertVideoToWebp().AsSticker();
                            //await ctx.Thread.SendMessageAsync(attachment);
                            Dictionary<string, string> dict = await ctx.Client.APIClient.UploadPhotoOriginalAsync(attachment.FileName, attachment.DataStream, ctx.Client.MyCloud.ThreadID, ctx.Client.MyCloud.ThreadType);
                            await ctx.Client.APIClient.SendWebpStickerImageMessageAsync(Path.ChangeExtension(dict["org"], "webp?Author=アロナちゃん&Library=ZepLaoSharp_by_ElectroHeavenVN"), Path.ChangeExtension(dict["thumb"], "webp?Author=アロナちゃん&Library=ZepLaoSharp_by_ElectroHeavenVN"), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 500, 500, ZaloStickerImageType.Unknown, 0, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), ctx.Thread.ThreadID, ctx.Thread.ThreadType);
                            await ctx.Message.RemoveAllReactionsAsync();
                            await ctx.Message.AddReactionAsync("/-ok");
                            return;
                        }
                        else if (!Utils.IsImage(memStream))
                        {
                            await ctx.Message.RemoveAllReactionsAsync();
                            await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                            await ctx.Message.ReplyAsync("Ảnh không hợp lệ!");
                            return;
                        }
                        else
                        {
                            using ZaloAttachment attachment = ZaloAttachment.FromData("image.png", memStream).AsSticker();
                            if (!isGIF)
                                await ctx.Thread.SendMessageAsync(attachment);
                            else
                            {
                                attachment.ConvertGIFToWebp();
                                Dictionary<string, string> dict = await ctx.Client.APIClient.UploadPhotoOriginalAsync(attachment.FileName, attachment.DataStream, ctx.Client.MyCloud.ThreadID, ctx.Client.MyCloud.ThreadType);
                                await ctx.Client.APIClient.SendWebpStickerImageMessageAsync(Path.ChangeExtension(dict["org"], "webp?Author=アロナちゃん&Library=ZepLaoSharp_by_ElectroHeavenVN"), Path.ChangeExtension(dict["thumb"], "webp?Author=アロナちゃん&Library=ZepLaoSharp_by_ElectroHeavenVN"), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 500, 500, ZaloStickerImageType.AISticker, 0, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), ctx.Thread.ThreadID, ctx.Thread.ThreadType);
                            }
                            await ctx.Message.RemoveAllReactionsAsync();
                            await ctx.Message.AddReactionAsync("/-ok");
                        }
                    }).ConfigureAwait(false);
                }
                else
                {
                    if (string.IsNullOrEmpty(contentUrl) || string.IsNullOrEmpty(thumbnailUrl))
                    {
                        await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                        await ctx.Message.ReplyAsync("Ảnh không hợp lệ hoặc không có ảnh nào được đính kèm!");
                        return;
                    }
                    if (!isVideo)
                    {
                        await ctx.Message.AddReactionAsync(new ZaloEmoji("⌛", 3490549));
                        if (!isGIF)
                        {
                            using ZaloAttachment attachment = ZaloAttachment.FromUrl(Path.ChangeExtension(contentUrl, Path.GetExtension(contentUrl) + "?Author=アロナちゃん&Library=ZepLaoSharp_by_ElectroHeavenVN"), "image.png", fileSize, Encoding.ASCII.GetString(MD5.HashData([])), Path.ChangeExtension(thumbnailUrl, Path.GetExtension(thumbnailUrl) + "?Author=アロナちゃん&Library=ZepLaoSharp_by_ElectroHeavenVN"), width, height).AsSticker();
                            await ctx.Thread.SendMessageAsync(attachment);
                        }
                        else
                        {
                            MemoryStream memoryStream = new MemoryStream();
                            using (Stream imgStream = await httpClient.GetStreamAsync(contentUrl))
                            {
                                await imgStream.CopyToAsync(memoryStream);
                            }
                            memoryStream.Position = 0;
                            if (!Utils.IsImage(memoryStream))
                            {
                                await ctx.Message.RemoveAllReactionsAsync();
                                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                                await ctx.Message.ReplyAsync("Ảnh không hợp lệ!");
                                return;
                            }
                            using ZaloAttachment attachment = ZaloAttachment.FromData("image." + (isGIF ? "gif" : "png"), memoryStream).AsSticker().WithStickerType(ZaloStickerImageType.AISticker).ConvertGIFToWebp();
                            Dictionary<string, string> dict = await ctx.Client.APIClient.UploadPhotoOriginalAsync(attachment.FileName, attachment.DataStream, ctx.Client.MyCloud.ThreadID, ctx.Client.MyCloud.ThreadType);
                            await ctx.Client.APIClient.SendWebpStickerImageMessageAsync(Path.ChangeExtension(dict["org"], "webp?Author=アロナちゃん&Library=ZepLaoSharp_by_ElectroHeavenVN"), Path.ChangeExtension(dict["thumb"], "webp?Author=アロナちゃん&Library=ZepLaoSharp_by_ElectroHeavenVN"), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 500, 500, ZaloStickerImageType.AISticker, 0, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), ctx.Thread.ThreadID, ctx.Thread.ThreadType);
                        }
                        await ctx.Message.RemoveAllReactionsAsync();
                        await ctx.Message.AddReactionAsync("/-ok");
                    }
                    else
                    {
                        await ctx.Message.AddReactionAsync(new ZaloEmoji("⌛", 3490549));
                        _ = Task.Run(async () =>
                        {
                            using MemoryStream memStream = new MemoryStream();
                            using (Stream videoStream = await httpClient.GetStreamAsync(contentUrl))
                            {
                                await videoStream.CopyToAsync(memStream);
                            }
                            memStream.Position = 0;
                            using ZaloAttachment attachment = AttachmentUtils.FromVideoData(memStream).ConvertVideoToWebp().AsSticker();
                            //await ctx.Thread.SendMessageAsync(attachment);
                            Dictionary<string, string> dict = await ctx.Client.APIClient.UploadPhotoOriginalAsync(attachment.FileName, attachment.DataStream, ctx.Client.MyCloud.ThreadID, ctx.Client.MyCloud.ThreadType);
                            await ctx.Client.APIClient.SendWebpStickerImageMessageAsync(Path.ChangeExtension(dict["org"], "webp?Author=アロナちゃん&Library=ZepLaoSharp_by_ElectroHeavenVN"), Path.ChangeExtension(dict["thumb"], "webp?Author=アロナちゃん&Library=ZepLaoSharp_by_ElectroHeavenVN"), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 500, 500, ZaloStickerImageType.Unknown, 0, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), ctx.Thread.ThreadID, ctx.Thread.ThreadType);
                            await ctx.Message.RemoveAllReactionsAsync();
                            await ctx.Message.AddReactionAsync("/-ok");
                        }).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                await Cooldown();
            }
        }
    }
}