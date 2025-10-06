using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace EHVN.AronaBot.Functions.AI.CharacterAI
{
    internal class CharacterAIIPCClient : IDisposable
    {
        internal class ChatSession
        {
            [JsonInclude, JsonPropertyName("chat_id")]
            public string ChatId { get; set; } = "";

            [JsonInclude, JsonPropertyName("character_id")]
            public string CharacterId { get; set; } = "";
        }

        Process pyCharacterAIWrapper;
        NamedPipeServerStream serverPipe;

        internal CharacterAIIPCClient(string token)
        {
            long id = Random.Shared.NextInt64();
            pyCharacterAIWrapper = Process.Start(new ProcessStartInfo
            {
                FileName = @"Tools\PyCharacterAI\EHVN.PyCharacterAI.Wrapper.exe",
                Arguments = $"{id}",
                EnvironmentVariables = { ["CHARACTERAI_TOKEN"] = token },
            }) ?? throw new Exception("Failed to start EHVN.PyCharacterAI.Wrapper.");
            serverPipe = new NamedPipeServerStream("EHVN.PyCharacterAI.Wrapper_" + id, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
        }

        internal async Task<ChatSession> CreateChatAsync(string characterID, CancellationToken cancellationToken = default)
        {
            if (!serverPipe.IsConnected)
                await serverPipe.WaitForConnectionAsync(cancellationToken);
            JsonObject jobj = new JsonObject()
            {
                ["command"] = "new_chat",
                ["data"] = new JsonObject
                {
                    ["character_id"] = characterID,
                }
            };
            byte[] buffer = Encoding.UTF8.GetBytes(jobj.ToJsonString());
            await serverPipe.WriteAsync(buffer, cancellationToken);
            serverPipe.WaitForPipeDrain();
            string jsonResponse = "";
            do
            {
                byte[] readBuffer = new byte[4096];
                int bytesRead = await serverPipe.ReadAsync(readBuffer, cancellationToken);
                jsonResponse += Encoding.UTF8.GetString(readBuffer, 0, bytesRead);
            }
            while (!serverPipe.IsMessageComplete);
            return JsonSerializer.Deserialize(jsonResponse, AISGContext.Default.ChatSession) ?? throw new Exception("Failed to parse chat session response.");
        }

        internal async Task<ChatSession> GetChatAsync(string chatID, CancellationToken cancellationToken = default)
        {
            if (!serverPipe.IsConnected)
                await serverPipe.WaitForConnectionAsync(cancellationToken);
            JsonObject jobj = new JsonObject()
            {
                ["command"] = "get_chat",
                ["data"] = new JsonObject
                {
                    ["chat_id"] = chatID,
                }
            };
            byte[] buffer = Encoding.UTF8.GetBytes(jobj.ToJsonString());
            await serverPipe.WriteAsync(buffer, cancellationToken);
            serverPipe.WaitForPipeDrain();
            string jsonResponse = "";
            do
            {
                byte[] readBuffer = new byte[4096];
                int bytesRead = await serverPipe.ReadAsync(readBuffer, cancellationToken);
                jsonResponse += Encoding.UTF8.GetString(readBuffer, 0, bytesRead);
            }
            while (!serverPipe.IsMessageComplete);
            return JsonSerializer.Deserialize(jsonResponse, AISGContext.Default.ChatSession) ?? throw new Exception("Failed to parse chat session response.");
        }

        internal async Task<string> SendMessageAsync(ChatSession chatSession, string message, CancellationToken cancellationToken = default)
        {
            if (!serverPipe.IsConnected)
                await serverPipe.WaitForConnectionAsync(cancellationToken);
            JsonObject jobj = new JsonObject()
            {
                ["command"] = "send_message",
                ["data"] = new JsonObject
                {
                    ["chat_id"] = chatSession.ChatId,
                    ["character_id"] = chatSession.CharacterId,
                    ["message"] = message,
                }
            };
            byte[] buffer = Encoding.UTF8.GetBytes(jobj.ToJsonString());
            await serverPipe.WriteAsync(buffer, cancellationToken);
            serverPipe.WaitForPipeDrain();
            string jsonResponse = "";
            do
            {
                byte[] readBuffer = new byte[4096];
                int bytesRead = await serverPipe.ReadAsync(readBuffer, cancellationToken);
                jsonResponse += Encoding.UTF8.GetString(readBuffer, 0, bytesRead);
            }
            while (!serverPipe.IsMessageComplete);
            JsonObject responseObj = JsonNode.Parse(jsonResponse)?.AsObject() ?? throw new Exception("Failed to parse send message response.");
            return responseObj["message"]?.GetValue<string>() ?? throw new Exception("No message in send message response.");
        }

        internal async Task SendCloseAsync(CancellationToken cancellationToken = default)
        {
            if (!serverPipe.IsConnected)
                await serverPipe.WaitForConnectionAsync(cancellationToken);
            JsonObject jobj = new()
            {
                ["command"] = "close",
            };
            byte[] buffer = Encoding.UTF8.GetBytes(jobj.ToJsonString());
            await serverPipe.WriteAsync(buffer, cancellationToken);
        }

        public void Dispose()
        {
            try
            {
                if (serverPipe.IsConnected)
                    serverPipe.Disconnect();
                serverPipe.Dispose();
            }
            catch { }
            try
            {
                if (!pyCharacterAIWrapper.HasExited)
                    pyCharacterAIWrapper.Kill();
                pyCharacterAIWrapper.Dispose();
            }
            catch { }
        }
    }
}
