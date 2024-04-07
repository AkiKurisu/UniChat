using System;
using System.Collections.Generic;
using Kurisu.NGDS;
using Kurisu.NGDS.AI;
namespace Kurisu.UniChat
{
    public class ChatHistoryContext : IChatHistoryQuery, ILLMInput
    {
        public string BotName { get; set; } = "Bot";
        public string UserName { get; set; } = "User";
        public string Context { get; set; }
        private readonly ChatFormatter formatter = new();
        public readonly List<ChatMessage> history = new();
        #region LLM Input Adapt
        string ILLMInput.OutputCharacter => BotName;
        IEnumerable<string> ILLMInput.InputCharacters
        {
            get
            {
                yield return UserName;
            }
        }
        IEnumerable<IMessage> ILLMInput.History => history;
        #endregion
        /// <summary>
        /// Append user input message to update history context
        /// </summary>
        /// <param name="content"></param>
        public void AppendUserMessage(string content)
        {
            history.Add(new()
            {
                character = UserName,
                characterId = ChatMessage.User,
                content = content,
                id = 0
            });
        }
        /// <summary>
        /// Append bot answered message to update history context
        /// </summary>
        /// <param name="content"></param>
        /// <param name="hash"></param>
        public void AppendBotMessage(string content, uint hash)
        {
            history.Add(new()
            {
                character = BotName,
                characterId = ChatMessage.Bot,
                content = content,
                id = hash
            });
        }
        public bool TryGetLastBotMessage(out ChatMessage message)
        {
            for (int i = history.Count - 1; i >= 0; --i)
            {
                if (history[i].characterId == ChatMessage.Bot)
                {
                    message = history[i];
                    return true;
                }
            }
            message = null;
            return false;
        }
        public bool TryGetLastUserDialogue(out ChatMessage message)
        {
            for (int i = history.Count - 1; i >= 0; --i)
            {
                if (history[i].characterId == ChatMessage.User)
                {
                    message = history[i];
                    return true;
                }
            }
            message = null;
            return false;
        }
        public IEnumerable<ChatMessage> GetUserMessages()
        {
            foreach (var message in history)
            {
                if (message.characterId == ChatMessage.User)
                {
                    yield return message;
                }
            }
        }
        public IEnumerable<ChatMessage> GetBotMessages()
        {
            foreach (var message in history)
            {
                if (message.characterId == ChatMessage.Bot)
                {
                    yield return message;
                }
            }
        }
        public void ClearHistory()
        {
            history.Clear();
        }
        public void RemoveLastInput()
        {
            for (int i = history.Count - 1; i >= 0; --i)
            {
                if (history[i].characterId == ChatMessage.User)
                {
                    history.RemoveAt(i);
                    return;
                }
            }
        }
        public string GetHistoryContext()
        {
            return formatter.Format(this);
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
                array[0] = history[2 * i].content;
                ids[0] = history[2 * i].id;
                if (2 * i + 1 < historyCount)
                {
                    array[1] = history[2 * i + 1].content;
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
                    characterId = (byte)(session.name1 == UserName ? 0 : 1),
                    content = contents[0],
                    id = ids[0]
                });
                if (!string.IsNullOrEmpty(contents[1]))
                    history.Add(new()
                    {
                        character = session.name2,
                        characterId = (byte)(session.name2 == UserName ? 0 : 1),
                        content = contents[1],
                        id = ids[1]
                    });
            }
        }

        public bool TryGetBotMessage(uint hash, out ChatMessage message)
        {
            foreach (var botMg in GetBotMessages())
            {
                if (botMg.id == hash)
                {
                    message = botMg; return true;
                }
            }
            message = null;
            return false;
        }
    }
}