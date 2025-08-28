using EHVN.ZaloBot.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace EHVN.ZaloBot.Functions.AI.Providers
{
    internal abstract class OpenRouterAIProvider : IAIProvider
    {
        public abstract long TokenLimit { get; }

        internal abstract string ModelName { get; }

        public abstract Task<long> CountTokensAsync(string text);

        public async Task<AIMessage> GetResponseAsync(List<AIMessage> messages)
        {
            JsonObject jsonContent = new JsonObject
            {
                { "model", ModelName },
                { "messages", JsonValue.Create(messages, SourceGenerationContext.Default.ListAIMessage) }
            };
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://openrouter.ai/api/v1/chat/completions"),
                Headers =
                {
                    Authorization = new AuthenticationHeaderValue("Bearer", BotConfig.ReadonlyConfig.OpenRouterAPIKey)
                },
                Content = new StringContent(jsonContent.ToJsonString(SourceGenerationContext.Default.Options), Encoding.UTF8, "application/json")
            };
            HttpResponseMessage response = await ChatAI.httpClient.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();
            JsonArray? arr = JsonNode.Parse(responseContent.Trim().Trim(Environment.NewLine.ToCharArray()))?["choices"]?.AsArray();
            if (arr is null || arr.Count == 0)
                return AIMessage.CreateAssistantMessage();
            AIMessage? aiMessage = arr.Last(e => e?["message"]?["role"]?.GetValue<string>() == "assistant")?["message"]?.Deserialize(SourceGenerationContext.Default.AIMessage);
            if (aiMessage is null)
                return AIMessage.CreateAssistantMessage();
            return AIMessage.CreateAssistantMessage(aiMessage.Content);

        }
    }
}
