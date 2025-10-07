using EHVN.AronaBot.Config;
using EHVN.ZepLaoSharp;
using EHVN.ZepLaoSharp.Entities;
using EHVN.ZepLaoSharp.Events;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using static EHVN.AronaBot.Functions.AI.CharacterAI.CharacterAIIPCClient;

namespace EHVN.AronaBot.Functions.AI.CharacterAI
{
    //TODO: find a way to have multiple chat sessions with the same character
    internal static class CAIMain
    {
        static CharacterAIIPCClient cClient = new CharacterAIIPCClient(BotConfig.ReadonlyConfig.CharacterAI.Token);
        static ChatSession chatSession;
        static bool initialized;
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        internal static async Task InitializeAsync()
        {
            if (initialized)
                return;
            initialized = true;
            chatSession = await cClient.GetChatAsync(BotConfig.ReadonlyConfig.CharacterAI.ChatID);
        }

        static bool ShouldRespond(ZaloGroupMessage groupMessage)
        {
            if (groupMessage.IsCurrent)
                return false;
            if (!BotConfig.WritableConfig.CharacterAIEnabledGroupIDs.Contains(groupMessage.Group.ID))
                return false;
            string content = groupMessage.Content?.Text ?? "";
            if (content.StartsWith(BotConfig.WritableConfig.Prefix))
                return false;
            if (groupMessage.MentionCurrentUser)
                return true;
            if (groupMessage.Quote is not null && groupMessage.Quote.IsCurrent)
                return true;
            return false;
        }

        internal static async Task GroupMessageReceived(ZaloClient sender, GroupMessageReceivedEventArgs args)
        {
            if (!ShouldRespond(args.GroupMessage))
                return;
            string content = args.GroupMessage.Content?.Text ?? "";
            if (string.IsNullOrWhiteSpace(content))
                return;
            await semaphoreSlim.WaitAsync();
            try
            {
                await args.GroupMessage.MarkAsReadAsync();
                await args.Group.TriggerTypingAsync();
                await InitializeAsync();
                //content = $"{args.Member.DisplayName} {args.Member.Mention}: {content}";
                Dictionary<string, string> mentions = [];
                foreach (var mention in args.GroupMessage.Mentions.Where(m => m.Type != ZaloMentionType.Everyone))
                {
                    string name = content[mention.Position..(mention.Position + mention.Length)];
                    mentions.Add(name, Formatter.Mention(mention.UserID));
                }
                for (int i = args.GroupMessage.Mentions.Length - 1; i >= 0; i--)
                {
                    ZaloMention? mention = args.GroupMessage.Mentions[i];
                    content = content.Remove(mention.Position, mention.Length).Insert(mention.Position, Formatter.Mention(mention.UserID));
                }
                JsonObject jobj = new JsonObject()
                {
                    ["message"] = content,
                    ["mentions"] = JsonValue.Create(mentions, SourceGenerationContext.Default.DictionaryStringString)
                };
                content = await cClient.SendMessageAsync(chatSession, jobj.ToJsonString(new JsonSerializerOptions(SourceGenerationContext.Default.Options) { WriteIndented = false }));
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
                {Formatter.Bold(Formatter.FontSizeLarge(Formatter.ColorGreen("アロナちゃん")))}
                {content}

                {Formatter.Bold(Formatter.FontSizeSmall(Formatter.ColorGreen("Character") + "." + Formatter.ColorOrange("AI") + " intergration by " + Formatter.ColorYellow("ElectroHeavenVN")))}
                """.Replace("\r", "");
        }
    }
}
