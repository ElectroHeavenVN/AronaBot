using System.Text.Json.Serialization;

namespace ZaloBot
{
    internal class AIMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        public AIMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }
        public override string ToString()
        {
            return $"{Role}: {Content}";
        }
    }
}
