using EHVN.AronaBot.Config;
using EHVN.ZepLaoSharp;
using EHVN.ZepLaoSharp.Entities;
using EHVN.ZepLaoSharp.Events;
using System.Threading;
using System.Threading.Tasks;
using static EHVN.AronaBot.Functions.AI.CharacterAI.CharacterAIIPCClient;

namespace EHVN.AronaBot.Functions.AI.CharacterAI
{
    internal static class CAIMain
    {
        static CharacterAIIPCClient cClient = new CharacterAIIPCClient(BotConfig.ReadonlyConfig.CharacterAIToken);
        static ChatSession chatSession;
        static bool initialized;
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        internal static async Task InitializeAsync()
        {
            if (initialized)
                return;
            initialized = true;
            chatSession = await cClient.GetChatAsync(BotConfig.ReadonlyConfig.CharacterAIChatID);
        }

        static bool ShouldRespond(ZaloGroupMessage groupMessage)
        {
            if (groupMessage.IsCurrent)
                return false;
            if (groupMessage.MentionCurrentUser)
                return true;
            if (groupMessage.Quote is not null && groupMessage.Quote.IsCurrent)
                return true;
            return false;
        }

        internal static async Task GroupMessageReceived(ZaloClient sender, GroupMessageReceivedEventArgs args)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                await InitializeAsync();
                if (args.Group.ID != 8566925697964157802)
                    return;
                if (!ShouldRespond(args.GroupMessage))
                    return;
                await args.GroupMessage.SendSeenAsync();
                await args.Group.TriggerTypingAsync();
                await Task.Delay(2000);
                string content = args.GroupMessage.Content?.Text ?? "";
                if (string.IsNullOrWhiteSpace(content))
                    return;
                content = $"{args.Member.DisplayName} {args.Member.Mention}: {content}";
                content = await cClient.SendMessageAsync(chatSession, content);
                //await args.GroupMessage.ReplyAsync(BuildReply(content, args.Group.CurrentMember.Mention));
                await args.GroupMessage.ReplyAsync(BuildReply(content, args.Member.Mention));
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        static string BuildReply(string content, string mention)
        {
            return
                $"""
                {mention}
                {Formatter.Bold(Formatter.FontSizeLarge(Formatter.ColorGreen("Character") + Formatter.ColorYellow(".") + Formatter.ColorRed("AI")))}
                {content}
                """.Replace("\r", "");
        }
    }
}
