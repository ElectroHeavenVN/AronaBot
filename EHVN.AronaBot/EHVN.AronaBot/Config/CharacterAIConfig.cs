using System.Text.Json.Serialization;

namespace EHVN.AronaBot.Config
{
    internal class CharacterAIConfig
    {
        [JsonInclude, JsonPropertyName("Token")]
        internal string Token { get; set; } = "";

        [JsonInclude, JsonPropertyName("ChatID")]
        internal string ChatID { get; set; } = "";

        [JsonInclude, JsonPropertyName("SystemPrompt")]
        internal string SystemPrompt { get; set; } = "";
    }
}
