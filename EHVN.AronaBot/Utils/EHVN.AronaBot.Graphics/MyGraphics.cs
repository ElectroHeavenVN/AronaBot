using ImageMagick;
using ImageMagick.Drawing;
using ImageMagick.Formats;

namespace EHVN.AronaBot
{
    internal class MyGraphics
    {
        static HttpClient httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(10) };
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

        internal static async Task<Stream> TryCreateCanvas(string bgPath, string bgUrl, string avatar1Url, string avatar2Url, string[] messages)
        {
            for (int i = 0; i < messages.Length; i++)
                messages[i] = ReplaceUnicodeChars(messages[i]);
            Stream background1;
            Stream background2 = new MemoryStream();
            Stream avatar1 = await GetAvatar(avatar1Url);
            Stream avatar2 = await GetAvatar(avatar2Url);
            try
            {
                if (File.Exists(bgPath))
                    background1 = File.OpenRead(bgPath);
                else
                    background1 = new MemoryStream();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                background1 = new MemoryStream();
            }
            try
            {
                if (!string.IsNullOrEmpty(bgUrl))
                {
                    Stream stream = await httpClient.GetStreamAsync(bgUrl);
                    await stream.CopyToAsync(background2);
                    background2.Position = 0;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                background2 = new MemoryStream();
            }
            try
            {
                if (background1.Length == 0)
                    Console.Error.WriteLine("Background 1 is empty.");
                if (background2.Length == 0)
                    Console.Error.WriteLine("Background 2 is empty.");
                if (avatar1.Length == 0)
                    Console.Error.WriteLine("Avatar 1 is empty.");
                if (avatar2.Length == 0)
                    Console.Error.WriteLine("Avatar 2 is empty.");
                return CreateCanvas(background1, background2, avatar1, avatar2, messages);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return new MemoryStream();
            }
        }

        static async Task<Stream> GetAvatar(string avatar1Url)
        {
            if (string.IsNullOrEmpty(avatar1Url))
            {
                if (File.Exists(@"Data\default.png"))
                    return File.OpenRead(@"Data\default.png");
                throw new FileNotFoundException("Default avatar not found.");
            }
            try
            {
                Stream stream = await httpClient.GetStreamAsync(avatar1Url);
                MemoryStream memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                return memoryStream;
            }
            catch
            {
                if (File.Exists(@"Data\default.png"))
                    return File.OpenRead(@"Data\default.png");
                throw;
            }
        }

        static Stream CreateCanvas(Stream background1, Stream background2, Stream avatar1, Stream avatar2, string[] messages)
        {
            using MagickImage img = new MagickImage(MagickColors.Black, 900, 300) { HasAlpha = false };
            try
            {
                if (background1.Length > 0)
                {
                    using MagickImage bg1 = new MagickImage(background1) { HasAlpha = false };
                    CropFill(bg1, 900, 300);
                    bg1.BrightnessContrast(new Percentage(-25), new Percentage(0));
                    img.Composite(bg1, 0, 0, CompositeOperator.Over);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
            try
            {
                if (background2.Length > 0)
                {
                    using MagickImage bg2 = new MagickImage(background2) { HasAlpha = false };
                    CropFill(bg2, 900, 300);
                    bg2.BrightnessContrast(new Percentage(-25), new Percentage(0));
                    img.Composite(bg2, 0, 0, CompositeOperator.Over);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
            try
            {
                if (avatar1.Length > 0)
                {
                    using MagickImage avt1 = new MagickImage(avatar1);
                    avt1.Resize(150, 150);
                    RoundImage(avt1);
                    img.Composite(avt1, 50, (int)(img.Height / 2 - avt1.Height / 2), CompositeOperator.Over);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
            try
            {
                if (avatar2.Length > 0)
                {
                    using MagickImage avt2 = new MagickImage(avatar2);
                    avt2.Resize(150, 150);
                    RoundImage(avt2);
                    img.Composite(avt2, (int)(img.Width - 50 - avt2.Width), (int)(img.Height / 2 - avt2.Height / 2), CompositeOperator.Over);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
            for (int i = 0; i < messages.Length; i++)
            {
                string message = messages[i];
                if (string.IsNullOrEmpty(message))
                    continue;
                using MagickImage text = new MagickImage();
                if (i == 0)
                {
                    MagickReadSettings settings = new MagickReadSettings
                    {
                        BackgroundColor = MagickColors.Transparent,
                        Width = img.Width - 150 - 150 - 100,
                        TextGravity = Gravity.North,
                        FontPointsize = 30,
                        FontStyle = FontStyleType.Normal,
                        FontWeight = FontWeight.Normal,
                        FillColor = MagickColors.White,
                        Font = @"Data\Fonts\VNF-Comic Sans.ttf",
                        FontFamily = "VNF-Comic Sans"
                    };
                    text.Read("caption:" + message, settings);
                    img.Composite(text, 50 + 150, 60, CompositeOperator.Over);
                }
                else if (i == 1)
                {
                    MagickReadSettings settings = new MagickReadSettings
                    {
                        BackgroundColor = MagickColors.Transparent,
                        Width = img.Width - 150 - 150 - 100,
                        TextGravity = Gravity.North,
                        FontPointsize = 35,
                        FontStyle = FontStyleType.Bold,
                        FontWeight = FontWeight.Bold,
                        FillColor = MagickColors.Yellow,
                        Font = @"Data\Fonts\Pacifico-Regular.ttf",
                        FontFamily = "Pacifico"
                    };
                    text.Read("caption:" + message, settings);
                    img.Composite(text, 50 + 150, 90, CompositeOperator.Over);
                }
                else if (i == 2)
                {
                    MagickReadSettings settings = new MagickReadSettings
                    {
                        BackgroundColor = MagickColors.Transparent,
                        Width = img.Width - 150 - 150 - 100,
                        TextGravity = Gravity.North,
                        FontPointsize = 30,
                        FontStyle = FontStyleType.Normal,
                        FontWeight = FontWeight.Normal,
                        FillColor = MagickColors.White,
                        Font = @"Data\Fonts\VNF-Comic Sans.ttf",
                        FontFamily = "VNF-Comic Sans"
                    };
                    text.Read("caption:" + message, settings);
                    img.Composite(text, 50 + 150, 145, CompositeOperator.Over);
                }
                else if (i == 3)
                {
                    MagickReadSettings settings = new MagickReadSettings
                    {
                        BackgroundColor = MagickColors.Transparent,
                        Width = img.Width - 150 - 150 - 100,
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
                    text.Read("caption:" + message, settings);
                    img.Composite(text, 50 + 150, 180, CompositeOperator.Over);
                }
                else if (i == 4)
                {
                    MagickReadSettings settings = new MagickReadSettings
                    {
                        BackgroundColor = MagickColors.Transparent,
                        Width = img.Width - 150 - 150 - 100,
                        Height = img.Height,
                        FontPointsize = 20,
                        FontStyle = FontStyleType.Bold,
                        FontWeight = FontWeight.Bold,
                        TextGravity = Gravity.South,
                        Font = @"Data\Fonts\VNF-Comic Sans.ttf",
                        FontFamily = "VNF-Comic Sans",
                        FillColor = MagickColors.White
                    };
                    text.Read("caption:" + message, settings);
                    img.Composite(text, 50 + 150, -10, CompositeOperator.Over);
                }
            }
            new Drawables()
                .FontPointSize(15)
                .Font("Arial")
                .FillColor(MagickColors.LightYellow)
                .Text(10, 15, "ZepLaoSharp - Developed by ElectroHeavenVN")
                .Draw(img);
            MemoryStream memoryStream = new MemoryStream();
            img.Write(memoryStream, MagickFormat.Jpg);
            memoryStream.Position = 0;
            return memoryStream;
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

        static void RoundImage(MagickImage image)
        {
            uint width = image.Width;
            uint height = image.Height;
            uint size = Math.Min(width, height);
            using MagickImage mask = new MagickImage(MagickColors.Transparent, size, size);
            var drawables = new Drawables()
                .FillColor(MagickColors.White)
                .Circle(size / 2, size / 2, size / 2, 0);
            drawables.Draw(mask);
            image.Alpha(AlphaOption.Set);
            image.Extent(size, size, Gravity.Center, MagickColors.Transparent);
            image.Composite(mask, CompositeOperator.CopyAlpha);
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
    }
}
