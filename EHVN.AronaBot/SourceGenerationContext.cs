using EHVN.AronaBot.Config;
using EHVN.AronaBot.Functions.AI;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EHVN.AronaBot
{
    [JsonSerializable(typeof(ReadonlyConfig))]
    [JsonSerializable(typeof(WritableConfig))]
    [JsonSerializable(typeof(DBOConfig))]
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
