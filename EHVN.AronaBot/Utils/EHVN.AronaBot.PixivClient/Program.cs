using PixivCS.Api;
using PixivCS.Models.Illust;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EHVN.AronaBot.PixivClient
{
    internal class Program
    {
        static PixivAppApi pixivClient = new PixivAppApi();

        static async Task<int> Main(string[] args)
        {
            if (args.Length < 2 || string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]) || !long.TryParse(args[1], out _))
            {
                Console.Error.WriteLine(
                    """
                    Usage: EHVN.AronaBot.PixivClient <refresh token> <id>
                    Output image(s) will be written to the standard output.
                    Format: [Images count (int32)][Image 1 size (int64)][Image 1 data][Image 2 size (int64)][Image 2 data]...[Title (string)][Caption (string)]
                    """
                );
                return 0;
            }
            await pixivClient.AuthAsync(args[0]);
            IllustDetail illustDetail = await pixivClient.GetIllustDetailAsync(args[1]);
            if (illustDetail.Illust?.ImageUrls is null)
                return 1;
            Stream stream = Console.OpenStandardOutput();
            BinaryWriter binaryWriter = new BinaryWriter(stream);
            if (illustDetail.Illust.MetaPages.Count <= 0)
            {
                if (illustDetail.Illust.ImageUrls.Original is null && illustDetail.Illust.ImageUrls.Large is null)
                    return 1;
                binaryWriter.Write(1);
                Stream imgStream = await pixivClient.GetImageStreamAsync(illustDetail.Illust.ImageUrls.Original ?? illustDetail.Illust.ImageUrls.Large);
                imgStream.Position = 0;
                binaryWriter.Write(imgStream.Length);
                imgStream.CopyTo(stream);
            }
            else
            {
                binaryWriter.Write(illustDetail.Illust.MetaPages.Count);
                for (int i = 0; i < illustDetail.Illust.MetaPages.Count; i++)
                {
                    MetaPage metaPage = illustDetail.Illust.MetaPages[i];
                    if (metaPage.ImageUrls is null)
                        continue;
                    if (metaPage.ImageUrls.Original is null && metaPage.ImageUrls.Large is null)
                        continue;
                    Stream imgStream = await pixivClient.GetImageStreamAsync(metaPage.ImageUrls.Original ?? metaPage.ImageUrls.Large!);
                    imgStream.Position = 0;
                    binaryWriter.Write(imgStream.Length);
                    imgStream.CopyTo(stream);
                }
            }
            //Console.WriteLine(illustDetail.Illust.Title);
            //Console.WriteLine(illustDetail.Illust.Caption);

            //binaryWriter.Write(Encoding.UTF8.GetByteCount(illustDetail.Illust.Title ?? ""));
            binaryWriter.Write(illustDetail.Illust.Title ?? "");
            //binaryWriter.Write(Encoding.UTF8.GetByteCount(illustDetail.Illust.Caption ?? ""));
            binaryWriter.Write(illustDetail.Illust.Caption ?? "");
            return 0;
        }
    }
}
