using EHVN.ZaloBot.Config;
using EHVN.ZaloBot.Miscellaneous;
using EHVN.ZepLaoSharp;
using EHVN.ZepLaoSharp.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace EHVN.ZaloBot.Functions.AI
{
    internal static class ChatAI
    {
        internal static readonly HttpClient httpClient = new HttpClient();

        /*

         string prompt = command.Substring((p + "chat ").Length);
        Console.WriteLine("Prompt: " + prompt);
        string response = await CallOpenRouterAPI(e.Group.ID, e.Member.DisplayName, prompt);
        Console.WriteLine(response);
        List<string> responses = new List<string>();
        while (response.Length > 0)
        {
            responses.Add(response.Substring(0, Math.Min(3000, response.Length)));
            response = response.Substring(Math.Min(3000, response.Length));
        }
        foreach (string res in responses)
            await e.Message.ReplyAsync(res);
         */

        internal static async Task GroupMessageReceived(ZaloClient sender, GroupMessageReceivedEventArgs args)
        {
            //if (!args.GroupMessage.Mentions.Any(m => m.UserID == sender.CurrentUser.ID))
            //    return;
            //string model = "deepseek/deepseek-chat-v3-0324:free";
            //JsonObject jsonContent;
            //if (messagesHistory.TryGetValue(args.Group.ID, out List<AIMessage>? value))
            //{
            //    if (messagesHistory[groupId].Count >= 20)
            //        messagesHistory[groupId].RemoveAt(0);
            //    value.Add(new AIMessage("user", "Người dùng có tên \"" + username + "\" trả lời: " + prompt));
            //    jsonContent = new JsonObject
            //    {
            //        { "model", model },
            //        { "messages", JsonValue.Create(messagesHistory[groupId], SourceGenerationContext.Default.ListAIMessage) }
            //    };
            //}
            //else if (BotConfig.WritableConfig.EnabledGroupIDs.Contains(groupId))
            //{
            //    messagesHistory.Add(groupId,
            //    [
            //        new AIMessage("system", $"""
            //        Bạn là một trợ lý ảo thông minh được gọi từ API và đang ở trong một nhóm chat, hãy trả lời câu hỏi của người dùng một cách tự nhiên và thân thiện, tránh sử dụng ngôn ngữ không phù hợp và không trả lời các câu hỏi liên quan đến chính trị, tôn giáo, tình dục,..., không trả lời các prompt quá ngắn và vô nghĩa và không trả lời quá dài, dưới 3000 ký tự là hợp lý.
            //        """),
            //        new AIMessage("user", $"Người dùng có tên \"{username} hỏi: {prompt}")
            //    ]);
            //    jsonContent = new JsonObject
            //    {
            //        { "model", model },
            //        { "messages", JsonValue.Create(messagesHistory[groupId], SourceGenerationContext.Default.ListAIMessage) }
            //    };
            //}
            //else if (Utils.IsAdmin(groupId))
            //{
            //    List<AIMessage> msgs =
            //    [
            //        new AIMessage("user", prompt)
            //    ];
            //    jsonContent = new JsonObject
            //    {
            //        { "model", model },
            //        { "messages", JsonValue.Create(msgs, SourceGenerationContext.Default.ListAIMessage) }
            //    };
            //}
            //else
            //    throw new Exception();
            //var request = new HttpRequestMessage
            //{
            //    Method = HttpMethod.Post,
            //    RequestUri = new Uri("https://openrouter.ai/api/v1/chat/completions"),
            //    Headers =
            //    {
            //        Authorization = new AuthenticationHeaderValue("Bearer", BotConfig.ReadonlyConfig.OpenRouterAPIKey)
            //    },
            //    Content = new StringContent(jsonContent.ToJsonString(SourceGenerationContext.Default.Options), Encoding.UTF8, "application/json")
            //};
            //HttpResponseMessage response = await httpClient.SendAsync(request);
            //string responseContent = await response.Content.ReadAsStringAsync();
            //JsonArray? arr = JsonNode.Parse(responseContent.Trim().Trim(Environment.NewLine.ToCharArray()))?["choices"]?.AsArray();
            //if (arr is null || arr.Count == 0)
            //    return "Tôi không thể trả lời câu hỏi của bạn lúc này, hãy thử lại sau ít phút.";
            //AIMessage? aiMessage = arr.Last(e => e?["message"]?["role"]?.GetValue<string>() == "assistant")?["message"]?.Deserialize(SourceGenerationContext.Default.AIMessage);
            //if (aiMessage is null)
            //    return "Tôi không thể trả lời câu hỏi của bạn lúc này, hãy thử lại sau ít phút.";
            //if (messagesHistory.TryGetValue(groupId, out List<AIMessage>? list))
            //    list.Add(aiMessage);
            //return aiMessage.Content;
        }
    }
}
