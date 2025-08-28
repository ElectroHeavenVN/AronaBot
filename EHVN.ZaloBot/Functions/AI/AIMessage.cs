using System.Text.Json.Serialization;

namespace EHVN.ZaloBot.Functions.AI
{
    internal class AIMessage
    {
        [JsonInclude, JsonPropertyName("role")]
        internal string Role { get; set; } = "";

        [JsonInclude, JsonPropertyName("content")]
        internal string Content { get; set; } = "";

        [JsonConstructor]
        internal AIMessage() { }

        internal AIMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }
        public override string ToString()
        {
            return $"{Role}: {Content}";
        }

        internal static AIMessage CreateAssistantMessage(string content = "") => new AIMessage("assistant", content);

        internal static AIMessage CreateUserMessage(string content = "") => new AIMessage("user", content);

        internal static AIMessage CreateSystemMessage(string content = "") => new AIMessage("system", content);
    }
}
