using ImageMagick;
using ImageMagick.Drawing;
using ImageMagick.Formats;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ZepLaoSharp;
using ZepLaoSharp.Auth;
using ZepLaoSharp.Entities;
using ZepLaoSharp.Events;
using ZepLaoSharp.FFMpeg;

namespace ZaloBot
{
    internal class Program
    {
        static readonly int MAX_BA_STICKERS_COUNT = 144;

        static ZaloClientBuilder clientBuilder = ZaloClientBuilder.CreateDefault();
        static ZaloClient client;
        static HttpClient httpClient = new HttpClient();
        static object locker = new object();
        static string prefix = ",";

        static char[] accentChars =
        [
            'à', 'á', 'ả', 'ã', 'ạ',
            'ă', 'ằ', 'ắ', 'ẳ', 'ẵ', 'ặ',
            'â', 'ầ', 'ấ', 'ẩ', 'ẫ', 'ậ',
            'đ', 'è', 'é', 'ẻ', 'ẽ', 'ẹ',
            'ê', 'ề', 'ế', 'ể', 'ễ', 'ệ',
            'ì', 'í', 'ỉ', 'ĩ', 'ị',
            'ò', 'ó', 'ỏ', 'õ', 'ọ',
            'ô', 'ồ', 'ố', 'ổ', 'ỗ', 'ộ',
            'ơ', 'ờ', 'ớ', 'ở', 'ỡ', 'ợ',
            'ù', 'ú', 'ủ', 'ũ', 'ụ',
            'ư', 'ừ', 'ứ', 'ử', 'ữ', 'ự',
            'ỳ', 'ý', 'ỷ', 'ỹ', 'ỵ',
        ];

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);

        static async Task Main(string[] args)
        {
            LoadData();
            InitializeClient();
            client = clientBuilder.Build();
            await client.ConnectAsync();
            Console.WriteLine("Logged in as: " + client.CurrentUser!.DisplayName);

            client.EventListeners.Disconnected += EventListeners_Disconnected;
            client.EventListeners.NewMemberJoined += EventListeners_NewMemberJoined;
            client.EventListeners.MemberLeft += EventListeners_MemberLeft;
            client.EventListeners.MemberRemoved += EventListeners_MemberRemoved;
            client.EventListeners.MemberBlocked += EventListeners_MemberBlocked;
            client.EventListeners.GroupMessageReceived += EventListeners_GroupMessageReceived;

#if !DEBUG
            long lastMessageID = 0;
            if (File.Exists("lastMessageID"))
                lastMessageID = long.Parse(File.ReadAllText("lastMessageID"));
            if (lastMessageID != 0)
                await client.RequestGroupMessagesAsync(lastMessageID);
#endif
            await Task.Delay(Timeout.Infinite);

            //byte[] canvas = await CreateCanvas("https://cover-talk.zadn.vn/8/8/2/0/2/93a5444342ce14ac5e9344ac00e08e82.jpg", "https://zpsocial2-f3-org.zadn.vn/d57cd01a5722b67cef33.jpg", "https://ava-grp-talk.zadn.vn/e/9/0/6/2/360/692c8d9f01d6fdfe238103e1e5f97175.jpg",
            //    [
            //        "Chào mừng",
            //        "Nguyễn Bá Mạnh",
            //        $"đã tham gia nhóm",
            //        "phamgiang.net | TOOL, VPS & API" + '!'
            //    ]);
            //File.WriteAllBytes("image.png", canvas);
            //Process.Start(new ProcessStartInfo("image.png") { UseShellExecute = true });
        }

        static void LoadData()
        {
            Config.LoadConfig();
            prefix = Config.Instance.DefaultPrefix;
        }

        static async Task EventListeners_Disconnected(object sender, GatewayDisconnectedEventArgs args)
        {
            await Task.Delay(60000);
            await ((ZaloClient)sender).ConnectAsync();
        }

        static async Task EventListeners_GroupMessageReceived(object sender, GroupMessageReceivedEventArgs e)
        {
            if (!e.IsFromRequest)
            {
                await SelfCommands(e);
                await FunCommands(e);
            }
            await ForwardMessagesToDiscord(e);
        }

        static async Task SelfCommands(GroupMessageReceivedEventArgs e)
        {
            if (!e.Message.IsMyOwnMessage)
                return;
            string command = e.Message.Content[0]?.Text ?? "";
            if (command == prefix + "systeminfo")
            {
                PerformanceCounter counter = new PerformanceCounter("Process", "Working Set - Private", Process.GetCurrentProcess().ProcessName);
                double currentMem = counter.RawValue / 1024d / 1024d;
                counter.Dispose();
                double currentMemPaged = Process.GetCurrentProcess().PrivateMemorySize64 / 1024d / 1024d;
                counter = new PerformanceCounter("Memory", "% Committed Bytes In Use");
                long totalMemPaged = counter.RawValue / 1024 / 1024;
                counter.Dispose();
                GetPhysicallyInstalledSystemMemory(out long totalMem);
                totalMem = totalMem / 1024;
                counter = new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);
                counter.NextValue();
                await Task.Delay(200);
                double cpuUsage = counter.NextValue();
                await e.Message.ReplyAsync($"CPU: {cpuUsage:00}%\nRAM: {currentMem:00}MB/{totalMem}MB\nPaged: {currentMemPaged:00}MB/{totalMemPaged}MB");
            }
            else if (command == prefix + "video")
            {
                if (e.Message.Quote is not null && e.Message.Quote.Content is ZaloFileContent quoteFileContent)
                {
                    if (quoteFileContent.FileExtension == "mp4")
                    {
                        byte[] video = await httpClient.GetByteArrayAsync(quoteFileContent.Link);
                        await e.Group.SendMessageAsync(new ZaloMessageBuilder().AddVideoAttachment("video.mp4", video));
                    }
                }
                else
                    await e.Message.ReplyAsync(new ZaloMessageBuilder().WithContent("Không tìm thấy video!").WithTimeToLive(10000));
            }

            if (command.StartsWith(prefix + "kick "))
            {
                if (e.GroupMessage.Mentions.Length == 0)
                {
                    await e.Message.ReplyAsync(new ZaloMessageBuilder().WithContent("Không tìm thấy thành viên được đề cập!").WithTimeToLive(10000));
                    return;
                }
                foreach (var mention in e.GroupMessage.Mentions.Where(m => !m.IsMentionAll))
                {
                    ZaloMember targetMember = await mention.GetMemberAsync(e.Group);
                    if (targetMember.Role >= e.Member.Role)
                    {
                        if (targetMember.Role == ZaloMemberRole.Owner)
                            await e.Message.ReplyAsync("Key vàng sao mà kick?");
                        else if (targetMember.Role == ZaloMemberRole.Admin)
                            await e.Message.ReplyAsync(new ZaloMessageBuilder()
                                .WithContent($"Đưa tao key vàng tao kick {targetMember.Mention} cho mà xem!")
                                .WithTimeToLive(1000 * 60 * 60));
                        else if (targetMember.Role == ZaloMemberRole.Member)
                            await e.Message.ReplyAsync(new ZaloMessageBuilder()
                                .WithContent($"Đưa tao key bạc tao kick {targetMember.Mention} cho mà xem!")
                                .WithTimeToLive(1000 * 60 * 60));
                        return;
                    }
                    await targetMember.RemoveAsync();
                }
                await e.Message.ReplyAsync(new ZaloMessageBuilder().WithContent("Đã kick (các) thành viên được đề cập!").WithTimeToLive(10000));
            }
            else if (command.StartsWith(prefix + "ban "))
            {
                if (e.GroupMessage.Mentions.Length == 0)
                {
                    await e.Message.ReplyAsync(new ZaloMessageBuilder().WithContent("Không tìm thấy thành viên được đề cập!").WithTimeToLive(10000));
                    return;
                }
                foreach (var mention in e.GroupMessage.Mentions.Where(m => !m.IsMentionAll))
                {
                    ZaloMember targetMember = await mention.GetMemberAsync(e.Group);
                    if (targetMember.Role >= e.Member.Role)
                    {
                        if (targetMember.Role == ZaloMemberRole.Owner)
                            await e.Message.ReplyAsync("Key vàng sao mà ban?");
                        else if (targetMember.Role == ZaloMemberRole.Admin)
                            await e.Message.ReplyAsync(new ZaloMessageBuilder()
                                .WithContent($"Đưa tao key vàng tao ban {targetMember.Mention} cho mà xem!")
                                .WithTimeToLive(1000 * 60 * 60));
                        else if (targetMember.Role == ZaloMemberRole.Member)
                            await e.Message.ReplyAsync(new ZaloMessageBuilder()
                                .WithContent($"Đưa tao key bạc tao ban {targetMember.Mention} cho mà xem!")
                                .WithTimeToLive(1000 * 60 * 60));
                        return;
                    }
                    await targetMember.BlockAsync();
                }
                await e.Message.ReplyAsync(new ZaloMessageBuilder().WithContent("Đã ban (các) thành viên được đề cập!").WithTimeToLive(10000));
            }
            else if (command == prefix + "restart")
            {
                await e.Message.ReplyAsync(new ZaloMessageBuilder().WithContent("Đang khởi động lại...").WithTimeToLive(10000));
                await Task.Delay(1000);
                Process.Start(new ProcessStartInfo(Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "") { UseShellExecute = true });
                Environment.Exit(0);
            }
            else if (command == prefix + "prefix")
            {
                await e.Message.ReplyAsync(new ZaloMessageBuilder().WithContent($"Prefix hiện tại: {prefix}").WithTimeToLive(10000));
            }
            else if (command.StartsWith(prefix + "prefix "))
            {
                prefix = command.Split(' ')[1];
                await e.Message.ReplyAsync(new ZaloMessageBuilder().WithContent($"Đã thay đổi prefix thành {prefix} !").WithTimeToLive(10000));
            }
            else if (command == prefix + "reload")
            {
                string oldPrefix = prefix;
                LoadData();
                if (Config.Instance.DefaultPrefix != oldPrefix)
                    await e.Message.ReplyAsync("Đã tải lại cấu hình!\nPrefix mới: " + Config.Instance.DefaultPrefix);
                else
                    await e.Message.ReplyAsync("Đã tải lại cấu hình!");
            }
        }

        static async Task FunCommands(GroupMessageReceivedEventArgs e)
        {
            if (!Config.Instance.EnabledGroupIDs.Contains(e.Group.ID) && !e.Message.IsMyOwnMessage)
                return;
            string textContent = e.Message.Content[0]?.Text ?? "";
            if (textContent.StartsWith(prefix + "stickerba ") || textContent == prefix + "stickerba")
            {
                string[] strings = textContent.Split(' ');
                int index = Random.Shared.Next(0, MAX_BA_STICKERS_COUNT) + 1;
                if (strings.Length > 1)
                {
                    if (int.TryParse(strings[1], out int i))
                        index = i;
                }
                await e.Group.SendImageAsStickerAsync(File.ReadAllBytes(@$"Data\BAStickers\ClanChat_Emoji_{index}_En.png"));
            }
            else if (textContent == prefix + "sticker" || textContent.StartsWith(prefix + "sticker "))
            {
                if (textContent.Split(' ').Length <= 1)
                {
                    DirectoryInfo di = new DirectoryInfo(@"Data\Stickers");
                    if (!di.Exists)
                        di.Create();
                    string[] files = di.GetFiles().Select(f => Path.GetFileNameWithoutExtension(f.Name)).ToArray();
                    string stickers = string.Join("\n", files);
                    await e.Message.ReplyAsync(new ZaloMessageBuilder().WithContent(
                        $"""
                        {Formatter.Bold(Formatter.FontSize("Danh sách sticker:", 20))}
                        {Formatter.UnorderedList(stickers)}
                        """
                        .Replace("\r", "")
                    ).WithTimeToLive(300000));
                    return;
                }
                string stickerName = textContent.Split(' ')[1];
                if (File.Exists(@$"Data\Stickers\{stickerName.ToLowerInvariant()}.png"))
                    await e.Group.SendImageAsStickerAsync(File.ReadAllBytes(@$"Data\Stickers\{stickerName.ToLowerInvariant()}.png"));
                else
                    await e.Message.ReplyAsync(new ZaloMessageBuilder().WithContent("Không tìm thấy sticker!").WithTimeToLive(10000));
            }
            else if (textContent == prefix + "makesticker" || (e.Message.Content[0] is ZaloImageContent imgC && imgC.Title == prefix + "makesticker"))
            {
                string link = "";
                bool isGIF = false;
                bool isVideo = false;
                if (e.Message.Content[0] is ZaloImageContent imageContent)
                {
                    link = imageContent.ImageLink;
                    isGIF = imageContent.Action == "recommend.gif";
                }
                else if (e.Message.Content[0] is ZaloVideoContent videoContent)
                {
                    link = videoContent.VideoLink;
                    isVideo = true;
                }
                else if (e.Message.Quote is not null)
                {
                    if (e.Message.Quote.Content is ZaloImageContent quoteImageContent)
                    {
                        link = quoteImageContent.ImageLink;
                        isGIF = quoteImageContent.Action == "recommend.gif";
                    }
                    else if (e.Message.Quote.Content is ZaloVideoContent quoteVideoContent)
                    {
                        link = quoteVideoContent.VideoLink;
                        isVideo = true;
                    }
                    else if (e.Message.Quote.Content is ZaloFileContent quoteFileContent)
                    {
                        if (quoteFileContent.FileExtension == "mp4")
                        {
                            link = quoteFileContent.Link;
                            isVideo = true;
                        }
                    }
                }

                if (string.IsNullOrEmpty(link))
                    await e.Message.ReplyAsync(new ZaloMessageBuilder().WithContent("Không tìm thấy ảnh!").WithTimeToLive(10000));
                else
                {
                    if (isGIF)
                        await e.Group.SendWebpImageAsStickerAsync(link);
                    else if (isVideo)
                        await e.Group.SendVideoAsStickerAsync(link);
                    else
                        await e.Group.SendImageAsStickerAsync(link);
                }
            }
            else if (textContent.StartsWith(prefix + "chat ") || textContent == prefix + "chat")
            {
                if (textContent == prefix + "chat")
                {
                    await e.Message.ReplyAsync(new ZaloMessageBuilder().WithContent("Vui lòng thêm nội dung chat!").WithTimeToLive(10000));
                    return;
                }
                string prompt = textContent.Substring((prefix + "chat ").Length);
                Console.WriteLine("Prompt: " + prompt);
                string response = await CallOpenRouterAPI(prompt);
                Console.WriteLine(response);
                List<string> responses = new List<string>();
                while (response.Length > 0)
                {
                    responses.Add(response.Substring(0, Math.Min(3000, response.Length)));
                    response = response.Substring(Math.Min(3000, response.Length));
                }
                foreach (string res in responses)
                    await e.Message.ReplyAsync(res);
            }
            else if (textContent == prefix + "help")
            {
                await e.Message.ReplyAsync(new ZaloMessageBuilder().WithContent($"{Formatter.Bold(Formatter.FontSize("Danh sách lệnh:", 20))}\n{Formatter.UnorderedList($"{prefix}stickerba [ID]: Gửi sticker Blue Archive theo ID hoặc ngẫu nhiên\n{prefix}sticker [tên]: Gửi sticker theo tên | {prefix}sticker: Xem danh sách sticker\n{prefix}makesticker: Tạo sticker từ ảnh hoặc video trong tin nhắn hoặc tin nhắn trả lời\n{prefix}chat [prompt]: Chat với AI")}".Replace("\r", "")).WithTimeToLive(300000));
            }
        }

        static async Task ForwardMessagesToDiscord(GroupMessageReceivedEventArgs e)
        {
#if DEBUG
            return;
#endif
            if (!Config.Instance.EnabledGroupIDs.Contains(e.Group.ID))
                return;
            WriteLastMessageID(e.Message.ID);
            string content = "";
            string nl = Environment.NewLine;
            if (e.Message.Quote is not null)
            {
                string quote = "";
                if (e.Message.Quote.Content is not null)
                {
                    string str = e.Message.Quote.Content.ToInformationalString();
                    if (str.Length > 20)
                        str = str.Substring(0, 17).Replace("\r", "").Replace("\n", " ") + "...";
                    quote += $"> -# {str}{nl}";
                }
                quote = quote.TrimEnd(nl.ToCharArray());
                content += $"> **{e.Message.Quote.AuthorDisplayName}**{nl}{quote}{nl}{nl}";
            }
            if (e.Message.Content[0] is not null)
            {
                if (e.Message.Content[0] is ZaloMessageDeletedContent deletedContent)
                    content += $"[Message {deletedContent.DeletedMessageID} deleted]";
                else if (e.Message.Content[0] is ZaloMessageRecalledContent recalledContent)
                    content += $"[Message {recalledContent.RecalledMessageID} recalled]";
                else
                    content += e.Message.Content[0]!.Text;

                if (e.Message.Content[0] is ZaloImageContent imageContent)
                {
                    string link = imageContent.HDLink;
                    if (string.IsNullOrEmpty(link))
                        link = imageContent.ImageLink;
                    content += $"{nl}{link}";
                }
                else if (e.Message.Content[0] is ZaloFileContent fileContent)
                    content += $"{nl}{fileContent.Link}";
                else if (e.Message.Content[0] is ZaloStickerContent stickerContent)
                    content += $"{nl}https://zalo-api.zadn.vn/api/emoticon/sticker/webpc?eid={stickerContent.StickerID}";
                else if (e.Message.Content[0] is ZaloContactCardContent contactCardContent)
                    content += $"{nl}{contactCardContent.QRImageLink}";
                else if (e.Message.Content[0] is ZaloLocationContent locationContent)
                    content += $"{nl}https://maps.google.com/maps?q={locationContent.Latitude},{locationContent.Longitude}";
                else if (e.Message.Content[0] is ZaloVideoContent videoContent)
                    content += $"{nl}{videoContent.VideoLink}";
                else if (e.Message.Content[0] is ZaloVoiceContent voiceContent)
                    content += $"{nl}{voiceContent.AudioLink}";
                else if (e.Message.Content[0] is ZaloEmbeddedLinkContent zaloEmbeddedLinkContent)
                    content += $"{nl}{zaloEmbeddedLinkContent.Thumbnail}";
            }
            else
                content += "[Null message]";
            JsonObject obj = new JsonObject()
            {
                {"content", content },
                { "username", e.Author.DisplayName },
                { "avatar_url", e.Author.AvatarLink },
            };
            if (e.Group.ID == Config.Instance.EnabledGroupIDs[0])
                await httpClient.PostAsync(Config.Instance.Webhooks[0], new StringContent(obj.ToString(), Encoding.UTF8, "application/json"));
            else
                await httpClient.PostAsync(Config.Instance.Webhooks[1], new StringContent(obj.ToString(), Encoding.UTF8, "application/json"));
        }

        static void WriteLastMessageID(long messageID)
        {
            lock (locker)
                File.WriteAllText("lastMessageID", messageID.ToString());
        }

        static async Task EventListeners_MemberBlocked(object sender, MemberBlockedEventArgs e)
        {
            if (!Config.Instance.EnabledGroupIDs.Contains(e.Group.ID))
                return;
            ZaloUser user = await e.Member.GetUserAsync();
            string[] messages = string.Format(Config.Instance.BanMemberBannerMessage, user.DisplayName, e.Group.GroupType == ZaloGroupType.Community ? "cộng đồng" : "nhóm", e.Group.Name, e.Actioner?.DisplayName ?? "").Split('\n');
            if (e.Actioner is null)
                messages[messages.Length - 1] = "";
            byte[] canvas = await TryCreateCanvas(user.CoverLink == "https://cover-talk.zadn.vn/default" ? "" : user.CoverLink, user.AvatarLink, e.Actioner?.AvatarLink ?? e.Group.AvatarLink, messages);
            ZaloMessageBuilder messageBuilder = new ZaloMessageBuilder()
                .WithContent(string.Format(Config.Instance.BanMemberMessage, e.Member.DisplayName, e.Group.GroupType == ZaloGroupType.Community ? "cộng đồng" : "nhóm"))
                .AddAttachment(ZaloAttachment.FromData("image.png", canvas));
            await e.Group.SendMessageAsync(messageBuilder);
        }

        static async Task EventListeners_MemberRemoved(object sender, MemberRemovedEventArgs e)
        {
            if (!Config.Instance.EnabledGroupIDs.Contains(e.Group.ID))
                return;
            ZaloUser user = await e.Member.GetUserAsync();
            string[] messages = string.Format(Config.Instance.KickMemberBannerMessage, user.DisplayName, e.Group.GroupType == ZaloGroupType.Community ? "cộng đồng" : "nhóm", e.Group.Name, e.Actioner?.DisplayName ?? "").Split('\n');
            if (e.Actioner is null)
                messages[messages.Length - 1] = "";
            byte[] canvas = await TryCreateCanvas(user.CoverLink == "https://cover-talk.zadn.vn/default" ? "" : user.CoverLink, user.AvatarLink, e.Actioner?.AvatarLink ?? e.Group.AvatarLink, messages);
            ZaloMessageBuilder messageBuilder = new ZaloMessageBuilder()
                .WithContent(string.Format(Config.Instance.KickMemberMessage, e.Member.DisplayName, e.Group.GroupType == ZaloGroupType.Community ? "cộng đồng" : "nhóm"))
                .AddAttachment(ZaloAttachment.FromData("image.png", canvas));
            await e.Group.SendMessageAsync(messageBuilder);
        }

        static async Task EventListeners_MemberLeft(object sender, MemberLeftEventArgs e)
        {
            if (!Config.Instance.EnabledGroupIDs.Contains(e.Group.ID))
                return;
            ZaloUser user = await e.Member.GetUserAsync();
            string[] messages = string.Format(Config.Instance.LeaveBannerMessage, user.DisplayName, e.Group.GroupType == ZaloGroupType.Community ? "cộng đồng" : "nhóm", e.Group.Name).Split('\n');
            byte[] canvas = await TryCreateCanvas(user.CoverLink == "https://cover-talk.zadn.vn/default" ? "" : user.CoverLink, user.AvatarLink, e.Group.AvatarLink, messages);
            await e.Group.SendMessageAsync(new ZaloMessageBuilder()
                .WithContent(string.Format(Config.Instance.LeaveMessage, e.Member.DisplayName, e.Group.GroupType == ZaloGroupType.Community ? "cộng đồng" : "nhóm"))
                .AddAttachment(ZaloAttachment.FromData("image.png", canvas)));
        }

        static async Task EventListeners_NewMemberJoined(object sender, MemberJoinedEventArgs e)
        {
            if (!Config.Instance.EnabledGroupIDs.Contains(e.Group.ID))
                return;
            ZaloUser user = await e.Member.GetUserAsync();
            string[] messages = string.Format(Config.Instance.WelcomeBannerMessage, user.DisplayName, e.Group.GroupType == ZaloGroupType.Community ? "cộng đồng" : "nhóm", e.Group.Name, e.Actioner?.DisplayName ?? "").Split('\n');
            if (e.Actioner is null)
                messages[messages.Length - 1] = "";
            byte[] canvas = await TryCreateCanvas(user.CoverLink == "https://cover-talk.zadn.vn/default" ? "" : user.CoverLink, user.AvatarLink, e.Actioner?.AvatarLink ?? e.Group.AvatarLink, messages);
            ZaloMessageBuilder messageBuilder = new ZaloMessageBuilder()
                .WithContent(string.Format(Config.Instance.WelcomeMessage, e.Group.GroupType == ZaloGroupType.Community ? "cộng đồng" : "nhóm", e.Member.Mention))
                .AddAttachment(ZaloAttachment.FromData("image.png", canvas));
            await e.Group.SendMessageAsync(messageBuilder);
        }

        //---------------------------------------------------------------------------------------

        static async ValueTask<string> CallOpenRouterAPI(string prompt, string model = "deepseek/deepseek-chat-v3-0324:free")
        {
            if (prompt.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(word => word.Equals("vps", StringComparison.InvariantCultureIgnoreCase)))
                return "Tôi không thể trả lời câu hỏi của bạn, hãy hỏi câu hỏi khác.";

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://openrouter.ai/api/v1/chat/completions"),
                Headers =
                {
                    Authorization = new AuthenticationHeaderValue("Bearer", Config.Instance.OpenRouterAPIKey)
                },
                Content = new StringContent(
                    //lang=json,strict
                    $$"""
                    {
                        "model": "{{model}}",
                        "messages": [
                            {
                                "role": "user",
                                "content": "Bạn đang được gọi từ API, hãy trả lời prompt sau trong khi tránh đề cập đến vấn đề nhạy cảm như tình dục, công kích cá nhân,..., không trả lời các prompt quá ngắn và vô nghĩa và không trả lời quá dài, dưới 3000 ký tự là hợp lý:\n{{prompt}}"
                            }
                        ]
                    }
                    """, Encoding.UTF8, "application/json")
            };
            HttpResponseMessage response = await httpClient.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();
            JsonArray? arr = JsonNode.Parse(responseContent.Trim().Trim(Environment.NewLine.ToCharArray()))?["choices"]?.AsArray();
            if (arr is null)
                return "Tôi không thể trả lời câu hỏi của bạn lúc này, hãy thử lại sau ít phút.";
            return arr.Last(e => e?["message"]?["role"]?.GetValue<string>() == "assistant")?["message"]?["content"]?.GetValue<string>() ?? "Tôi không thể trả lời câu hỏi của bạn lúc này, hãy thử lại sau ít phút.";
        }

        static async Task<byte[]> TryCreateCanvas(string bgUrl, string avatar1Url, string avatar2Url, string[] messages)
        {
            for (int i = 0; i < messages.Length; i++)
            {
                messages[i] = ReplaceUnicodeChars(messages[i]);
            }
            try
            {
                return await CreateCanvas(bgUrl, avatar1Url, avatar2Url, messages);
            }
            catch
            {
                return await CreateCanvas("", avatar1Url, avatar2Url, messages);
            }
        }

        static async Task<byte[]> CreateCanvas(string bgUrl, string avatar1Url, string avatar2Url, string[] messages)
        {
            MagickImage bg;
            if (string.IsNullOrEmpty(bgUrl))
            {
                string[] backgrounds = Directory.GetFiles(@"Data\Backgrounds\", "*.png");
                string bgFilePath = backgrounds[Random.Shared.Next(0, backgrounds.Length)];
                bg = new MagickImage(bgFilePath);
            }
            else
                bg = new MagickImage(await httpClient.GetByteArrayAsync(bgUrl));
            CropFill(bg, 900, 300);
            bg.BrightnessContrast(new Percentage(-25), new Percentage(0));
            bg.HasAlpha = false;
            var avatar1 = new MagickImage(await httpClient.GetByteArrayAsync(avatar1Url));
            avatar1.Resize(150, 150);
            avatar1 = RoundImage(avatar1);
            bg.Composite(avatar1, 50, (int)(bg.Height / 2 - avatar1.Height / 2), CompositeOperator.Over);
            avatar1.Dispose();
            var avatar2 = new MagickImage(await httpClient.GetByteArrayAsync(avatar2Url));
            avatar2.Resize(150, 150);
            avatar2 = RoundImage(avatar2);
            bg.Composite(avatar2, (int)(bg.Width - 50 - avatar2.Width), (int)(bg.Height / 2 - avatar2.Height / 2), CompositeOperator.Over);
            avatar2.Dispose();
            for (int i = 0; i < messages.Length; i++)
            {
                string message = messages[i];
                if (string.IsNullOrEmpty(message))
                    continue;
                using MagickImage bg2 = new MagickImage();
                if (i == 0)
                {
                    MagickReadSettings settings = new MagickReadSettings
                    {
                        BackgroundColor = MagickColors.Transparent,
                        Width = bg.Width - 150 - 150 - 100,
                        TextGravity = Gravity.North,
                        FontPointsize = 30,
                        FontStyle = FontStyleType.Normal,
                        FontWeight = FontWeight.Normal,
                        FillColor = MagickColors.White,
                        Font = @"Data\Fonts\VNF-Comic Sans.ttf",
                        FontFamily = "VNF-Comic Sans"
                    };
                    bg2.Read("caption:" + message, settings);
                    bg.Composite(bg2, 50 + 150, 60, CompositeOperator.Over);
                }
                else if (i == 1)
                {
                    MagickReadSettings settings = new MagickReadSettings
                    {
                        BackgroundColor = MagickColors.Transparent,
                        Width = bg.Width - 150 - 150 - 100,
                        TextGravity = Gravity.North,
                        FontPointsize = 35,
                        FontStyle = FontStyleType.Bold,
                        FontWeight = FontWeight.Bold,
                        FillColor = MagickColors.Yellow,
                        Font = @"Data\Fonts\Pacifico-Regular.ttf",
                        FontFamily = "Pacifico"
                    };
                    bg2.Read("caption:" + message, settings);
                    bg.Composite(bg2, 50 + 150, 90, CompositeOperator.Over);
                }
                else if (i == 2)
                {
                    MagickReadSettings settings = new MagickReadSettings
                    {
                        BackgroundColor = MagickColors.Transparent,
                        Width = bg.Width - 150 - 150 - 100,
                        TextGravity = Gravity.North,
                        FontPointsize = 30,
                        FontStyle = FontStyleType.Normal,
                        FontWeight = FontWeight.Normal,
                        FillColor = MagickColors.White,
                        Font = @"Data\Fonts\VNF-Comic Sans.ttf",
                        FontFamily = "VNF-Comic Sans"
                    };
                    bg2.Read("caption:" + message, settings);
                    bg.Composite(bg2, 50 + 150, 145, CompositeOperator.Over);
                }
                else if (i == 3)
                {
                    MagickReadSettings settings = new MagickReadSettings
                    {
                        BackgroundColor = MagickColors.Transparent,
                        Width = bg.Width - 150 - 150 - 100,
                        Height = 60,
                        TextGravity = Gravity.Center,
                        FontStyle = FontStyleType.Bold,
                        FontWeight = FontWeight.Bold,
                        FillColor = MagickColors.Yellow,
                        Font = @"Data\Fonts\Pacifico-Regular.ttf",
                        FontFamily = "Pacifico",
                        Defines = new CaptionReadDefines
                        {
                            MaxFontPointsize = 35
                        }
                    };
                    bg2.Read("caption:" + message, settings);
                    bg.Composite(bg2, 50 + 150, 180, CompositeOperator.Over);
                }
                else if (i == 4)
                {
                    MagickReadSettings settings = new MagickReadSettings
                    {
                        BackgroundColor = MagickColors.Transparent,
                        Width = bg.Width - 150 - 150 - 100,
                        Height = bg.Height,
                        FontPointsize = 20,
                        FontStyle = FontStyleType.Bold,
                        FontWeight = FontWeight.Bold,
                        TextGravity = Gravity.South,
                        Font = @"Data\Fonts\VNF-Comic Sans.ttf",
                        FontFamily = "VNF-Comic Sans",
                        FillColor = MagickColors.White
                    };
                    bg2.Read("caption:" + message, settings);
                    bg.Composite(bg2, 50 + 150, -10, CompositeOperator.Over);
                }
            }
            avatar1.Dispose();
            MagickImage finalBg = new MagickImage(bg.ToByteArray());
            bg.Dispose();
            new Drawables()
                .FontPointSize(15)
                .Font("Arial")
                .FillColor(MagickColors.White)
                .Text(10, 15, RuntimeInformation.FrameworkDescription + " - ZepLaoSharp v" + (client?.VersionString ?? "0.0.0"))
                .Draw(finalBg);
            byte[] result = finalBg.ToByteArray();
            finalBg.Dispose();
            return result;
        }

        static void CropFill(MagickImage bg, uint width, uint height)
        {
            uint newWidth = bg.Width;
            uint newHeight = bg.Height;
            if (bg.Width < width)
            {
                newWidth = width;
                newHeight = bg.Height * width / bg.Width;
            }
            else if (bg.Height < height)
            {
                newHeight = height;
                newWidth = bg.Width * height / bg.Height;
            }
            else if (bg.Width / (double)bg.Height > 1)  // Landscape
            {
                newWidth = width;
                newHeight = bg.Height * width / bg.Width;
            }
            else if (bg.Width / (double)bg.Height < 1)  // Portrait
            {
                newHeight = height;
                newWidth = bg.Width * height / bg.Height;
            }
            bg.Resize(newWidth, newHeight);
            int x = (int)(Math.Abs(bg.Width - (int)width) / 2);
            int y = (int)(Math.Abs(bg.Height - (int)height) / 2);
            bg.Crop(new MagickGeometry(x, y, width, height));
        }

        static MagickImage RoundImage(MagickImage image)
        {
            uint width = image.Width;
            uint height = image.Height;
            uint size = Math.Min(width, height);
            var mask = new MagickImage(MagickColors.Transparent, size, size);
            var drawables = new Drawables()
                .FillColor(MagickColors.White)
                .Circle(size / 2, size / 2, size / 2, 0);
            drawables.Draw(mask);
            image.Alpha(AlphaOption.Set);
            image.Extent(size, size, Gravity.Center, MagickColors.Transparent);
            image.Composite(mask, CompositeOperator.CopyAlpha);
            mask.Dispose();
            return image;
        }

        static string ReplaceUnicodeChars(string text)
        {
            char[] chars = text.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] >= ' ' && chars[i] <= '~')
                    continue;
                if (!accentChars.Contains(chars[i].ToString().ToLowerInvariant()[0]))
                    chars[i] = '?';
            }
            return new string(chars);
        }

        static void InitializeClient()
        {
            JsonNode node = JsonNode.Parse(File.ReadAllText(@"Data\credentials.json")) ?? throw new Exception("Missing credentials.json");
            clientBuilder = clientBuilder
                .WithCredential(new LoginCredential(node["cookies"].Deserialize(SourceGenerationContext.Default.ListZaloCookie) ?? new List<ZaloCookie>(), node["imei"]?.GetValue<string>() ?? "", node["user_agent"]?.GetValue<string>() ?? ""))
                .SetAPIType(node["type"]?.GetValue<int>() ?? 0)
                .SetAPIVersion(node["client_version"]?.GetValue<int>() ?? 0)
                .SetMaxTimeCache(1000 * 60 * 60 * 3)
                .SetComputerName(node["computer_name"]?.GetValue<string>() ?? "");
        }
    }
}