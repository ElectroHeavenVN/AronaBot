using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHVN.AronaBot.Functions.AI.Providers
{
    internal class DeepSeekV3_2024 : OpenRouterAIProvider
    {
        public override long TokenLimit => throw new NotImplementedException();

        internal override string ModelName => "deepseek/deepseek-chat-v3-0324:free";

        public override Task<long> CountTokensAsync(string text)
        {
            throw new NotImplementedException();
        }
    }
}
