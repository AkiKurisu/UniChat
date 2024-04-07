using System.Collections.Generic;
namespace Kurisu.UniChat
{
    public interface IChatHistoryQuery
    {
        bool TryGetBotMessage(uint hash, out ChatMessage message);
        /// <summary>
        /// User contents
        /// </summary>
        /// <returns></returns>
        IEnumerable<ChatMessage> GetUserMessages();
        /// <summary>
        /// Bot contents
        /// </summary>
        /// <returns></returns>
        IEnumerable<ChatMessage> GetBotMessages();
        /// <summary>
        /// Get constructed history context
        /// </summary>
        /// <returns></returns>
        string GetHistoryContext();
    }
}
