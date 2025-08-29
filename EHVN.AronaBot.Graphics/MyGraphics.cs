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

        internal static byte[] AddWatermark(byte[] image, string prefix)
        {
            using MagickImage img = new MagickImage(image);
            using MagickImage watermark = new MagickImage();
            MagickReadSettings settings = new MagickReadSettings
            {
                BackgroundColor = MagickColors.Transparent,
                TextGravity = Gravity.North,
                FontPointsize = 20,
                Width = img.Width,
                Height = img.Height,
                FontStyle = FontStyleType.Bold,
                FontWeight = FontWeight.Normal,
                FillColor = MagickColors.Red,
            };
            watermark.Read("caption:AronaBot - Developed by ElectroHeavenVN", settings);
            img.Composite(watermark, 0, 0, CompositeOperator.Over);
            settings.FontPointsize = 16;
            settings.FontStyle = FontStyleType.Normal;
            settings.FillColor = MagickColors.Orange;
            watermark.Read(string.Format("caption:Chat {0}about để biết thêm thông tin", prefix), settings);
            img.Composite(watermark, 0, 22, CompositeOperator.Over);
            return img.ToByteArray(MagickFormat.Png);
        }

        internal static async Task<byte[]> TryCreateCanvas(string bgUrl, string avatar1Url, string avatar2Url, string[] messages)
        {
            for (int i = 0; i < messages.Length; i++)
                messages[i] = ReplaceUnicodeChars(messages[i]);
            byte[] bgData = [];
            if (string.IsNullOrEmpty(bgUrl))
            {
                string[] backgrounds = Directory.GetFiles(@"Data\Backgrounds\", "*.png");
                string bgFilePath = backgrounds[Random.Shared.Next(0, backgrounds.Length)];
                bgData = File.ReadAllBytes(bgFilePath);
            }
            try
            {
                try
                {
                    if (!string.IsNullOrEmpty(bgUrl))
                        bgData = await httpClient.GetByteArrayAsync(bgUrl);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
                return await CreateCanvas(bgData, avatar1Url, avatar2Url, messages);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                try
                {
                    return await CreateCanvas(bgData, "", "", messages);
                }
                catch (Exception ex2)
                {
                    Console.Error.WriteLine(ex2);
                    return [];
                }
            }
        }

        static async Task<byte[]> CreateCanvas(byte[] bgData, string avatar1Url, string avatar2Url, string[] messages)
        {
            MagickImage bg = new MagickImage(bgData);
            CropFill(bg, 900, 300);
            bg.BrightnessContrast(new Percentage(-25), new Percentage(0));
            bg.HasAlpha = false;
            byte[] data;
            if (!string.IsNullOrEmpty(avatar1Url))
                data = await httpClient.GetByteArrayAsync(avatar1Url);
            else if (File.Exists(@"Data\default.png"))
                data = File.ReadAllBytes(@"Data\default.png");
            else
                throw new Exception();
            var avatar1 = new MagickImage(data);
            avatar1.Resize(150, 150);
            avatar1 = RoundImage(avatar1);
            bg.Composite(avatar1, 50, (int)(bg.Height / 2 - avatar1.Height / 2), CompositeOperator.Over);
            avatar1.Dispose();
            if (!string.IsNullOrEmpty(avatar2Url))
                data = await httpClient.GetByteArrayAsync(avatar2Url);
            else if (File.Exists(@"Data\default.png"))
                data = File.ReadAllBytes(@"Data\default.png");
            else
                throw new Exception();
            var avatar2 = new MagickImage(data);
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
                .Text(10, 15, "AronaBot by ElectroHeavenVN")
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
    }
}
