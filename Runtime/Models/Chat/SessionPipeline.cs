using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Kurisu.UniChat.Memory;
using Unity.Sentis;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using Debug = UnityEngine.Debug;
namespace Kurisu.UniChat
{
    /// <summary>
    /// Pipeline to write embedding from <see cref="ChatSession"/>
    /// </summary>
    public class SessionPipeline : IDisposable
    {
        private readonly SemaphoreSlim semaphore = new(1, 1);
        private IEncoder encoder;
        private IEmbeddingTable sourceTable;
        private ChatDataBase dataBase;
        private IPersistEmbeddingValue<string> stringPersister;
        private ISplitter splitter;
        private ChatMemory memory;
        private static readonly int[] reduceAxis = new int[1] { 1 };
        private readonly ITensorAllocator allocator = new TensorCachingAllocator();
        private Ops ops;
        #region  Build Methods
        public SessionPipeline SetBackend(BackendType backendType)
        {
            ops = WorkerFactory.CreateOps(backendType, allocator);
            return this;
        }
        public SessionPipeline SetSource(IEmbeddingTable sourceTable)
        {
            this.sourceTable = sourceTable;
            return this;
        }
        public SessionPipeline SetEmbedding(ChatDataBase embeddingDB)
        {
            dataBase = embeddingDB;
            return this;
        }
        public SessionPipeline SetEncoder(IEncoder encoder)
        {
            this.encoder = encoder;
            return this;
        }
        public SessionPipeline SetPersister(IPersistEmbeddingValue<string> stringPersister)
        {
            this.stringPersister = stringPersister;
            return this;
        }
        public SessionPipeline SetSplitter(ISplitter splitter)
        {
            this.splitter = splitter;
            return this;
        }
        public SessionPipeline SetMemory(ChatMemory memory)
        {
            this.memory = memory;
            return this;
        }
        #endregion
        private void AssertPipeline()
        {
            Assert.IsNotNull(ops);
            Assert.IsNotNull(encoder);
            Assert.IsNotNull(sourceTable);
            Assert.IsNotNull(dataBase);
            Assert.IsNotNull(stringPersister);
            Assert.IsNotNull(splitter);
            Assert.IsNotNull(memory);
        }
        /// <summary>
        /// Run chat session embedding pipeline
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async UniTask Run(ChatSession session)
        {
            await semaphore.WaitAsync();
            Debug.Log("Session pipeline start...");
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                AssertPipeline();
                //Preprocessing
                memory.ChatHistory.ClearHistory();
                var pairs = (from x in session.history.contents where !string.IsNullOrEmpty(x[1]) select x).ToArray();
                var pool = ListPool<string>.Get();
                var inputTextEmbeddings = new TensorFloat[pairs.Length];
                for (int i = 0; i < pairs.Length; ++i)
                {
                    memory.ChatHistory.AppendUserMessage(pairs[i][0]);
                    pool.Clear();
                    splitter.Split(memory.GetMemoryContext(), pool);
                    var contextTensor = encoder.Encode(ops, pool);
                    TensorFloat contextTensorExpanded = contextTensor.ShallowReshape(contextTensor.shape.Unsqueeze(0)) as TensorFloat;
                    inputTextEmbeddings[i] = ops.ReduceMean(contextTensorExpanded, new(reduceAxis), false);
                    memory.ChatHistory.AppendBotMessage(pairs[i][1]);
                }
                ListPool<string>.Release(pool);
                var outputs = (from x in pairs select x[1]).ToArray();
                var outputTextEmbeddings = encoder.Encode(ops, outputs);
                //Make Readable
                await UniTask.WhenAll(inputTextEmbeddings.MakeReadableAsync(), outputTextEmbeddings.MakeReadableAsync().AsUniTask());
                //Writing
                for (int i = 0; i < pairs.Length; ++i)
                {
                    uint inputHash = XXHash.CalculateHash(inputTextEmbeddings[i]);
                    uint outputHash = XXHash.CalculateHash(outputs[i]);
                    var inputEmb = new Embedding()
                    {
                        values = inputTextEmbeddings[i].ToArray(i)
                    };
                    var outputEmb = new Embedding()
                    {
                        values = outputTextEmbeddings.ToArray(i)
                    };
                    if (stringPersister.Persist(outputHash, outputs[i], outputEmb, out var entry))
                    {
                        //Add to source table
                        if (sourceTable.AddEntry(entry))
                            dataBase.AddEdge(inputHash, inputEmb, outputHash, outputEmb);
                    }
                }
            }
            finally
            {
                stopWatch.Stop();
                Debug.Log($"Session pipeline end, time used: {stopWatch.ElapsedMilliseconds}.");
                semaphore.Release();
            }
        }
        public void Dispose()
        {
            ops.Dispose();
            allocator.Dispose();
        }
    }
}