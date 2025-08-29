using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EHVN.AronaBot.Config
{
    internal class WritableConfig
    {
        [JsonInclude, JsonPropertyName("Prefix")]
        internal string Prefix { get; set; } = "";

        [JsonInclude, JsonPropertyName("IDNhomKichHoat")]
        internal List<long> EnabledGroupIDs { get; set; } = [];

        [JsonInclude, JsonPropertyName("IDNhomThongBaoGame")]
        internal List<long> DBONotifGroupIDs { get; set; } = [];

        [JsonInclude, JsonPropertyName("IDNguoiDungBoQua")]
        internal List<long> DisabledUserIDs { get; set; } = [];
    }
}
