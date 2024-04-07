using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
namespace Kurisu.UniChat
{
    public interface IGenerator
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
        /// <summary>
        /// Call context generation
        /// </summary>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        UniTask<bool> Generate(GenerateContext context, CancellationToken ct);
    }
}