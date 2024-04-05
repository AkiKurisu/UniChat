using Unity.Sentis;
namespace Kurisu.UniChat
{
    public class ChatDataBase : ChatGraph, IEmbeddingDataBase
    {
        public int Count => edges.Count;
        public ChatDataBase(int dim = 512) : base(dim) { }
        public ChatDataBase(string filePath) : base(filePath) { }
        public bool AddEmbedding(uint inputHash, Embedding inputEmbedding, uint outputHash, Embedding outputEmbedding)
        {
            AddEmbedding(inputEmbedding);
            AddEmbedding(outputEmbedding);
            edges.Add(new()
            {
                input = new(inputHash),
                output = new(outputHash)
            });
            return true;
        }
        public TensorFloat[] AllocateTensors()
        {
            int length = Count;
            var inputTextEmbeddings = TensorFloat.Zeros(new TensorShape(length, dim));
            var outputTextEmbeddings = TensorFloat.Zeros(new TensorShape(length, dim));
            for (int i = 0; i < length; ++i)
            {
                for (int j = 0; j < dim; ++j)
                {
                    inputTextEmbeddings[i, j] = embeddings[2 * i * dim + j];
                    outputTextEmbeddings[i, j] = embeddings[(2 * i + 1) * dim + j];
                }
            }
            return new TensorFloat[2] { inputTextEmbeddings, outputTextEmbeddings };
        }
        private void AddEmbedding(Embedding embedding)
        {
            unsafe
            {
                fixed (float* ptr = embedding.values)
                {
                    embeddings.AddRange(ptr, embedding.values.Length);
                }
            }
        }
        public void SetEmbedding(int index, Embedding embedding)
        {
            for (int i = 0; i < dim; ++i)
            {
                embeddings[index * dim + i] = embedding.values[i];
            }
        }
        public uint GetOutput(int index)
        {
            return edges[index].output.uniqueId;
        }
    }
}