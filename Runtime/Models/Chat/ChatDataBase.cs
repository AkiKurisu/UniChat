using Unity.Collections;
using Unity.Sentis;
namespace UniChat
{
    public class ChatDataBase : ChatGraph, IEmbeddingDataBase
    {
        public int Count => edges.Count;
        public ChatDataBase(int dim = 512) : base(dim) { }
        public ChatDataBase(string filePath) : base(filePath) { }
        public bool AddEdge(uint inputHash, Embedding inputEmbedding, uint outputHash, Embedding outputEmbedding)
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
            var slice = new NativeSlice<float>(embeddings.AsArray(), index * dim, dim);
            slice.CopyFrom(embedding.values);
        }
        public void RemoveEdge(int index)
        {
            edges.RemoveRange(index, 1);
            embeddings.RemoveRange(index * dim, 2 * dim);
        }
        public uint GetOutput(int index)
        {
            return edges[index].output.uniqueId;
        }
    }
}