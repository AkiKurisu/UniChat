using System.Collections.Generic;
using Kurisu.NGDS;
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
    public class ChatMessage : IMessage
    {
        public const byte User = 0;
        public const byte Bot = 1;
        public byte characterId;
        public string character;
        public string content;
        public uint id;
        public string Character => character;
        public string Content => content;
    }
}