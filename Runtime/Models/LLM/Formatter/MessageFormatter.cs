using System;
using System.Collections.Generic;
using System.Text;
namespace Kurisu.UniChat.LLMs
{
    public class MessageFormatter
    {
        public string UserPrefix { get; set; } = "User";
        public string BotPrefix { get; set; } = "Bot";
        public string SystemPrefix { get; set; } = "System";
        private readonly StringBuilder stringBuilder = new();
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
        public string Format(ILLMRequest llmInput)
        {
            stringBuilder.Clear();
            if (!string.IsNullOrEmpty(llmInput.Context))
            {
                stringBuilder.AppendLine(llmInput.Context);
            }
            foreach (var param in llmInput.Messages)
            {
                if (string.IsNullOrEmpty(GetPrefix(param.Role)))
                    stringBuilder.AppendLine(param.Content);
                else
                    stringBuilder.AppendLine($"{GetPrefix(param.Role)}: {param.Content}");
            }
            stringBuilder.Append(llmInput.BotName);
            stringBuilder.Append(':');
            stringBuilder.Append('\n');
            return stringBuilder.ToString();
        }
        /// <summary>
        /// Format messages history
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        public string Format(IEnumerable<IMessage> messages)
        {
            stringBuilder.Clear();
            foreach (var param in messages)
            {
                if (string.IsNullOrEmpty(GetPrefix(param.Role)))
                    stringBuilder.AppendLine(param.Content);
                else
                    stringBuilder.AppendLine($"{GetPrefix(param.Role)}: {param.Content}");
            }
            return stringBuilder.ToString();
        }
    }
}
