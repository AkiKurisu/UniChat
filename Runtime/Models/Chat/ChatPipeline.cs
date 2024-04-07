using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Kurisu.NGDS.NLP;
using Unity.Collections;
using Unity.Sentis;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using Debug = UnityEngine.Debug;
namespace Kurisu.UniChat
{
    public class ChatPipeline : IDisposable
    {
        public class ContextConverter : ITensorConverter
        {
            private readonly IEncoder encoder;
            private static readonly int[] reduceAxis = new int[1] { 1 };
            private readonly TensorFloat[] inputTensors;
            private readonly IChatHistoryQuery historyQuery;
            public ContextConverter(IEncoder encoder, IChatHistoryQuery historyQuery)
            {
                this.encoder = encoder;
                this.historyQuery = historyQuery;
                inputTensors = new TensorFloat[2];
            }
            public TensorFloat[] Convert(Ops ops, IReadOnlyList<string> inputs)
            {
                //Exclude last bot response 
                var lastResponse = historyQuery.GetBotMessages().LastOrDefault()?.Content;
                if (lastResponse != null)
                    inputTensors[1] = encoder.Encode(ops, lastResponse);
                else
                    inputTensors[1] = encoder.Encode(ops, inputs[^1]);
                var contextTensor = encoder.Encode(ops, inputs);
                TensorFloat contextTensorExpanded = contextTensor.ShallowReshape(contextTensor.shape.Unsqueeze(0)) as TensorFloat;
                inputTensors[0] = ops.ReduceMean(contextTensorExpanded, new(reduceAxis), false);
                return inputTensors;
            }
        }
        private readonly SemaphoreSlim semaphore = new(1, 1);
        #region  Properties
        protected ITensorConverter InputConverter { get; set; }
        protected ITensorConverter OutputConverter { get; set; }
        protected IGenerator Generator { get; set; }
        protected IFilter Filter { get; set; }
        protected IEmbeddingTable SourceTable { get; set; }
        protected IChatHistoryQuery HistoryQuery { get; set; }
        protected IPersistEmbeddingValue<string> StringPersister { get; set; }
        protected ChatDataBase DataBase { get; set; }
        #endregion
        private CancellationTokenSource ct;
        private readonly ITensorAllocator allocator = new TensorCachingAllocator();
        protected Ops ops;
        /// <summary>
        /// Output threshold to clip answers above this value, useful for multi-turn chat
        /// </summary>
        public float Temperature { get; set; } = 0.85f;
        private void AssertPipeline()
        {
            Assert.IsNotNull(ops);
            Assert.IsNotNull(HistoryQuery);
            Assert.IsNotNull(InputConverter);
            Assert.IsNotNull(OutputConverter);
            Assert.IsNotNull(Filter);
            Assert.IsNotNull(SourceTable);
            Assert.IsNotNull(DataBase);
        }
        public TensorFloat ThresholdClipping(TensorFloat input, TensorFloat clip, float threshold)
        {
            var thresholdTensor = TensorFloat.Zeros(clip.shape);
            for (int i = 0; i < clip.shape[0]; ++i)
            {
                for (int j = 0; j < clip.shape[1]; ++j)
                {
                    thresholdTensor[i, j] = threshold;
                }
            }
            var maskTensor = TensorFloat.Zeros(clip.shape);
            var outputs = ops.Where(ops.GreaterOrEqual(clip, thresholdTensor), maskTensor, input);
            thresholdTensor.Dispose();
            maskTensor.Dispose();
            return outputs;
        }
        protected virtual TensorFloat ScoreDataBase(TensorFloat[] inputTensors)
        {
            TensorFloat[] comparedTensors = DataBase.AllocateTensors();
            //Input similarity
            TensorFloat inputScores = ops.CosineSimilarity(inputTensors[0], comparedTensors[0]);
            //Output similarity
            TensorFloat outputScores = ops.CosineSimilarity(inputTensors[1], comparedTensors[1]);
            //Clipping
            TensorFloat clippingScores = ThresholdClipping(inputScores, outputScores, Temperature);
            //Mask
            TensorFloat mask = TensorFloat.Zeros(new TensorShape(1, DataBase.Count));//transpose
            for (int i = 0; i < DataBase.Count; ++i)
            {
                mask[0, i] = HistoryQuery.TryGetBotMessage(DataBase.GetOutput(i), out _) ? 0 : 1;
            }
            return ops.Mul(clippingScores, mask);
        }
        /// <summary>
        /// Run chat pipeline
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async UniTask Run(GenerateContext context)
        {
            await semaphore.WaitAsync();
            ct?.Dispose();
            ct = new();
            Debug.Log("Pipeline start...");
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            NativeArray<int> ids = default;
            NativeArray<float> scores = default;
            try
            {
                AssertPipeline();
                TensorFloat[] inputTensors = InputConverter.Convert(ops, context.input);
                if (DataBase.Count > 0 && Filter.Filter(ops, ScoreDataBase(inputTensors), ref ids, ref scores))
                {
                    context.flag |= 1 << 0;
                    context.flag |= 1 << 1;
                    SelectorPostProcessing(inputTensors, ref ids, ref scores, context);
                }
                else
                {
                    context.flag |= 0 << 0;
                    if (Generator != null)
                    {
                        if (await Generator.Generate(context, ct.Token))
                        {
                            context.flag |= 1 << 1;
                            GeneratorPostProcessing(inputTensors, context);
                        }
                        else
                        {
                            context.flag |= 0 << 1;
                            Debug.LogError("Generation failed!");
                        }
                    }
                    else
                    {
                        context.flag |= 0 << 1;
                        Debug.LogWarning("Generator is null!");
                    }
                }
            }
            finally
            {
                stopWatch.Stop();
                Debug.Log($"Pipeline end, time used: {stopWatch.ElapsedMilliseconds}.");
                semaphore.Release();
                ids.DisposeSafe();
                scores.DisposeSafe();
            }
        }
        public void Dispose()
        {
            ct?.Dispose();
            ops.Dispose();
            allocator.Dispose();
        }
        #region  Build Methods
        public ChatPipeline SetBackend(BackendType backendType)
        {
            ops = WorkerFactory.CreateOps(backendType, allocator);
            return this;
        }
        public ChatPipeline SetFilter(IFilter filter)
        {
            Filter = filter;
            return this;
        }
        public ChatPipeline SetSource(IEmbeddingTable sourceTable)
        {
            SourceTable = sourceTable;
            return this;
        }
        public ChatPipeline SetEmbedding(ChatDataBase embeddingDB)
        {
            DataBase = embeddingDB;
            return this;
        }
        public ChatPipeline SetInputConvertor(ITensorConverter converter)
        {
            InputConverter = converter;
            return this;
        }
        public ChatPipeline SetOutputConvertor(ITensorConverter converter)
        {
            OutputConverter = converter;
            return this;
        }
        public ChatPipeline SetGenerator(IGenerator generator)
        {
            Generator = generator;
            return this;
        }
        public ChatPipeline SetPersister(IPersistEmbeddingValue<string> stringPersister)
        {
            StringPersister = stringPersister;
            return this;
        }
        public ChatPipeline SetTemperature(float temperature)
        {
            Temperature = temperature;
            return this;
        }
        public ChatPipeline SetHistoryQuery(IChatHistoryQuery historyQuery)
        {
            HistoryQuery = historyQuery;
            return this;
        }
        #endregion
        protected virtual void SelectorPostProcessing(TensorFloat[] inputTensors, ref NativeArray<int> ids, ref NativeArray<float> scores, GenerateContext context)
        {
            //You selector implementation, in this case select first one
            if (SourceTable.TryGetEntry(DataBase.GetOutput(ids[0]), out var entry))
            {
                context.outputEntry = entry;
            }
        }
        protected virtual void GeneratorPostProcessing(TensorFloat[] inputTensors, GenerateContext context)
        {
            if (StringPersister != null)
            {
                if (Persist(inputTensors, context.generatedContent, out var entry))
                    context.outputEntry = entry;
            }
        }
        private bool Persist(TensorFloat[] inputTensors, string persistStringValue, out IEmbeddingEntry entry)
        {
            var pool = ListPool<string>.Get();
            pool.Add(persistStringValue);
            var outputTensors = OutputConverter.Convert(ops, pool);
            ListPool<string>.Release(pool);

            inputTensors.MakeReadable();
            outputTensors.MakeReadable();

            //Calculate hash
            uint inputHash = XXHash.CalculateHash(inputTensors[0]);
            uint outputHash = XXHash.CalculateHash(outputTensors[0]);
            var inputEmb = new Embedding()
            {
                values = inputTensors[0].ToReadOnlyArray(),
            };
            var outputEmb = new Embedding()
            {
                values = outputTensors[0].ToReadOnlyArray(),
            };

            //Persist value
            if (!StringPersister.Persist(outputHash, persistStringValue, outputEmb, out entry)) return false;
            //Update embedding table
            if (!SourceTable.AddEntry(entry)) return false;
            //Update embedding db
            DataBase.AddEmbedding(inputHash, inputEmb, outputHash, outputEmb);
            return true;
        }
    }
}