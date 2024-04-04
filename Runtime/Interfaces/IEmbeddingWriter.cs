namespace Kurisu.UniChat
{
    public interface IEmbeddingWriter
    {
        void WriteEmbedding(Embedding embedding);
        void UpdateEmbeddings();
    }
}