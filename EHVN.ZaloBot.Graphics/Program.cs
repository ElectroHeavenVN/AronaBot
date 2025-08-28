namespace EHVN.ZaloBot.Graphics
{
    internal class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine(
                    """
                    Usage: EHVN.ZaloBot.Graphics <mode> [options]
                    Modes:
                      add-watermark <image-path> <prefix>
                      create-canvas <bg-url> <avatar1-url> <avatar2-url> <messages>
                    Output image will be written to the standard output.
                    """
                );
                return 0;
            }
            string mode = args[0].ToLowerInvariant();
            Stream stream = Console.OpenStandardOutput();
            switch (mode)
            {
                case "add-watermark":
                    if (args.Length < 3)
                    {
                        Console.Error.WriteLine("Usage: add-watermark <image-path> <prefix>");
                        return 0;
                    }
                    string imagePath = args[1];
                    string prefix = args[2];
                    byte[] watermarkedImage = MyGraphics.AddWatermark(File.ReadAllBytes(imagePath), prefix);
                    stream.Write(watermarkedImage);
                    stream.Flush();
                    return 0;
                case "create-canvas":
                    if (args.Length < 6)
                    {
                        Console.Error.WriteLine("Usage: create-canvas <bg-url> <avatar1-url> <avatar2-url> <messages>");
                        return 0;
                    }
                    string bgUrl = args[1];
                    string avatar1Url = args[2];
                    string avatar2Url = args[3];
                    string[] messages = args[4..];
                    byte[] canvas = MyGraphics.TryCreateCanvas(bgUrl, avatar1Url, avatar2Url, messages).GetAwaiter().GetResult();
                    stream.Write(canvas);
                    stream.Flush();
                    return 0;
            }
            Console.Error.WriteLine($"Unknown mode: {mode}");
            return 1;
        }
    }
}
