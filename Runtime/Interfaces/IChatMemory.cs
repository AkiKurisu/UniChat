using System.Collections.Generic;
namespace Kurisu.UniChat
{
    public interface IChatMemory
    {
        IEnumerable<ChatMessage> GetMessages(MessageRole messageRole);
        /// <summary>
        /// Get constructed memory context
        /// </summary>
        /// <returns></returns>
        string GetMemoryContext();
    }
}