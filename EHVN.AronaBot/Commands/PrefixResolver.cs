using EHVN.ZepLaoSharp.Commands;
using EHVN.ZepLaoSharp.Entities;
using EHVN.AronaBot.Config;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace EHVN.AronaBot
{
    internal class PrefixResolver : IPrefixResolver
    {
        public ValueTask<int> ResolvePrefixAsync(CommandsExtension extension, ZaloMessage message)
        {
            string? text = message.Content?.Text;
            if (text is null || string.IsNullOrWhiteSpace(text))
                return new ValueTask<int>(-1);
            else if (text.StartsWith('@' + extension.Client.CurrentUser.DisplayName, StringComparison.OrdinalIgnoreCase))
            {
                int mentionMeLength = extension.Client.CurrentUser.DisplayName.Length + 1; 
                int spacesCount = text.Skip(mentionMeLength).TakeWhile(c => c == ' ').Count();
                return new ValueTask<int>(mentionMeLength + spacesCount);
            }
            string prefix = BotConfig.WritableConfig.Prefix;
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                int spacesCount = text.Skip(prefix.Length).TakeWhile(c => c == ' ').Count();
                return new ValueTask<int>(prefix.Length + spacesCount);
            }
            return new ValueTask<int>(-1);
        }
    }
}
