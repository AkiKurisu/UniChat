using System.Collections.Generic;
namespace Kurisu.UniChat
{
    public interface IEmbeddingTable : IReadOnlyList<IEmbeddingEntry>
    {
        /// <summary>
        /// Get embedding entry from table
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="entry"></param>
        /// <returns></returns>
        bool TryGetEntry(uint hash, out IEmbeddingEntry entry);
        /// <summary>
        /// Add new entry to table
        /// </summary>
        /// <param name="entry">entry</param>
        /// <returns></returns>
        bool AddEntry(IEmbeddingEntry entry);
    }
    public interface IPersistHandlerFactory<T>
    {
        /// <summary>
        /// Create a persist handler
        /// </summary>
        /// <returns></returns>
        IPersistEmbeddingValue<T> CreatePersistHandler();
    }
    public interface IPersistEmbeddingValue<T>
    {
        /// <summary>
        /// Persist value and embedding to saveable entry
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="value"></param>
        /// <param name="embedding"></param>
        /// <param name="entry"></param>
        /// <returns></returns>
        bool Persist(uint hash, T value, Embedding embedding, out IEmbeddingEntry entry);
    }
    public interface IPersistEmbeddingValue<T, K>
    {
        /// <summary>
        /// Persist value and embedding to saveable entry
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="value"></param>
        /// <param name="embedding"></param>
        /// <param name="entry"></param>
        /// <returns></returns>
        bool Persist(uint hash, T value, Embedding embedding, out IEmbeddingEntry<K> entry);
    }
    public interface ISerializable
    {
        void Save(string filePath);
        void Load(string filePath);
    }
    public interface IEmbeddingEntry
    {
        uint Hash { get; }
        Embedding Embedding { get; }
        object Value { get; }
    }
    public interface IEmbeddingEntry<T> : IEmbeddingEntry
    {
        T TValue { get; }
    }
}