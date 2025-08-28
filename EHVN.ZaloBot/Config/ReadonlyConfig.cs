using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EHVN.ZaloBot
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

        [JsonInclude, JsonPropertyName("TaiKhoanNRO")]
        internal string NROAccount { get; set; } = "";

        [JsonInclude, JsonPropertyName("MatKhauNRO")]
        internal string NROPassword { get; set; } = "";
    }
}
