using ImageMagick;
using ImageMagick.Drawing;
using ImageMagick.Formats;
using System.Diagnostics;
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
        static long[] includedGroupIDs = [1203610066457374544, 8095917403278057918];
        static long xiaoGroupID = 2516010607529018126;
        static int maxBgLength = 1;
        static ZaloClientBuilder clientBuilder = ZaloClientBuilder.CreateDefault();
        static ZaloClient client;
        static HttpClient httpClient = new HttpClient();
        static List<long> mutedUserIDs = new List<long>();
        static object locker = new object();
        static string[] webhooks = [];

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);

        static async Task Main(string[] args)
        {
            while (File.Exists(@$"Backgrounds\{maxBgLength}.png"))
                maxBgLength++;
            maxBgLength--;
            webhooks = File.ReadAllLines("webhooks.txt");

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
            try
            {
                if (File.Exists("mutedUserIDs"))
                    mutedUserIDs = File.ReadAllText("mutedUserIDs").Split(',').Select(long.Parse).ToList();
            }
            catch { }
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

        private static async Task EventListeners_Disconnected(object sender, GatewayDisconnectedEventArgs args)
        {
            await Task.Delay(60000);
            await ((ZaloClient)sender).ConnectAsync();
        }

        static async Task EventListeners_GroupMessageReceived(object sender, GroupMessageReceivedEventArgs e)
        {
            if (e.Message.IsMyOwnMessage && !e.IsFromRequest)
            {
                string textContent = e.Message.Content[0]?.ToInformationalString() ?? "";
                if (textContent == "!system_info")
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

                    await e.Message.ReplyAsync(new ZaloMessageBuilder().WithContent($"CPU: {cpuUsage:00}%\nRAM: {currentMem:00}MB/{totalMem}MB\nPaged: {currentMemPaged:00}MB/{totalMemPaged}MB"));
                    await e.Message.RecallAsync();
                }
                else if (textContent == "!sticker" || (e.Message.Content[0] is ZaloImageContent imgC && imgC.Title == "!sticker"))
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
                        await e.Message.RecallAsync();
                        if (isGIF)
                            await e.Group.SendWebpImageAsStickerAsync(link);
                        else if (isVideo)
                            await e.Group.SendVideoAsStickerAsync(link);
                        else
                            await e.Group.SendImageAsStickerAsync(link);
                    }
                }
                else if (textContent == "!video")
                {
                    if (e.Message.Quote is not null && e.Message.Quote.Content is ZaloFileContent quoteFileContent)
                    {
                        if (quoteFileContent.FileExtension == "mp4")
                        {
                            await e.Message.RecallAsync();
                            byte[] video = await httpClient.GetByteArrayAsync(quoteFileContent.Link);
                            await e.Group.SendMessageAsync(new ZaloMessageBuilder().AddVideoAttachment("video.mp4", video));
                        }
                    }
                    else
                        await e.Message.ReplyAsync(new ZaloMessageBuilder().WithContent("Không tìm thấy video!").WithTimeToLive(10000));
                }

                else if (textContent == "!testwelcome")
                {
                    await e.Message.RecallAsync();
                    ZaloUser user = await client.GetUserAsync(910878639181570880);
                    byte[] canvas = await TryCreateCanvas(user.CoverLink == "https://cover-talk.zadn.vn/default" ? "" : user.CoverLink, user.AvatarLink, e.Group.AvatarLink,
                        [
                            "Chào mừng",
                            user.DisplayName, 
                            $"đã tham gia {(e.Group.GroupType == ZaloGroupType.Community ? "cộng đồng" : "nhóm")}",
                            e.Group.Name + '!', 
                            $"Duyệt bởi {e.Group.Owner.DisplayName}"
                        ]);
                    ZaloMessageBuilder messageBuilder = new ZaloMessageBuilder()
                        .WithTimeToLive(120000)
                        .WithContent("Chào mừng thành viên mới @" + user.DisplayName + "!")
                        .AddAttachment(ZaloAttachment.FromData("image.png", canvas));
                    await e.Group.SendMessageAsync(messageBuilder);
                }
            }
            await ForwardMessagesToDiscord(e);
            await ModerateXiaoGroupAsync(e);
        }

        private static async Task ModerateXiaoGroupAsync(GroupMessageReceivedEventArgs e)
        {
            if (e.Group.ID != xiaoGroupID)
                return;
            if (!e.Message.IsMyOwnMessage)
            {
                if (mutedUserIDs.Contains(e.Author.ID))
                {
                    await e.Message.DeleteAsync();
                    return;
                }
            }
            if (e.Message.Content[0] is ZaloTextContent textContent && (e.Group.Admins.Any(a => a.ID == e.Author.ID) || e.Group.Owner.ID == e.Author.ID) && !e.IsFromRequest)
            {
                string content = textContent.Content ?? "";
                if (content.StartsWith("!khoamom "))
                {
                    if (e.Message.Mentions.Length == 0)
                    {
                        await e.Message.ReplyAsync(new ZaloMessageBuilder().WithContent("Không tìm thấy thành viên được đề cập!").WithTimeToLive(10000));
                        return;
                    }
                    foreach (var mention in e.Message.Mentions.Where(m => !m.IsMentionAll))
                    {
                        if (!mutedUserIDs.Contains(mention.UserID))
                            mutedUserIDs.Add(mention.UserID);
                    }
                    File.WriteAllText("mutedUserIDs", string.Join(',', mutedUserIDs));
                    await e.Message.ReplyAsync(new ZaloMessageBuilder().WithContent("Đã khoá mõm (các) thành viên được đề cập!").WithTimeToLive(10000));
                }
                else if (content.StartsWith("!mokhoamom "))
                {
                    if (e.Message.Mentions.Length == 0)
                    {
                        await e.Message.ReplyAsync(new ZaloMessageBuilder().WithContent("Không tìm thấy thành viên được đề cập!").WithTimeToLive(10000));
                        return;
                    }
                    foreach (var mention in e.Message.Mentions.Where(m => !m.IsMentionAll))
                    {
                        if (mutedUserIDs.Contains(mention.UserID))
                            mutedUserIDs.Remove(mention.UserID);
                    }
                    File.WriteAllText("mutedUserIDs", string.Join(',', mutedUserIDs));
                    await e.Message.ReplyAsync(new ZaloMessageBuilder().WithContent("Đã mở khoá mõm (các) thành viên được đề cập!").WithTimeToLive(10000));
                }
            }
        }

        private static async Task ForwardMessagesToDiscord(GroupMessageReceivedEventArgs e)
        {
            if (!includedGroupIDs.Contains(e.Group.ID))
                return;
            WriteLastMessageID(e.Message.ID);
            string content = "";
            string nl = Environment.NewLine;
            if (e.Message.Quote is not null)
            {
                string quote = "";
                if (e.Message.Quote.Content is not null)
                {
                    foreach (string s in e.Message.Quote.Content.ToInformationalString().Split('\n'))
                    {
                        quote += $"> -# {s.Trim(nl.ToCharArray())}{nl}";
                    }
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
                    content += e.Message.Content[0]!.ToInformationalString();

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
            if (e.Group.ID == includedGroupIDs[0])
                await httpClient.PostAsync(webhooks[0], new StringContent(obj.ToString(), Encoding.UTF8, "application/json"));
            else
                await httpClient.PostAsync(webhooks[1], new StringContent(obj.ToString(), Encoding.UTF8, "application/json"));
        }

        static void WriteLastMessageID(long messageID)
        {
            lock (locker)
                File.WriteAllText("lastMessageID", messageID.ToString());
        }

        static async Task EventListeners_MemberBlocked(object sender, MemberBlockedEventArgs e)
        {
            if (!includedGroupIDs.Contains(e.Group.ID))
                return;
            string content = $"-# **{e.Member.DisplayName}** vừa bị {e.Actioner?.DisplayName} chặn khỏi {(e.Group.GroupType == ZaloGroupType.Community ? "cộng đồng" : "nhóm")}.";
            JsonObject obj = new JsonObject()
            {
                {"content", content },
            };
            if (e.Group.ID == includedGroupIDs[0])
                await httpClient.PostAsync(webhooks[0], new StringContent(obj.ToString(), Encoding.UTF8, "application/json"));
            else
                await httpClient.PostAsync(webhooks[1], new StringContent(obj.ToString(), Encoding.UTF8, "application/json"));
            ZaloUser user = await e.Member.GetUserAsync();
            byte[] canvas = await TryCreateCanvas(user.CoverLink == "https://cover-talk.zadn.vn/default" ? "" : user.CoverLink, user.AvatarLink, e.Group.AvatarLink,
            [
                "Vĩnh biệt",
                user.DisplayName, 
                $"Đã không thể quay lại {(e.Group.GroupType == ZaloGroupType.Community ? "cộng đồng" : "nhóm")}",
                e.Group.Name + '!', 
                $"Bị {e.Group.Owner.DisplayName} chặn"
            ]);
            ZaloMessageBuilder messageBuilder = new ZaloMessageBuilder().WithContent($"Thành viên {e.Member.DisplayName} đã bị chặn khỏi {(e.Group.GroupType == ZaloGroupType.Community ? "cộng đồng" : "nhóm")}!").AddAttachment(ZaloAttachment.FromData("image.png", canvas));
            await e.Group.SendMessageAsync(messageBuilder);
        }

        static async Task EventListeners_MemberRemoved(object sender, MemberRemovedEventArgs e)
        {
            if (!includedGroupIDs.Contains(e.Group.ID))
                return;
            string content = $"-# **{e.Member.DisplayName}** vừa bị {e.Actioner?.DisplayName} xoá khỏi {(e.Group.GroupType == ZaloGroupType.Community ? "cộng đồng" : "nhóm")}.";
            JsonObject obj = new JsonObject()
            {
                {"content", content },
            };
            if (e.Group.ID == includedGroupIDs[0])
                await httpClient.PostAsync(webhooks[0], new StringContent(obj.ToString(), Encoding.UTF8, "application/json"));
            else
                await httpClient.PostAsync(webhooks[1], new StringContent(obj.ToString(), Encoding.UTF8, "application/json"));
            ZaloUser user = await e.Member.GetUserAsync();
            byte[] canvas = await TryCreateCanvas(user.CoverLink == "https://cover-talk.zadn.vn/default" ? "" : user.CoverLink, user.AvatarLink, e.Group.AvatarLink,
            [
                "Thành viên",
                user.DisplayName,
                $"Đã bị đá khỏi {(e.Group.GroupType == ZaloGroupType.Community ? "cộng đồng" : "nhóm")}",
                e.Group.Name + '!',
                $"Bị {e.Group.Owner.DisplayName} đuổi"
            ]);
            ZaloMessageBuilder messageBuilder = new ZaloMessageBuilder().WithContent($"Thành viên {e.Member.DisplayName} đã bị đuổi khỏi {(e.Group.GroupType == ZaloGroupType.Community ? "cộng đồng" : "nhóm")}!").AddAttachment(ZaloAttachment.FromData("image.png", canvas));
            await e.Group.SendMessageAsync(messageBuilder);
        }

        static async Task EventListeners_MemberLeft(object sender, MemberLeftEventArgs e)
        {
            if (!includedGroupIDs.Contains(e.Group.ID))
                return;
            string content = $"-# **{e.Member.DisplayName}** vừa rời khỏi {(e.Group.GroupType == ZaloGroupType.Community ? "cộng đồng" : "nhóm")}.";
            JsonObject obj = new JsonObject()
            {
                {"content", content },
            };
            if (e.Group.ID == includedGroupIDs[0])
                await httpClient.PostAsync(webhooks[0], new StringContent(obj.ToString(), Encoding.UTF8, "application/json"));
            else
                await httpClient.PostAsync(webhooks[1], new StringContent(obj.ToString(), Encoding.UTF8, "application/json"));
            ZaloUser user = await e.Member.GetUserAsync();
            byte[] canvas = await TryCreateCanvas(user.CoverLink == "https://cover-talk.zadn.vn/default" ? "" : user.CoverLink, user.AvatarLink, e.Group.AvatarLink,
            [
                "Tạm biệt thành viên",
                user.DisplayName,
                $"Đã rời {(e.Group.GroupType == ZaloGroupType.Community ? "cộng đồng" : "nhóm")}",
                e.Group.Name + '!',
            ]);
            await e.Group.SendMessageAsync(new ZaloMessageBuilder()
                .AppendContent($"Tạm biệt {e.Member.DisplayName} đã rời {(e.Group.GroupType == ZaloGroupType.Community ? "cộng đồng" : "nhóm")}!")
                .AddAttachment(ZaloAttachment.FromData("image.png", canvas)));
        }

        static async Task EventListeners_NewMemberJoined(object sender, MemberJoinedEventArgs e)
        {
            if (!includedGroupIDs.Contains(e.Group.ID))
                return;
            string content;
            content = $"-# **{e.Member.DisplayName}** được **{e.Actioner?.DisplayName}** duyệt vào {(e.Group.GroupType == ZaloGroupType.Community ? "cộng đồng" : "nhóm")}.";
            JsonObject obj = new JsonObject()
            {
                {"content", content },
            };
            if (e.Group.ID == includedGroupIDs[0])
                await httpClient.PostAsync(webhooks[0], new StringContent(obj.ToString(), Encoding.UTF8, "application/json"));
            else
                await httpClient.PostAsync(webhooks[1], new StringContent(obj.ToString(), Encoding.UTF8, "application/json"));
            ZaloUser user = await e.Member.GetUserAsync();
            byte[] canvas = await TryCreateCanvas(user.CoverLink == "https://cover-talk.zadn.vn/default" ? "" : user.CoverLink, user.AvatarLink, e.Group.AvatarLink,
            [
                "Chào mừng",
                user.DisplayName,
                $"Đã tham gia {(e.Group.GroupType == ZaloGroupType.Community ? "cộng đồng" : "nhóm")}",
                e.Group.Name + '!',
                e.Actioner is null ? "" : $"Duyệt bởi {e.Actioner.DisplayName}"
            ]);
            ZaloMessageBuilder messageBuilder = new ZaloMessageBuilder()
                .WithContent("Chào mừng thành viên mới ")
                .AddMention(e.Member)
                .AppendContent("!")
                .AddAttachment(ZaloAttachment.FromData("image.png", canvas));
            await e.Group.SendMessageAsync(messageBuilder);
        }

        static async Task<byte[]> TryCreateCanvas(string bgUrl, string avatar1Url, string avatar2Url, string[] messages)
        {
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
            int random;
            if (string.IsNullOrEmpty(bgUrl))
            {
                random = Random.Shared.Next(0, maxBgLength) + 1;
                bg = new MagickImage(@$"Backgrounds\{random}.png");
            }
            else
            {
                bg = new MagickImage(await httpClient.GetByteArrayAsync(bgUrl));
                random = 0;
            }
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
                        Font = @"Fonts\VNF-Comic Sans.ttf",
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
                        Font = @"Fonts\Pacifico-Regular.ttf",
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
                        Font = @"Fonts\VNF-Comic Sans.ttf",
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
                        Font = @"Fonts\Pacifico-Regular.ttf",
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
                        Font = @"Fonts\VNF-Comic Sans.ttf",
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
            int x = (int)((bg.Width - width) / 2);
            int y = (int)((bg.Height - height) / 2);
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

        static void InitializeClient()
        {
            JsonNode node = JsonNode.Parse(File.ReadAllText("credentials.json")) ?? throw new Exception("Missing credentials.json");
            clientBuilder = clientBuilder
                .WithCredential(new LoginCredential(node["cookies"].Deserialize(SourceGenerationContext.Default.ListZaloCookie) ?? new List<ZaloCookie>(), node["imei"]?.GetValue<string>() ?? "", node["user_agent"]?.GetValue<string>() ?? ""))
                .SetAPIType(node["type"]?.GetValue<int>() ?? 0)
                .SetAPIVersion(node["client_version"]?.GetValue<int>() ?? 0)
                .SetMaxTimeCache(1000 * 60 * 60 * 3)
                .SetComputerName(node["computer_name"]?.GetValue<string>() ?? "");
        }
    }
}
