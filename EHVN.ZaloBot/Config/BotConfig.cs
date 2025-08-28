using System.IO;
using System.Text.Json;

namespace EHVN.ZaloBot.Config
{
    internal static class BotConfig
    {
        internal static ReadonlyConfig ReadonlyConfig { get; private set; } = JsonSerializer.Deserialize(File.ReadAllText(@"Data\readonly-config.json"), SourceGenerationContext.Default.ReadonlyConfig) ?? new ReadonlyConfig();

        internal static WritableConfig WritableConfig { get; private set; } = JsonSerializer.Deserialize(File.ReadAllText(@"Data\writable-config.json"), SourceGenerationContext.Default.WritableConfig) ?? new WritableConfig();

        internal static void Save()
        {
            File.WriteAllText(@"Data\writable-config.json", JsonSerializer.Serialize(WritableConfig, SourceGenerationContext.Default.WritableConfig));
        }

        internal static void Reload()
        {
            ReadonlyConfig = JsonSerializer.Deserialize(File.ReadAllText(@"Data\readonly-config.json"), SourceGenerationContext.Default.ReadonlyConfig) ?? new ReadonlyConfig();
            WritableConfig = JsonSerializer.Deserialize(File.ReadAllText(@"Data\writable-config.json"), SourceGenerationContext.Default.WritableConfig) ?? new WritableConfig();
        }

        internal static long[] GetAllAdminIDs() => [.. ReadonlyConfig.AdminIDs];
    }
}
