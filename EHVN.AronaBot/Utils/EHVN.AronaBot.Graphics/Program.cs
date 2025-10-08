namespace EHVN.AronaBot.Graphics
{
    internal class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine(
                    """
                    Usage: EHVN.AronaBot.Graphics <mode> [options]
                    Modes:
                      create-canvas <bg path> <bg url> <avatar1 url> <avatar2 url> <messages... (more than 1)>
                    Output image will be written to the standard output.
                    """
                );
                return 0;
            }
            string mode = args[0].ToLowerInvariant();
            Stream stream = Console.OpenStandardOutput();
            switch (mode)
            {
                case "create-canvas":
                    if (args.Length < 7)
                    {
                        Console.Error.WriteLine("Missing arguments for create-canvas mode.");
                        return 1;
                    }
                    string bgPath = args[1];
                    string bgUrl = args[2];
                    string avatar1Url = args[3];
                    string avatar2Url = args[4];
                    string[] messages = args[5..];
                    var canvas = MyGraphics.TryCreateCanvas(bgPath, bgUrl, avatar1Url, avatar2Url, messages).GetAwaiter().GetResult();
                    canvas.CopyTo(stream);
                    stream.Flush();
                    return 0;
            }
            Console.Error.WriteLine($"Unknown mode: {mode}");
            return 1;
        }
    }
}
