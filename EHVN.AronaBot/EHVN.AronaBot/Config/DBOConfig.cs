using System.Text.Json.Serialization;

namespace EHVN.AronaBot.Config
{
    internal class DBOConfig
    {
        [JsonInclude, JsonPropertyName("TaiKhoan")]
        internal string Account { get; set; } = "";

        [JsonInclude, JsonPropertyName("MatKhau")]
        internal string Password { get; set; } = "";

        [JsonInclude, JsonPropertyName("MayChu")]
        internal string ServerAddress { get; set; } = "";

        [JsonInclude, JsonPropertyName("Cong")]
        internal ushort ServerPort { get; set; }
    }
}
