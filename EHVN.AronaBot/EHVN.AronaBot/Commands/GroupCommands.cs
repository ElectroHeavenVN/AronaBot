using EHVN.AronaBot.Config;
using EHVN.AronaBot.Miscellaneous;
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
        [GeneratedRegex("^((https?:)?\\/\\/)?((?<type>www|m|music)\\.)?((youtube\\.com|youtu\\.be))(\\/([\\w\\-]+\\?v=|(?<kind>|embed|v|shorts)\\/)?)(?<id>[\\w\\-]+)(\\S+)?$", RegexOptions.Compiled)]
        private static partial Regex GetRegexMatchYTLink();
        [GeneratedRegex("^(((spotify:|https?:\\/\\/)[a-z]*\\.?spotify\\.com(\\/embed)?\\/track\\/))(?<id>.[^\\?\\n]*)(\\?.*)?$", RegexOptions.Compiled)]
        private static partial Regex GetRegexMatchSpotifyLink();
        [GeneratedRegex("^(https?:\\/\\/)?((((m|on)\\.)?soundcloud\\.com)|(snd\\.sc))\\/([\\w-]*)\\/?([\\w-]*)\\??.*$", RegexOptions.Compiled)]
        private static partial Regex GetRegexMatchSoundCloudLink();
        [GeneratedRegex(@"zingmp3\.vn\/(bai-hat|video-clip)\/(.*\/?)(Z[A-Z0-9]{7})\.html", RegexOptions.Compiled)]
        private static partial Regex GetRegexMatchZingMP3Link();
        [GeneratedRegex(@"pixiv\.net\/(.*?\/)?artworks\/(?<id>\d+)", RegexOptions.Compiled)]
        private static partial Regex GetRegexMatchPixivLink();

        static bool isDownloading = false;
        static HttpClient httpClient = new HttpClient(new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.All
        });
        static SemaphoreSlim cmdSemaphore = new SemaphoreSlim(1, 1);
        static SoundCloudClient? scClient;
        static ZingMP3Client? zClient;

        internal static void Register(CommandsExtension cmd)
        {
            GroupCheck groupCheck = new EnabledGroupsAndUsersCheck();
            cmd.RegisterCommand(new CommandBuilder()
                .AddCheck(groupCheck)
                .WithCommand("help")
                .WithDescription("Hiển thị danh sách lệnh thành viên")
                .WithHandler(Help)
            );
            cmd.RegisterCommand(new CommandBuilder("sptf")
                .AddCheck(groupCheck)
                .WithDescription("Tải nhạc từ Spotify")
                .AddAlias("spotify")
                .AddParameter(new CommandParameterBuilder()
                    .WithName("link")
                    .WithDescription("Link to download")
                    .WithType<string>()
                    .WithDefaultValue("")
                    .TakeRemainingText()
                    .AsOptional()
                    )
                .WithHandler(SpotifyDownload));
            cmd.RegisterCommand(new CommandBuilder("yt")
                .AddCheck(groupCheck)
                .WithDescription("Tải video từ YouTube")
                .AddAlias("youtube")
                .AddParameter(new CommandParameterBuilder()
                    .WithName("link")
                    .WithDescription("Link YouTube")
                    .WithType<string>()
                    .WithDefaultValue("")
                    .TakeRemainingText()
                    .AsOptional()
                    )
                .WithHandler(YouTubeDownload));
            cmd.RegisterCommand(new CommandBuilder("scl")
                .AddCheck(groupCheck)
                .WithDescription("Tải nhạc từ SoundCloud")
                .AddAlias("soundcloud")
                .AddParameter(new CommandParameterBuilder()
                    .WithName("link")
                    .WithDescription("Link SoundCloud")
                    .WithType<string>()
                    .WithDefaultValue("")
                    .TakeRemainingText()
                    .AsOptional()
                    )
                .WithHandler(SoundCloudDownload));
            cmd.RegisterCommand(new CommandBuilder("zmp3")
                .AddCheck(groupCheck)
                .WithDescription("Tải nhạc từ Zing MP3")
                .AddAlias("zingmp3")
                .AddParameter(new CommandParameterBuilder()
                    .WithName("link")
                    .WithDescription("Link Zing MP3")
                    .WithType<string>()
                    .WithDefaultValue("")
                    .TakeRemainingText()
                    .AsOptional()
                    )
                .WithHandler(ZingMP3Download));
            cmd.RegisterCommand(new CommandBuilder("px")
                .AddCheck(groupCheck)
                .WithDescription("Tải ảnh từ Pixiv")
                .AddAlias("pixiv")
                .AddParameter(new CommandParameterBuilder()
                    .WithName("linkOrID")
                    .WithDescription("Link hoặc ID Pixiv")
                    .WithType<string>()
                    .WithDefaultValue("")
                    .TakeRemainingText()
                    .AsOptional()
                    )
                .WithHandler(PixivDownload));
            cmd.RegisterCommand(new CommandBuilder("tts")
                .AddCheck(groupCheck)
                .WithDescription("Chuyển văn bản thành giọng nói")
                .AddParameter(new CommandParameterBuilder()
                    .WithName("text")
                    .WithDescription("Văn bản để chuyển đổi")
                    .WithType<string>()
                    .TakeRemainingText()
                    )
                .WithHandler(TTS));
            cmd.RegisterCommand(new CommandBuilder()
                .AddCheck(groupCheck)
                .WithCommand("stk")
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

        static async Task YouTubeDownload(CommandContext ctx)
        {
            if (!cmdSemaphore.Wait(0))
                return;
            try
            {
                if (isDownloading)
                {
                    await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                    await ctx.RespondAsync("Đang có phiên tải nội dung rồi, thầy vui lòng thử lại sau ạ!", TimeSpan.FromMinutes(15));
                    return;
                }
                string link = ctx.Arguments[0] as string ?? "";
                Match match = GetRegexMatchYTLink().Match(link);
                if (!match.Success)
                {
                    await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                    await ctx.RespondAsync("Link video YouTube không hợp lệ rồi thầy ạ!", TimeSpan.FromMinutes(15));
                    return;
                }
                isDownloading = true;
                await ctx.Message.AddReactionAsync(new ZaloEmoji("⌛", 3490549));
                await ctx.Thread.TriggerTypingAsync();
                string tempPath = Path.Combine(Path.GetTempPath(), "yt-dlp", Utils.RandomString(10));
                if (!Directory.Exists(tempPath))
                    Directory.CreateDirectory(tempPath);
                try
                {
                    bool isMusic = match.Groups["type"].Value == "music";
                    bool isShorts = match.Groups["kind"].Value == "shorts";
                    string id = match.Groups["id"].Value;
                    string extArg = "-f \"bv[vcodec^=avc1][ext=mp4]+ba[ext=m4a]/bv+ba\" -S res:1080 --remux-video mp4 --embed-thumbnail --embed-metadata --no-mtime";
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
                        return;
                    }
                    await yt_dlp.WaitForExitAsync();
                    if (yt_dlp.ExitCode != 0)
                    {
                        await ctx.Message.RemoveAllReactionsAsync();
                        return;
                    }
                    if (new DirectoryInfo(tempPath).GetFiles().Length == 0)
                    {
                        await ctx.Message.RemoveAllReactionsAsync();
                        return;
                    }
                    string filePath = new DirectoryInfo(tempPath).GetFiles().First().FullName;
                    if (!filePath.Contains(id))
                    {
                        await ctx.Message.RemoveAllReactionsAsync();
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
                            return;
                        }
                        await ffmpeg.WaitForExitAsync();
                        if (ffmpeg.ExitCode != 0 || !File.Exists(newFilePath))
                        {
                            await ctx.Message.RemoveAllReactionsAsync();
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
                        Stream? thumbnailStream = null;
                        //if (isShorts)
                        //{
                        //    using var stream = await httpClient.GetStreamAsync($"https://i.ytimg.com/vi/{id}/oardefault.jpg");
                        //    thumbnailStream = new MemoryStream();
                        //    await stream.CopyToAsync(thumbnailStream);
                        //    thumbnailStream.Position = 0;
                        //}
                        using ZaloAttachment video = AttachmentUtils.FromVideoFile(filePath, thumbnailStream).WithGroupMediaTitle(Path.GetFileNameWithoutExtension(filePath).Replace($" [{id}]", ""));
                        await ctx.Thread.SendMessagesAsync(new ZaloMessageBuilder()
                            .GroupMediaMessages()
                            //.WithContent(Path.GetFileNameWithoutExtension(filePath).Replace($" [{id}]", ""))
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

        static async Task SpotifyDownload(CommandContext ctx)
        {
            if (!cmdSemaphore.Wait(0))
                return;
            try
            {
                if (isDownloading)
                {
                    await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                    await ctx.RespondAsync("Đang có phiên tải nội dung rồi, thầy vui lòng thử lại sau ạ!", TimeSpan.FromMinutes(15));
                    return;
                }
                string link = ctx.Arguments[0] as string ?? "";
                Match match = GetRegexMatchSpotifyLink().Match(link);
                if (!match.Success)
                {
                    await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                    await ctx.RespondAsync("Link bài hát Spotify không hợp lệ rồi thầy ạ!", TimeSpan.FromMinutes(15));
                    return;
                }
                isDownloading = true;
                await ctx.Message.AddReactionAsync(new ZaloEmoji("⌛", 3490549));
                await ctx.Thread.TriggerTypingAsync();
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
                        return;
                    }
                    await zotify.WaitForExitAsync();
                    if (zotify.ExitCode != 0)
                    {
                        await ctx.Message.RemoveAllReactionsAsync();
                        return;
                    }
                    if (new DirectoryInfo(tempPath).GetFiles("*.mp3").Length == 0)
                    {
                        await ctx.Message.RemoveAllReactionsAsync();
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

        static async Task SoundCloudDownload(CommandContext ctx)
        {
            if (!cmdSemaphore.Wait(0))
                return;
            try
            {
                scClient ??= new SoundCloudClient(BotConfig.ReadonlyConfig.SoundCloudClientID, httpClient);
                if (isDownloading)
                {
                    await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                    await ctx.RespondAsync("Đang có phiên tải nội dung rồi, thầy vui lòng thử lại sau ạ!", TimeSpan.FromMinutes(15));
                    return;
                }
                string link = ctx.Arguments[0] as string ?? "";
                Match match = GetRegexMatchSoundCloudLink().Match(link);
                if (!match.Success || link.Contains("/you/"))
                {
                    await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                    await ctx.RespondAsync("Link nhạc SoundCloud không hợp lệ rồi thầy ạ!", TimeSpan.FromMinutes(15));
                    return;
                }
                isDownloading = true;
                await ctx.Message.AddReactionAsync(new ZaloEmoji("⌛", 3490549));
                await ctx.Thread.TriggerTypingAsync();
                try
                {
                    var track = await scClient.Tracks.GetAsync(link);
                    if (track is null)
                    {
                        await ctx.Message.RemoveAllReactionsAsync();
                        return;
                    }
                    if (track.Duration > 1000 * 60 * 30 || track.FullDuration > 1000 * 60 * 30)
                    {
                        await ctx.Message.RemoveAllReactionsAsync();
                        return;
                    }
                    string title = track.Title ?? "";
                    string artist = track.User?.Username ?? "Unknown Artist";
                    string downloadLink = await scClient.Tracks.GetDownloadUrlAsync(track) ?? "";
                    if (string.IsNullOrEmpty(downloadLink))
                    {
                        await ctx.Message.RemoveAllReactionsAsync();
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

        static async Task ZingMP3Download(CommandContext ctx)
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
                    await ctx.RespondAsync("Đang có phiên tải nội dung rồi, thầy vui lòng thử lại sau ạ!", TimeSpan.FromMinutes(15));
                    return;
                }
                string link = ctx.Arguments[0] as string ?? "";
                Match match = GetRegexMatchZingMP3Link().Match(link);
                if (!match.Success || link.Contains("/you/") || (match.Groups[1].Value != "bai-hat" && match.Groups[1].Value != "video-clip"))
                {
                    await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                    await ctx.RespondAsync("Link nhạc Zing MP3 không hợp lệ rồi thầy ạ!", TimeSpan.FromMinutes(15));
                    return;
                }
                isDownloading = true;
                await ctx.Message.AddReactionAsync(new ZaloEmoji("⌛", 3490549));
                await ctx.Thread.TriggerTypingAsync();
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
                            return;
                        }
                        string title = song.Title;
                        string artist = song.AllArtistsNames;
                        string downloadLink = await zClient.Songs.GetAudioStreamUrlAsync(link);
                        if (string.IsNullOrEmpty(downloadLink))
                        {
                            await ctx.Message.RemoveAllReactionsAsync();
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
                            return;
                        }
                        string title = video.Title;
                        string artist = video.AllArtistsNames;
                        string downloadLink = video.VideoStream.GetBestHLS();
                        if (string.IsNullOrEmpty(downloadLink))
                        {
                            await ctx.Message.RemoveAllReactionsAsync();
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
                            return;
                        }
                        await yt_dlp.WaitForExitAsync();
                        if (yt_dlp.ExitCode != 0)
                        {
                            await ctx.Message.RemoveAllReactionsAsync();
                            return;
                        }
                        if (new DirectoryInfo(tempPath).GetFiles().Length == 0)
                        {
                            await ctx.Message.RemoveAllReactionsAsync();
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

        static async Task TTS(CommandContext ctx)
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
                    await ctx.RespondAsync("Văn bản quá dài, thầy vui lòng giới hạn trong 500 ký tự ạ!", TimeSpan.FromMinutes(15));
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
                const string WATERMARK_SUFFIX = "?Author=アロナちゃん&Library=ZepLaoSharp_by_ElectroHeavenVN";
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
                    return;
                }
                if (isFile)
                {
                    await ctx.Message.AddReactionAsync(new ZaloEmoji("⌛", 3490549));
                    await ctx.Thread.TriggerTypingAsync();
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
                            return;
                        }
                        else if (isVideo)
                        {
                            using ZaloAttachment attachment = AttachmentUtils.FromVideoData(memStream).ConvertVideoToWebp().AsSticker();
                            Dictionary<string, string> dict = await ctx.Client.APIClient.UploadPhotoOriginalAsync(attachment.FileName, attachment.DataStream, ctx.Client.MyCloud.ThreadID, ctx.Client.MyCloud.ThreadType);
                            await ctx.Client.APIClient.SendWebpStickerImageMessageAsync(Path.ChangeExtension(dict["org"], "webp" + WATERMARK_SUFFIX), Path.ChangeExtension(dict["thumb"], "webp" + WATERMARK_SUFFIX), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 500, 500, ZaloStickerImageType.AISticker, 0, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), ctx.Thread.ThreadID, ctx.Thread.ThreadType);
                            await ctx.Message.RemoveAllReactionsAsync();
                            await ctx.Message.AddReactionAsync("/-ok");
                            return;
                        }
                        else if (!Utils.IsImage(memStream))
                        {
                            await ctx.Message.RemoveAllReactionsAsync();
                            await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
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
                                await ctx.Client.APIClient.SendWebpStickerImageMessageAsync(Path.ChangeExtension(dict["org"], "webp" + WATERMARK_SUFFIX), Path.ChangeExtension(dict["thumb"], "webp" + WATERMARK_SUFFIX), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 500, 500, ZaloStickerImageType.AISticker, 0, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), ctx.Thread.ThreadID, ctx.Thread.ThreadType);
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
                        return;
                    }
                    if (!isVideo)
                    {
                        await ctx.Message.AddReactionAsync(new ZaloEmoji("⌛", 3490549));
                        await ctx.Thread.TriggerTypingAsync();
                        if (!isGIF)
                        {
                            using MemoryStream memoryStream = new MemoryStream();
                            using (Stream stream = await httpClient.GetStreamAsync(contentUrl))
                                await stream.CopyToAsync(memoryStream);
                            using ZaloAttachment attachment = ZaloAttachment.FromData("image.png", memoryStream).ConvertImageToWebp().AsSticker().WithStickerType(ZaloStickerImageType.AISticker);
                            Dictionary<string, string> dict = await ctx.Client.APIClient.UploadPhotoOriginalAsync(attachment.FileName, attachment.DataStream, ctx.Client.MyCloud.ThreadID, ctx.Client.MyCloud.ThreadType);
                            await ctx.Client.APIClient.SendWebpStickerImageMessageAsync(Path.ChangeExtension(dict["org"], "webp" + WATERMARK_SUFFIX), Path.ChangeExtension(dict["thumb"], "webp" + WATERMARK_SUFFIX), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 500, 500, ZaloStickerImageType.AISticker, 0, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), ctx.Thread.ThreadID, ctx.Thread.ThreadType);
                        }
                        else
                        {
                            using MemoryStream memoryStream = new MemoryStream();
                            using (Stream imgStream = await httpClient.GetStreamAsync(contentUrl))
                            {
                                await imgStream.CopyToAsync(memoryStream);
                            }
                            memoryStream.Position = 0;
                            if (!Utils.IsImage(memoryStream))
                            {
                                await ctx.Message.RemoveAllReactionsAsync();
                                await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                                return;
                            }
                            using ZaloAttachment attachment = ZaloAttachment.FromData("image." + (isGIF ? "gif" : "png"), memoryStream).AsSticker().WithStickerType(ZaloStickerImageType.AISticker).ConvertGIFToWebp();
                            Dictionary<string, string> dict = await ctx.Client.APIClient.UploadPhotoOriginalAsync(attachment.FileName, attachment.DataStream, ctx.Client.MyCloud.ThreadID, ctx.Client.MyCloud.ThreadType);
                            await ctx.Client.APIClient.SendWebpStickerImageMessageAsync(Path.ChangeExtension(dict["org"], "webp" + WATERMARK_SUFFIX), Path.ChangeExtension(dict["thumb"], "webp" + WATERMARK_SUFFIX), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 500, 500, ZaloStickerImageType.AISticker, 0, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), ctx.Thread.ThreadID, ctx.Thread.ThreadType);
                        }
                        await ctx.Message.RemoveAllReactionsAsync();
                        await ctx.Message.AddReactionAsync("/-ok");
                    }
                    else
                    {
                        await ctx.Message.AddReactionAsync(new ZaloEmoji("⌛", 3490549));
                        await ctx.Thread.TriggerTypingAsync();
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
                            await ctx.Client.APIClient.SendWebpStickerImageMessageAsync(Path.ChangeExtension(dict["org"], "webp" + WATERMARK_SUFFIX), Path.ChangeExtension(dict["thumb"], "webp" + WATERMARK_SUFFIX), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 500, 500, ZaloStickerImageType.AISticker, 0, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), ctx.Thread.ThreadID, ctx.Thread.ThreadType);
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
    
        static async Task PixivDownload(CommandContext ctx)
        {
            if (!cmdSemaphore.Wait(0))
                return;
            try
            {
                if (isDownloading)
                {
                    await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                    await ctx.RespondAsync("Đang có phiên tải nội dung rồi, thầy vui lòng thử lại sau ạ!", TimeSpan.FromMinutes(15));
                    return;
                }
                string linkOrID = ctx.Arguments[0] as string ?? "";
                Match match = GetRegexMatchPixivLink().Match(linkOrID);
                if (match.Success)
                    linkOrID = match.Groups["id"].Value;
                if (!long.TryParse(linkOrID, out long illustID) || illustID <= 0)
                {
                    await ctx.Message.AddReactionAsync(new ZaloEmoji("❌", 4305703));
                    await ctx.RespondAsync("Link hoặc ID Pixiv không hợp lệ rồi thầy ạ!", TimeSpan.FromMinutes(15));
                    return;
                }
                isDownloading = true;
                await ctx.Message.AddReactionAsync(new ZaloEmoji("⌛", 3490549));
                await ctx.Thread.TriggerTypingAsync();
                try
                {
                    if (!PixivClient.GetImage(BotConfig.ReadonlyConfig.PixivRefreshToken, illustID, out string title, out string caption, out List<Stream> imageStreams))
                    {
                        await ctx.Message.RemoveAllReactionsAsync();
                        return;
                    }
                    ZaloMessageBuilder msgBuilder = new ZaloMessageBuilder().GroupMediaMessages();
                    for (int i = 0; i < imageStreams.Count; i++)
                    {
                        Stream imgStream = imageStreams[i];
                        imgStream.Position = 0;
                        msgBuilder.AddAttachment(ZaloAttachment.FromData($"{illustID}-{i + 1}.png", imgStream).AsOriginal().GeneratePreviewThumb().WithGroupMediaTitle(title));
                    }
                    if (msgBuilder.Attachments.Count == 0)
                    {
                        await ctx.Message.RemoveAllReactionsAsync();
                        return;
                    }
                    await ctx.Thread.SendMessagesAsync(msgBuilder);
                    if (!string.IsNullOrEmpty(caption))
                        await ctx.Thread.SendMessageAsync(caption.Replace("<br />", "\n").Replace("\r", ""));
                    await ctx.Message.RemoveAllReactionsAsync();
                    await ctx.Message.AddReactionAsync("/-ok");
                    foreach (var attachment in msgBuilder.Attachments)
                        attachment.Dispose();
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
    }
}