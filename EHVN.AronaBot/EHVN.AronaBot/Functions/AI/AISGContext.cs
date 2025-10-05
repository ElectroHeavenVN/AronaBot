using EHVN.AronaBot.Functions.AI.CharacterAI;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EHVN.AronaBot.Functions.AI
{
    [JsonSerializable(typeof(CharacterAIIPCClient.ChatSession))]
    internal partial class AISGContext : JsonSerializerContext
    {
        static AISGContext()
        {
            Default = new AISGContext(CreateJsonSerializerOptions(Default));
        }

        static JsonSerializerOptions CreateJsonSerializerOptions(AISGContext defaultContext)
        {
            var options = new JsonSerializerOptions(defaultContext.GeneratedSerializerOptions!)
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
            };
            return options;
        }
    }
}
