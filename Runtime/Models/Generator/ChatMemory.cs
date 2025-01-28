using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UniChat.LLMs;
using UnityEngine.Pool;
namespace UniChat.Memory
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
        
        protected readonly MessageFormatter DefaultFormatter = new();

        private MessageFormatter _formatter;
        
        [JsonIgnore]
        public IEnumerable<IMessage> Messages => GetAllMessages();
        
        [JsonIgnore]
        public MessageFormatter Formatter { get => _formatter ?? GetDefaultFormatter(); set => _formatter = value; }

        protected ChatMemory() { }

        protected ChatMemory(ChatHistory chatHistory)
        {
            ChatHistory = chatHistory;
        }
        
        public abstract IEnumerable<ChatMessage> GetAllMessages();
        
        public abstract IEnumerable<ChatMessage> GetMessages(MessageRole messageRole);
        
        public virtual string GetMemoryContext()
        {
            return Formatter.Format(GetAllMessages());
        }
        
        protected virtual MessageFormatter GetDefaultFormatter()
        {
            DefaultFormatter.UserPrefix = ChatHistory.UserName;
            DefaultFormatter.BotPrefix = ChatHistory.BotName;
            return DefaultFormatter;
        }
    }
    
    /// <summary>
    /// Buffered chat memory
    /// </summary>
    public sealed class ChatBufferMemory : ChatMemory
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
            return ChatHistory.History;
        }
    }
    
    /// <summary>
    /// Chat memory exclude user messages
    /// </summary>
    public sealed class ToolUseMemory : ChatMemory
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
            foreach (var message in ChatHistory.History)
            {
                if (message.Role != MessageRole.User) yield return message;
            }
        }
        
        protected override MessageFormatter GetDefaultFormatter()
        {
            DefaultFormatter.UserPrefix = "";
            DefaultFormatter.BotPrefix = "";
            DefaultFormatter.SystemPrefix = "";
            return DefaultFormatter;
        }
    }
    
    /// <summary>
    /// Buffered chat memory with window
    /// </summary>
    public sealed class ChatWindowBufferMemory : ChatMemory
    {
        /// <summary>
        /// Window size per role
        /// </summary>
        /// <value></value>
        public int WindowSize { get; set; } = 5;
        
        public ChatWindowBufferMemory() { }
        
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
                pool.AddRange(ChatHistory.History);
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