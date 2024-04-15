using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Kurisu.UniChat.NLP;
using Unity.Sentis;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;
namespace Kurisu.UniChat
{
    /// <summary>
    /// Pipeline to write embedding from <see cref="ChatSession"/>
    /// </summary>
    public class SessionPipeline : IDisposable
    {
        private readonly SemaphoreSlim semaphore = new(1, 1);
        #region Properties
        protected IEncoder Encoder { get; set; }
        protected IClassifier Classifier { get; set; }
        protected IEmbeddingTable SourceTable { get; set; }
        protected ChatDataBase DataBase { get; set; }
        protected IPersistEmbeddingValue<string> StringPersister { get; set; }
        #endregion
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
            SourceTable = sourceTable;
            return this;
        }
        public SessionPipeline SetEmbedding(ChatDataBase embeddingDB)
        {
            DataBase = embeddingDB;
            return this;
        }
        public SessionPipeline SetEncoder(IEncoder encoder)
        {
            Encoder = encoder;
            return this;
        }
        public SessionPipeline SetClassifier(IClassifier classifier)
        {
            Classifier = classifier;
            return this;
        }
        public SessionPipeline SetPersister(IPersistEmbeddingValue<string> stringPersister)
        {
            StringPersister = stringPersister;
            return this;
        }
        #endregion
        private void AssertPipeline()
        {
            Assert.IsNotNull(ops);
            Assert.IsNotNull(Encoder);
            Assert.IsNotNull(Classifier);
            Assert.IsNotNull(SourceTable);
            Assert.IsNotNull(DataBase);
        }
        /// <summary>
        /// Run chat session embedding pipeline
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async UniTask Run(ChatSession session)
        {
            await semaphore.WaitAsync();
            Debug.Log("Pipeline start...");
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                AssertPipeline();
                //Preprocessing
                var pairs = (from x in session.history.contents where !string.IsNullOrEmpty(x[1]) select x).ToArray();
                var inputs = (from x in pairs select x[0]).ToArray();
                var outputs = (from x in pairs select x[1]).ToArray();
                var inputTextEmbeddings = Encoder.Encode(ops, inputs);
                (var inputSentimentEmbeddings, var inputSentimentLabelIds) = Classifier.Classify(ops, inputs);
                var outputTextEmbeddings = Encoder.Encode(ops, outputs);
                (var outputSentimentEmbeddings, var outputSentimentLabelIds) = Classifier.Classify(ops, outputs);
                //Mark
                inputTextEmbeddings.MakeReadable();
                inputSentimentEmbeddings.MakeReadable();
                inputSentimentLabelIds.MakeReadable();
                outputTextEmbeddings.MakeReadable();
                outputSentimentEmbeddings.MakeReadable();
                outputSentimentLabelIds.MakeReadable();
                //Writing
                for (int i = 0; i < pairs.Length; ++i)
                {
                    uint inputHash = XXHash.CalculateHash(inputTextEmbeddings, i);
                    uint outputHash = XXHash.CalculateHash(outputTextEmbeddings, i);
                    var inputEmb = new Embedding()
                    {
                        values = inputTextEmbeddings.ToArray(i)
                    };
                    var outputEmb = new Embedding()
                    {
                        values = outputTextEmbeddings.ToArray(i)
                    };
                    if (StringPersister.Persist(outputHash, outputs[i], outputEmb, out var entry))
                    {
                        //Add to source table
                        if (SourceTable.AddEntry(entry))
                            DataBase.AddEdge(inputHash, inputEmb, outputHash, outputEmb);
                    }
                }
            }
            finally
            {
                stopWatch.Stop();
                Debug.Log($"Pipeline end, time used: {stopWatch.ElapsedMilliseconds}.");
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