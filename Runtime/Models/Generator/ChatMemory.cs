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
    public abstract class ChatMemory : IChatMemory, ILLMRequest
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
        public IEnumerable<IMessage> History => GetAllMessages();
        protected readonly MessageFormatter formatter = new();
        public ChatMemory() { }
        public ChatMemory(ChatHistory chatHistory)
        {
            ChatHistory = chatHistory;
        }
        public abstract IEnumerable<ChatMessage> GetAllMessages();
        public abstract IEnumerable<ChatMessage> GetMessages(MessageRole messageRole);
        public abstract string GetMemoryContext();
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
        public override string GetMemoryContext()
        {
            formatter.UserPrefix = ChatHistory.UserName;
            formatter.BotPrefix = ChatHistory.BotName;
            return formatter.Format(this);
        }

        public override IEnumerable<ChatMessage> GetAllMessages()
        {
            return ChatHistory.history;
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
                return pool;
            }
            finally
            {
                ListPool<ChatMessage>.Release(pool);
            }
        }
        public override string GetMemoryContext()
        {
            formatter.UserPrefix = ChatHistory.UserName;
            formatter.BotPrefix = ChatHistory.BotName;
            return formatter.Format(GetAllMessages());
        }
        public override IEnumerable<ChatMessage> GetAllMessages()
        {
            var pool = ListPool<ChatMessage>.Get();
            try
            {
                pool.AddRange(ChatHistory.history);
                int numMessages = Math.Min(pool.Count, WindowSize * 2);
                pool.RemoveRange(0, pool.Count - numMessages);
                return pool;
            }
            finally
            {
                ListPool<ChatMessage>.Release(pool);
            }
        }
    }
}