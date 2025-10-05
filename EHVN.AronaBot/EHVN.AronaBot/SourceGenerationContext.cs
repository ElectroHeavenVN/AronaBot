using EHVN.AronaBot.Config;
using EHVN.ZepLaoSharp.Net.LongPolling;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EHVN.AronaBot
{
    [JsonSerializable(typeof(LongPollingClientOptions))]
    [JsonSerializable(typeof(ReadonlyConfig))]
    [JsonSerializable(typeof(WritableConfig))]
    [JsonSerializable(typeof(DBOConfig))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
        static SourceGenerationContext()
        {
            Default = new SourceGenerationContext(new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }
    }
}
