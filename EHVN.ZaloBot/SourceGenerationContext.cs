using EHVN.ZaloBot.Functions.AI;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EHVN.ZaloBot
{
    [JsonSerializable(typeof(ReadonlyConfig))]
    [JsonSerializable(typeof(WritableConfig))]
    [JsonSerializable(typeof(AIMessage))]
    [JsonSerializable(typeof(List<AIMessage>))]
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
