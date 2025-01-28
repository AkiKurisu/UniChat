using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniChat.Memory;
using Unity.Sentis;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using Debug = UnityEngine.Debug;
namespace UniChat
{
    /// <summary>
    /// Pipeline to write embedding from <see cref="ChatSession"/>
    /// </summary>
    public class SessionPipeline : IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        
        private IEncoder _encoder;
        
        private IEmbeddingTable _sourceTable;
        
        private ChatDataBase _dataBase;
        
        private IPersistEmbeddingValue<string> _stringPersist;
        
        private ISplitter _splitter;
        
        private ChatMemory _memory;
        
        private static readonly int[] ReduceAxis = { 1 };
        
        private readonly ITensorAllocator _allocator = new TensorCachingAllocator();
        
        private Ops _ops;
        
        #region  Build Methods
        public SessionPipeline SetBackend(BackendType backendType)
        {
            _ops = WorkerFactory.CreateOps(backendType, _allocator);
            return this;
        }
        
        public SessionPipeline SetSource(IEmbeddingTable sourceTable)
        {
            _sourceTable = sourceTable;
            return this;
        }
        
        public SessionPipeline SetEmbedding(ChatDataBase embeddingDB)
        {
            _dataBase = embeddingDB;
            return this;
        }
        
        public SessionPipeline SetEncoder(IEncoder encoder)
        {
            _encoder = encoder;
            return this;
        }
        
        public SessionPipeline SetPersister(IPersistEmbeddingValue<string> stringPersister)
        {
            _stringPersist = stringPersister;
            return this;
        }
        
        public SessionPipeline SetSplitter(ISplitter splitter)
        {
            _splitter = splitter;
            return this;
        }
        
        public SessionPipeline SetMemory(ChatMemory memory)
        {
            _memory = memory;
            return this;
        }
        #endregion
        
        private void AssertPipeline()
        {
            Assert.IsNotNull(_ops);
            Assert.IsNotNull(_encoder);
            Assert.IsNotNull(_sourceTable);
            Assert.IsNotNull(_dataBase);
            Assert.IsNotNull(_stringPersist);
            Assert.IsNotNull(_splitter);
            Assert.IsNotNull(_memory);
        }

        /// <summary>
        /// Run chat session embedding pipeline
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public async UniTask Run(ChatSession session)
        { 
            _ops ??= WorkerFactory.CreateOps(BackendType.CPU, _allocator);
            await _semaphore.WaitAsync();
            Debug.Log("Session pipeline start...");
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                AssertPipeline();
                //Preprocessing
                _memory.ChatHistory.ClearHistory();
                var pairs = (from x in session.history.contents where !string.IsNullOrEmpty(x[1]) select x).ToArray();
                var pool = ListPool<string>.Get();
                var inputTextEmbeddings = new TensorFloat[pairs.Length];
                for (int i = 0; i < pairs.Length; ++i)
                {
                    _memory.ChatHistory.AppendUserMessage(pairs[i][0]);
                    pool.Clear();
                    _splitter.Split(_memory.GetMemoryContext(), pool);
                    var contextTensor = _encoder.Encode(_ops, pool);
                    TensorFloat contextTensorExpanded = contextTensor.ShallowReshape(contextTensor.shape.Unsqueeze(0)) as TensorFloat;
                    inputTextEmbeddings[i] = _ops.ReduceMean(contextTensorExpanded, new(ReduceAxis), false);
                    _memory.ChatHistory.AppendBotMessage(pairs[i][1]);
                }
                ListPool<string>.Release(pool);
                var outputs = (from x in pairs select x[1]).ToArray();
                var outputTextEmbeddings = _encoder.Encode(_ops, outputs);
                //Make Readable
                await UniTask.WhenAll(inputTextEmbeddings.MakeReadableAsync(), outputTextEmbeddings.MakeReadableAsync().AsUniTask());
                //Writing
                for (int i = 0; i < pairs.Length; ++i)
                {
                    uint inputHash = XXHash.CalculateHash(inputTextEmbeddings[i]);
                    uint outputHash = XXHash.CalculateHash(outputs[i]);
                    var inputEmb = new Embedding
                    {
                        values = inputTextEmbeddings[i].ToArray(0)
                    };
                    var outputEmb = new Embedding
                    {
                        values = outputTextEmbeddings.ToArray(i)
                    };
                    if (_stringPersist.Persist(outputHash, outputs[i], outputEmb, out var entry))
                    {
                        //Add to source table
                        if (_sourceTable.AddEntry(entry))
                            _dataBase.AddEdge(inputHash, inputEmb, outputHash, outputEmb);
                    }
                }
            }
            finally
            {
                stopWatch.Stop();
                Debug.Log($"Session pipeline end, time used: {stopWatch.ElapsedMilliseconds}.");
                _semaphore.Release();
                _allocator.Reset(false);
            }
        }
        public void Dispose()
        {
            _ops.Dispose();
            _allocator.Dispose();
        }
    }
}