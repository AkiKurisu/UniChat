using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
namespace Kurisu.UniChat
{
    public class GenerateContext
    {
        public IReadOnlyList<string> input;
        public int flag = 0;
        /// <summary>
        /// Generator output
        /// </summary>
        public string generatedContent;
        /// <summary>
        /// Final output
        /// </summary>
        public IEmbeddingEntry outputEntry;
        public GenerateContext(IReadOnlyList<string> input)
        {
            this.input = input;
        }
        /// <summary>
        /// Get final output value from entry
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T CastOutputValue<T>()
        {
            if (outputEntry is IEmbeddingEntry<T> tEntry) return tEntry.TValue;
            return (T)outputEntry.Value;
        }
        /// <summary>
        /// Get string value from entry if exists, else return generatedContent
        /// </summary>
        /// <returns></returns>
        public string CastStringValue()
        {
            if (outputEntry != null && outputEntry is IEmbeddingEntry<string> stringEntry) return stringEntry.TValue;
            return generatedContent;
        }
    }
    public interface IGenerator
    {
        /// <summary>
        /// User contents
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetUserContents();
        /// <summary>
        /// Bot contents
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetBotContents();
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