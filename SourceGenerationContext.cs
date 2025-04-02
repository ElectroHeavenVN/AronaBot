using System.Text.Json.Serialization;
using ZepLaoSharp.Auth;

namespace ZaloBot
{
    [JsonSerializable(typeof(List<ZaloCookie>))]
    [JsonSerializable(typeof(Config))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}
