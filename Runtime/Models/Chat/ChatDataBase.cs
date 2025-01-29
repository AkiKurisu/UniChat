using Unity.Collections;
using Unity.Sentis;
namespace UniChat
{
    public class ChatDataBase : ChatGraph, IEmbeddingDataBase
    {
        public int Count => Edges.Count;
        
        public ChatDataBase(int dim = 512) : base(dim) { }
        
        public ChatDataBase(string filePath) : base(filePath) { }
        
        public bool AddEdge(uint inputHash, Embedding inputEmbedding, uint outputHash, Embedding outputEmbedding)
        {
            AddEmbedding(inputEmbedding);
            AddEmbedding(outputEmbedding);
            Edges.Add(new Edge
            {
                input = new Port(inputHash),
                output = new Port(outputHash)
            });
            return true;
        }
        
        public TensorFloat[] AllocateTensors()
        {
            int length = Count;
            var inputTextEmbeddings = TensorFloat.Zeros(new TensorShape(length, Dim));
            var outputTextEmbeddings = TensorFloat.Zeros(new TensorShape(length, Dim));
            for (int i = 0; i < length; ++i)
            {
                for (int j = 0; j < Dim; ++j)
                {
                    inputTextEmbeddings[i, j] = Embeddings[2 * i * Dim + j];
                    outputTextEmbeddings[i, j] = Embeddings[(2 * i + 1) * Dim + j];
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
                    Embeddings.AddRange(ptr, embedding.values.Length);
                }
            }
        }
        
        public void SetEmbedding(int index, Embedding embedding)
        {
            var slice = new NativeSlice<float>(Embeddings.AsArray(), index * Dim, Dim);
            slice.CopyFrom(embedding.values);
        }
        
        public void RemoveEdge(int index)
        {
            Edges.RemoveRange(index, 1);
            Embeddings.RemoveRange(index * Dim, 2 * Dim);
        }
        
        public uint GetOutput(int index)
        {
            return Edges[index].output.uniqueId;
        }
    }
}