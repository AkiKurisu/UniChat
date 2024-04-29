using System;
using System.Collections.Generic;
using Kurisu.UniChat.LLMs;
using Newtonsoft.Json;
using UnityEngine.Pool;
namespace Kurisu.UniChat.Memory
{
    /// <summary>
    /// Memory defines how to load message from <see cref="UniChat.ChatHistory"/>
    /// </summary>
    public abstract class ChatMemory : IChatMemory, IChatRequest
    {
        [JsonIgnore]
        public ChatHistory ChatHistory { get; set; }
        [JsonIgnore]
        public string BotName { get => ChatHistory.BotName; set => ChatHistory.BotName = value; }
        [JsonIgnore]
        public string UserName { get => ChatHistory.UserName; set => ChatHistory.UserName = value; }
        [JsonIgnore]
        public string Context { get => ChatHistory.Context; set => ChatHistory.Context = value; }
        [JsonIgnore]
        public IEnumerable<IMessage> Messages => GetAllMessages();
        protected readonly MessageFormatter defaultFormatter = new();
        [JsonIgnore]
        public MessageFormatter Formatter { get; set; }
        public ChatMemory() { }
        public ChatMemory(ChatHistory chatHistory)
        {
            ChatHistory = chatHistory;
        }
        public abstract IEnumerable<ChatMessage> GetAllMessages();
        public abstract IEnumerable<ChatMessage> GetMessages(MessageRole messageRole);
        public virtual string GetMemoryContext()
        {
            if (Formatter == null)
            {
                defaultFormatter.UserPrefix = ChatHistory.UserName;
                defaultFormatter.BotPrefix = ChatHistory.BotName;
                return defaultFormatter.Format(Messages);
            }
            else
            {
                return Formatter.Format(Messages);
            }
        }
    }
    /// <summary>
    /// Buffered chat memory
    /// </summary>
    public class ChatBufferMemory : ChatMemory
    {
        public ChatBufferMemory() : base() { }
        public ChatBufferMemory(ChatHistory chatHistory) : base(chatHistory)
        {
        }

        public override IEnumerable<ChatMessage> GetMessages(MessageRole messageRole)
        {
            return ChatHistory.GetMessages(messageRole);
        }

        public override IEnumerable<ChatMessage> GetAllMessages()
        {
            return ChatHistory.history;
        }
    }
    /// <summary>
    /// Chat memory exclude user messages
    /// </summary>
    public class ToolUseMemory : ChatMemory
    {
        public ToolUseMemory() : base() { }
        public ToolUseMemory(ChatHistory chatHistory) : base(chatHistory)
        {
        }

        public override IEnumerable<ChatMessage> GetMessages(MessageRole messageRole)
        {
            return ChatHistory.GetMessages(messageRole);
        }
        public override IEnumerable<ChatMessage> GetAllMessages()
        {
            foreach (var message in ChatHistory.history)
            {
                if (message.Role != MessageRole.User) yield return message;
            }
        }
    }
    /// <summary>
    /// Buffered chat memory with window
    /// </summary>
    public class ChatWindowBufferMemory : ChatMemory
    {
        /// <summary>
        /// Window size per role
        /// </summary>
        /// <value></value>
        public int WindowSize { get; set; } = 5;
        public ChatWindowBufferMemory() : base() { }
        public ChatWindowBufferMemory(ChatHistory chatHistory) : base(chatHistory)
        {
        }
        public override IEnumerable<ChatMessage> GetMessages(MessageRole messageRole)
        {
            var pool = ListPool<ChatMessage>.Get();
            try
            {
                pool.AddRange(ChatHistory.GetMessages(messageRole));
                int numMessages = Math.Min(pool.Count, WindowSize);
                pool.RemoveRange(0, pool.Count - numMessages);
                foreach (var message in pool)
                {
                    yield return message;
                }
            }
            finally
            {
                ListPool<ChatMessage>.Release(pool);
            }
        }
        public override IEnumerable<ChatMessage> GetAllMessages()
        {
            var pool = ListPool<ChatMessage>.Get();
            try
            {
                pool.AddRange(ChatHistory.history);
                int numMessages = Math.Min(pool.Count, WindowSize * 2);
                pool.RemoveRange(0, pool.Count - numMessages);
                foreach (var message in pool)
                {
                    yield return message;
                }
            }
            finally
            {
                ListPool<ChatMessage>.Release(pool);
            }
        }
    }
}