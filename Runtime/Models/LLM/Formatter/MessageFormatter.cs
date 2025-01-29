using System;
using System.Collections.Generic;
using System.Text;
namespace UniChat.LLMs
{
    public class MessageFormatter
    {
        public string UserPrefix { get; set; } = "User";
        
        public string BotPrefix { get; set; } = "Bot";
        
        public string SystemPrefix { get; set; } = "System";
        
        private readonly StringBuilder _stringBuilder = new();
        
        public string GetPrefix(MessageRole role)
        {
            return role switch
            {
                MessageRole.User => UserPrefix,
                MessageRole.Bot => BotPrefix,
                MessageRole.System => SystemPrefix,
                _ => throw new ArgumentOutOfRangeException(nameof(role)),
            };
        }
        
        /// <summary>
        /// Format llm request
        /// </summary>
        /// <param name="llmInput"></param>
        /// <returns></returns>
        public string Format(IChatRequest llmInput)
        {
            _stringBuilder.Clear();
            if (!string.IsNullOrEmpty(llmInput.Context))
            {
                _stringBuilder.AppendLine(llmInput.Context);
            }
            foreach (var param in llmInput.Messages)
            {
                if (string.IsNullOrEmpty(GetPrefix(param.Role)))
                    _stringBuilder.AppendLine(param.Content);
                else
                    _stringBuilder.AppendLine($"{GetPrefix(param.Role)}: {param.Content}");
            }
            return _stringBuilder.ToString();
        }
        
        /// <summary>
        /// Format messages history
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        public string Format(IEnumerable<IMessage> messages)
        {
            _stringBuilder.Clear();
            foreach (var param in messages)
            {
                if (string.IsNullOrEmpty(GetPrefix(param.Role)))
                    _stringBuilder.AppendLine(param.Content);
                else
                    _stringBuilder.AppendLine($"{GetPrefix(param.Role)}: {param.Content}");
            }
            return _stringBuilder.ToString();
        }
    }
}
