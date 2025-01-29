using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UniChat
{
    /// <summary>
    /// Graph only stores text embeddings, which is enough for chat, question-answer tasks.
    /// </summary>
    public class ChatGraph : IDisposable
    {
        /// <summary>
        /// Max count should lower than 1048 (according to NativeList max length).
        /// </summary>
        public readonly List<Edge> Edges = new();
        
        /// <summary>
        /// Embedding dim should be the same, so we can use index to slice it.
        /// </summary>
        public int Dim = 512;
        
        [NativeDisableContainerSafetyRestriction]
        public NativeList<float> Embeddings;
        
        public ChatGraph(int dim = 512)
        {
            Dim = dim;
            Embeddings = new NativeList<float>(dim, Allocator.Persistent);
        }
        
        public ChatGraph(string filePath)
        {
            Load(filePath);
        }
        
        public void Save(string filePath)
        {
            using var bw = new BinaryWriter(new FileStream(filePath, FileMode.Create));
            Save(bw);
        }
        
        public void Save(BinaryWriter bw)
        {
            bw.Write(Dim);
            bw.Write(Edges.Count);
            for (int i = 0; i < Embeddings.Length; ++i)
            {
                bw.Write(Embeddings[i]);
            }
            foreach (var edge in Edges)
            {
                edge.Save(bw);
            }
        }
        
        public void Load(string filePath)
        {
            using var br = new BinaryReader(new FileStream(filePath, FileMode.Open));
            Load(br);
        }
        
        public void Load(BinaryReader br)
        {
            Edges.Clear();
            Dim = br.ReadInt32();
            int edgeL = br.ReadInt32();
            int embeddingL = edgeL * Dim * 2;
            Embeddings.DisposeSafe();
            Embeddings = new NativeList<float>(embeddingL, Allocator.Persistent);
            var temp = new NativeArray<float>(embeddingL, Allocator.Temp);
            for (int i = 0; i < embeddingL; ++i)
            {
                temp[i] = br.ReadSingle();
            }
            Embeddings.AddRange(temp);
            temp.Dispose();
            for (int i = 0; i < edgeL; ++i)
            {
                var edge = new Edge();
                edge.Load(br);
                Edges.Add(edge);
            }
        }

        public void Dispose()
        {
            Embeddings.DisposeSafe();
        }
        
        [Serializable]
        public struct Edge
        {
            public Port input;
            
            public Port output;
            
            public readonly void Save(BinaryWriter bw)
            {
                input.Save(bw);
                output.Save(bw);
            }
            
            public void Load(BinaryReader br)
            {
                input.Load(br);
                output.Load(br);
            }
        }
        
        [Serializable]
        public struct Port
        {
            public uint uniqueId;
            
            public Port(uint uniqueId)
            {
                this.uniqueId = uniqueId;
            }
            
            public readonly void Save(BinaryWriter bw)
            {
                bw.Write(uniqueId);
            }
            
            public void Load(BinaryReader br)
            {
                uniqueId = br.ReadUInt32();
            }
        }
    }
}