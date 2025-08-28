using EHVN.ZaloBot.Config;
using MetadataExtractor;
using MetadataExtractor.Formats.Bmp;
using MetadataExtractor.Formats.Gif;
using MetadataExtractor.Formats.Jpeg;
using MetadataExtractor.Formats.Png;
using MetadataExtractor.Formats.QuickTime;
using MetadataExtractor.Formats.WebP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHVN.ZaloBot.Miscellaneous
{
    internal static class Utils
    {
        static Dictionary<char, char> vietnameseAccentCharsDict = new Dictionary<char, char>()
        {
            { 'à', 'a' }, { 'á', 'a' }, { 'ả', 'a' }, { 'ã', 'a' }, { 'ạ', 'a' },
            { 'ă', 'a' }, { 'ắ', 'a' }, { 'ằ', 'a' }, { 'ẳ', 'a' }, { 'ẵ', 'a' }, { 'ặ', 'a' },
            { 'â', 'a' }, { 'ấ', 'a' }, { 'ầ', 'a' }, { 'ẩ', 'a' }, { 'ẫ', 'a' }, { 'ậ', 'a' },
            { 'đ', 'd' },
            { 'è', 'e' }, { 'é', 'e' }, { 'ẻ', 'e' }, { 'ẽ', 'e' }, { 'ẹ', 'e' },
            { 'ê', 'e' }, { 'ế', 'e' }, { 'ề', 'e' }, { 'ể', 'e' }, { 'ễ', 'e' }, { 'ệ', 'e' },
            { 'ì', 'i' }, { 'í', 'i' }, { 'ỉ', 'i' }, { 'ĩ', 'i' }, { 'ị', 'i' },
            { 'ò', 'o' }, { 'ó', 'o' }, { 'ỏ', 'o' }, { 'õ', 'o' }, { 'ọ', 'o' },
            { 'ô', 'o' }, { 'ố', 'o' }, { 'ồ', 'o' }, { 'ổ', 'o' }, { 'ỗ', 'o' }, { 'ộ', 'o' },
            { 'ơ', 'o' }, { 'ớ', 'o' }, { 'ờ', 'o' }, { 'ở', 'o' }, { 'ỡ', 'o' }, { 'ợ', 'o' },
            { 'ù', 'u' }, { 'ú', 'u' }, { 'ủ', 'u' }, { 'ũ', 'u' }, { 'ụ', 'u' },
            { 'ư', 'u' }, { 'ứ', 'u' }, { 'ừ', 'u' }, { 'ử', 'u' }, { 'ữ', 'u' }, { 'ự', 'u' },
            { 'ỳ', 'y' }, { 'ý', 'y' }, { 'ỷ', 'y' }, { 'ỹ', 'y' }, { 'ỵ', 'y' }
        };

        internal static bool IsAdmin(long userID) => BotConfig.GetAllAdminIDs().Contains(userID) || Program.client.CurrentUser.ID == userID;

        internal static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
        }

        internal static bool ContainsIgnoreAccent(this string self, string value, StringComparison comparisonType = StringComparison.CurrentCultureIgnoreCase)
        {
            if (string.IsNullOrEmpty(self) || string.IsNullOrEmpty(value))
                return false;
            string selfWithoutAccent = self.ReplaceVietnameseChars();
            string valueWithoutAccent = value.ReplaceVietnameseChars();
            return selfWithoutAccent.Contains(valueWithoutAccent, comparisonType);
        }

        internal static bool IsImage(byte[] imageData)
        {
            using MemoryStream stream = new MemoryStream(imageData);
            IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(stream);
            return directories.Any(directory =>
                directory is JpegDirectory ||
                directory is PngDirectory ||
                directory is WebPDirectory ||
                directory is GifHeaderDirectory ||
                directory is BmpHeaderDirectory);
        }
        
        internal static bool TryGetVideoMetadata(byte[] videoData, out int width, out int height, out long duration)
        {
            width = 0;
            height = 0;
            duration = 0;
            using MemoryStream stream = new MemoryStream(videoData);
            IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(stream);
            bool sizeSet = false;
            bool durationSet = false;
            foreach (var directory in directories)
                if (directory is QuickTimeTrackHeaderDirectory qtTrackHeaderDirectory && !sizeSet)
                {
                    width = qtTrackHeaderDirectory.GetInt32(QuickTimeTrackHeaderDirectory.TagWidth);
                    height = qtTrackHeaderDirectory.GetInt32(QuickTimeTrackHeaderDirectory.TagHeight);
                    sizeSet = true;
                }
                else if (directory is QuickTimeMovieHeaderDirectory qtMovieHeaderDirectory && !durationSet)
                {
                    duration = (long)((TimeSpan)qtMovieHeaderDirectory.GetObject(QuickTimeTrackHeaderDirectory.TagDuration)!).TotalMilliseconds;
                    durationSet = true;
                }
            return sizeSet && durationSet;
        }

        static string ReplaceVietnameseChars(this string text)
        {
            char[] chars = text.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!vietnameseAccentCharsDict.TryGetValue(chars[i], out char replacement))
                    continue;
                if (char.IsUpper(chars[i]))
                    chars[i] = char.ToUpper(replacement);
                else
                    chars[i] = char.ToLower(replacement);
            }
            return new string(chars);
        }
    }
}
