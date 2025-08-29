using EHVN.AronaBot.Functions.AI.Providers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EHVN.AronaBot.Functions.AI
{
    internal class ChatConversation
    {
        internal static List<ChatConversation> ActiveConversations { get; set; } = [];

        internal Queue<AIMessage> Messages { get; set; } = [];

        internal long ThreadID { get; set; }

        internal long TotalTokens { get; set; }

        internal IAIProvider AIProvider { get; set; }  //TODO: implement at least 1 ai provider for testing

        static AIMessage SystemMessage = new AIMessage("system", $"""
                                         Bạn là một trợ lý ảo thông minh được gọi từ API và đang ở trong một nhóm chat, hãy trả lời câu hỏi của người dùng một cách tự nhiên và thân thiện, tránh sử dụng ngôn ngữ không phù hợp và không trả lời các câu hỏi liên quan đến chính trị, tôn giáo, tình dục,..., không trả lời các prompt quá ngắn và vô nghĩa và không trả lời quá dài, dưới 3000 ký tự là hợp lý.
                                         """);

        ChatConversation(long threadID, IAIProvider aiProvider)
        {
            ThreadID = threadID;
            AIProvider = aiProvider;

            
        }

        internal async Task<string> GetResponseAsync(string userPrompt)
        {
            if (TotalTokens == 0)
                TotalTokens += await AIProvider.CountTokensAsync(SystemMessage.Content);
            TotalTokens += await AIProvider.CountTokensAsync(userPrompt);
            while (TotalTokens > AIProvider.TokenLimit)
            {
                AIMessage firstMsg = Messages.Dequeue();
                TotalTokens -= await AIProvider.CountTokensAsync(firstMsg.Content);
            }
            Messages.Enqueue(new AIMessage("user", userPrompt));
            List<AIMessage> msgs = [SystemMessage, ..Messages];
            var response = await AIProvider.GetResponseAsync(msgs);
            Messages.Enqueue(response);
            TotalTokens += await AIProvider.CountTokensAsync(response.Content);
            return response.Content;
        }

        internal static ChatConversation GetOrCreateConversation(long threadID, IAIProvider aiProvider)
        {
            var conversation = ActiveConversations.Find(c => c.ThreadID == threadID);
            if (conversation == null)
            {
                conversation = new ChatConversation(threadID, aiProvider);
                ActiveConversations.Add(conversation);
            }
            return conversation;
        }
    }
}
