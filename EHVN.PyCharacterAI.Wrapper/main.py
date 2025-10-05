import asyncio
import json
from PyCharacterAI import get_client
from PyCharacterAI.exceptions import SessionClosedError
from PyCharacterAI.types import Chat
import time
import os
import win32pipe, win32file

pipeName = r'\\.\pipe\EHVN.PyCharacterAI.Wrapper'

if (len(os.sys.argv) < 2):
    print("Usage: EHVN.PyCharacterAI.Wrapper <id>")
    exit(1)

async def main():
    id = os.sys.argv[1]
    token = os.getenv("CHARACTERAI_TOKEN")

    client = await get_client(token=token)
    me = await client.account.fetch_me()
    print(f"Authenticated as @{me.username}")

    try:
        while True:
            try:
                handle = win32file.CreateFile(
                    pipeName + "_" + id,
                    win32file.GENERIC_READ | win32file.GENERIC_WRITE,
                    0, None,
                    win32file.OPEN_EXISTING,
                    0, None
                )
                break
            except Exception:
                time.sleep(1)

        while True:
            _, resp = win32file.ReadFile(handle, 64*1024)
            print("Received from C#: ", resp)
            resp_str = resp.decode('utf-8')
            resp_json = json.loads(resp_str)
            command = resp_json.get('command')
            data = resp_json.get('data')
            match (command):
                case 'new_chat':
                    character_id = data.get('character_id')
                    chat, greeting_message = await client.chat.create_chat(character_id, False)
                    chat_data = {
                        'chat_id': chat.chat_id,
                        'character_id': character_id
                    }
                    json_data = json.dumps(chat_data).encode('utf-8')
                    print("Sending to C#: ", json_data)
                    win32file.WriteFile(handle, json_data)
                case 'get_chat':
                    chat_id = data.get('chat_id')
                    chat = await client.chat.fetch_chat(chat_id)
                    chat_data = {
                        'chat_id': chat.chat_id,
                        'character_id': chat.character_id
                    }
                    json_data = json.dumps(chat_data).encode('utf-8')
                    print("Sending to C#: ", json_data)
                    win32file.WriteFile(handle, json_data)
                case 'send_message':
                    character_id = data.get('character_id')
                    chat_id = data.get('chat_id')
                    message = data.get('message')
                    answer = await client.chat.send_message(character_id, chat_id, message)
                    message_data = {
                        'message': answer.get_primary_candidate().text,
                    }
                    json_data = json.dumps(message_data).encode('utf-8')
                    print("Sending to C#: ", json_data)
                    win32file.WriteFile(handle, json_data)
                case 'close':
                    break
                case _:
                    print("Unknown command: ", command)
                    win32file.WriteFile(handle, b'{"error": "Unknown command"}')
        await client.close_session()
    except KeyboardInterrupt:
        await client.close_session()
        
asyncio.run(main())