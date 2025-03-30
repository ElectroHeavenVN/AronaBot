using System.Text.Json.Serialization;
using ZepLaoSharp.Auth;

namespace ZaloBot
{
    [JsonSerializable(typeof(List<ZaloCookie>))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}
