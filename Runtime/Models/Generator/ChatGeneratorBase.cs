using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Kurisu.NGDS;
using Kurisu.NGDS.AI;
namespace Kurisu.UniChat
{
    /// <summary>
    /// Chatbot style generator that shares data structures with NGDS.AI
    /// </summary>
    public abstract class ChatGeneratorBase : IGenerator, ILLMInput
    {
        public string BotName { get; set; } = "Bot";
        string ILLMInput.OutputCharacter => BotName;
        public string UserName { get => characters[0]; set => characters[0] = value; }
        private readonly string[] characters = new string[1] { "User" };
        public IEnumerable<string> InputCharacters => characters;
        public readonly List<DialogueParam> history = new();
        IEnumerable<DialogueParam> ILLMInput.History => history;
        public string Context { get; set; }
        public void AppendUserDialogue(string content)
        {
            history.Add(new DialogueParam(UserName, content));
        }
        public void AppendBotDialogue(string content)
        {
            history.Add(new DialogueParam(BotName, content));
        }
        public void ClearHistory()
        {
            history.Clear();
        }
        public void RemoveLastInput()
        {
            for (int i = history.Count - 1; i >= 0; --i)
            {
                if (history[i].character == characters[0])
                {
                    history.RemoveAt(i);
                    return;
                }
            }
        }
        private readonly ChatFormatter formatter = new();
        public abstract UniTask<bool> Generate(GenerateContext context, CancellationToken ct);
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
                history = new() { internalData = new string[length][] }
            };
            for (int i = 0; i < length; ++i)
            {
                var array = new string[2];
                array[0] = history[2 * i].content;
                if (2 * i + 1 < historyCount)
                {
                    array[1] = history[2 * i + 1].content;
                }
                else
                {
                    array[1] = string.Empty;
                }
                session.history.internalData[i] = array;
            }
            return session;
        }
        public void LoadSession(ChatSession session)
        {
            Context = session.context;
            UserName = session.name1;
            BotName = session.name2;
            foreach (var array in session.history.internalData)
            {
                history.Add(new DialogueParam(session.name1, array[0]));
                if (!string.IsNullOrEmpty(array[1]))
                    history.Add(new DialogueParam(session.name2, array[1]));
            }
        }
    }
}