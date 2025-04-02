using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZaloBot
{
    internal class Config
    {
        internal static void LoadConfig()
        {
            Instance = JsonSerializer.Deserialize(File.ReadAllText(@"Data\config.json"), SourceGenerationContext.Default.Config) ?? new Config();
        }

        internal static Config Instance { get; set; } = new Config();

        [JsonPropertyName("PrefixMacDinh")]
        public string DefaultPrefix { get; set; } = "";

        [JsonPropertyName("Webhook")]
        public string[] Webhooks { get; set; } = [];

        [JsonPropertyName("OpenRouterAPIKey")]
        public string OpenRouterAPIKey { get; set; } = "";

        [JsonPropertyName("IDNhomKichHoat")]
        public long[] EnabledGroupIDs { get; set; } = [];

        [JsonPropertyName("BannerChaoMung")]
        public string WelcomeBannerMessage { get; set; } = "";

        [JsonPropertyName("BannerTamBiet")]
        public string LeaveBannerMessage { get; set; } = "";

        [JsonPropertyName("BannerThanhVienBiDuoi")]
        public string KickMemberBannerMessage { get; set; } = "";

        [JsonPropertyName("BannerThanhVienBiChan")]
        public string BanMemberBannerMessage { get; set; } = "";

        [JsonPropertyName("TinNhanChaoMung")]
        public string WelcomeMessage { get; set; } = "";

        [JsonPropertyName("TinNhanTamBiet")]
        public string LeaveMessage { get; set; } = "";

        [JsonPropertyName("TinNhanThanhVienBiDuoi")]
        public string KickMemberMessage { get; set; } = "";

        [JsonPropertyName("TinNhanThanhVienBiChan")]
        public string BanMemberMessage { get; set; } = "";
    }

}
