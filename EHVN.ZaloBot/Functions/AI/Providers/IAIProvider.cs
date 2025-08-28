using System.Collections.Generic;
using System.Threading.Tasks;

namespace EHVN.ZaloBot.Functions.AI.Providers
{
    //TODO: implement function calling for different providers
    internal interface IAIProvider
    {
        Task<long> CountTokensAsync(string text);

        long TokenLimit { get; }

        Task<AIMessage> GetResponseAsync(List<AIMessage> messages);
    }
}
