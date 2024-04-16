using System;
using System.Collections.Generic;
using UnityEngine.Pool;
namespace Kurisu.UniChat
{
    /// <summary>
    /// Define chat history data
    /// </summary>
    public class ChatHistory
    {
        public string BotName { get; set; } = "Bot";
        public string UserName { get; set; } = "User";
        public string Context { get; set; }
        public readonly List<ChatMessage> history = new();
        /// <summary>
        /// Append user input message to update history context
        /// </summary>
        /// <param name="content"></param>
        /// <param name="id"></param>
        public void AppendUserMessage(string content, uint? id = null)
        {
            history.Add(new()
            {
                character = UserName,
                Role = MessageRole.User,
                Content = content,
                id = id ?? XXHash.CalculateHash(content)
            });
        }
        /// <summary>
        /// Append bot answered message to update history context
        /// </summary>
        /// <param name="content"></param>
        /// <param name="id"></param>
        public void AppendBotMessage(string content, uint? id = null)
        {
            history.Add(new()
            {
                character = BotName,
                Role = MessageRole.Bot,
                Content = content,
                id = id ?? XXHash.CalculateHash(content)
            });
        }
        public bool TryGetLastMessage(MessageRole messageRole, out ChatMessage message)
        {
            var pool = ListPool<ChatMessage>.Get();
            message = null;
            try
            {
                pool.AddRange(GetMessages(messageRole));
                if (pool.Count > 0)
                {
                    message = pool[^1];
                    return true;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                ListPool<ChatMessage>.Release(pool);
            }
        }
        public IEnumerable<ChatMessage> GetMessages(MessageRole messageRole)
        {
            foreach (var message in history)
            {
                if (message.Role == messageRole)
                {
                    yield return message;
                }
            }
        }
        public void ClearHistory()
        {
            history.Clear();
        }
        public void RemoveLastUserMessage()
        {
            for (int i = history.Count - 1; i >= 0; --i)
            {
                if (history[i].Role == MessageRole.User)
                {
                    history.RemoveAt(i);
                    return;
                }
            }
        }
        public ChatSession SaveSession()
        {
            int historyCount = history.Count;
            int length = (int)Math.Ceiling((double)historyCount / 2);
            var session = new ChatSession
            {
                context = Context,
                name1 = UserName,
                name2 = BotName,
                history = new() { contents = new string[length][], ids = new uint[length][] }
            };
            for (int i = 0; i < length; ++i)
            {
                var array = new string[2];
                var ids = new uint[2];
                array[0] = history[2 * i].Content;
                ids[0] = history[2 * i].id;
                if (2 * i + 1 < historyCount)
                {
                    array[1] = history[2 * i + 1].Content;
                    ids[1] = history[2 * i + 1].id;
                }
                else
                {
                    array[1] = string.Empty;
                }
                session.history.contents[i] = array;
                session.history.ids[i] = ids;
            }
            return session;
        }
        public void LoadSession(ChatSession session)
        {
            Context = session.context;
            UserName = session.name1;
            BotName = session.name2;
            for (int i = 0; i < session.history.contents.Length; ++i)
            {
                var contents = session.history.contents[i];
                var ids = session.history.ids[i];
                history.Add(new()
                {
                    character = session.name1,
                    Role = session.name1 == UserName ? MessageRole.User : MessageRole.Bot,
                    Content = contents[0],
                    id = ids[0]
                });
                if (!string.IsNullOrEmpty(contents[1]))
                    history.Add(new()
                    {
                        character = session.name2,
                        Role = session.name2 == UserName ? MessageRole.User : MessageRole.Bot,
                        Content = contents[1],
                        id = ids[1]
                    });
            }
        }
    }
}