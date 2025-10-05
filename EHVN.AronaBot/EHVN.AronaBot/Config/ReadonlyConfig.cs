using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EHVN.AronaBot.Config
{
    internal class ReadonlyConfig
    {
        [JsonInclude, JsonPropertyName("OpenRouterAPIKey")]
        internal string OpenRouterAPIKey { get; set; } = ""; 

        [JsonInclude, JsonPropertyName("IDAdmin")]
        internal List<long> AdminIDs { get; set; } = [];

        [JsonInclude, JsonPropertyName("TenNguoiDungSpotify")]
        internal string SpotifyUsername { get; set; } = "";

        [JsonInclude, JsonPropertyName("TokenSpotify")]
        internal string SpotifyToken { get; set; } = "";

        [JsonInclude, JsonPropertyName("SoundCloudClientID")]
        internal string SoundCloudClientID { get; set; } = "";

        [JsonInclude, JsonPropertyName("CharacterAIToken")]
        internal string CharacterAIToken { get; set; } = "";

        [JsonInclude, JsonPropertyName("CharacterAIChatID")]
        internal string CharacterAIChatID { get; set; } = "";

        //TODO: listen to multiple servers

        [JsonInclude, JsonPropertyName("NRO")]
        internal DBOConfig DBO { get; set; } = new DBOConfig();
    }
}
