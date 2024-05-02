using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Sentis;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using Debug = UnityEngine.Debug;
namespace Kurisu.UniChat
{
    public class ChatPipeline : IDisposable
    {
        private readonly SemaphoreSlim semaphore = new(1, 1);
        #region Public Properties
        public bool Verbose { get; set; }
        public IEncoder Encoder { get; set; }
        public IGenerator Generator { get; set; }
        public IFilter Filter { get; set; }
        public IEmbeddingTable SourceTable { get; set; }
        public IChatMemory Memory { get; set; }
        public IPersistEmbeddingValue<string> StringPersister { get; set; }
        public ChatDataBase DataBase { get; set; }
        /// <summary>
        /// Output threshold to clip answers above this value, useful for multi-turn chat
        /// </summary>
        public float Temperature { get; set; } = 0.85f;
        #endregion
        private CancellationTokenSource ct;
        private BackendType backendType;
        private ITensorAllocator allocator = new TensorCachingAllocator();
        protected Ops ops;
        private static readonly int[] reduceAxis = new int[1] { 1 };
        private readonly TensorFloat[] inputTensors = new TensorFloat[2];
        private void AssertPipeline()
        {
            Assert.IsNotNull(ops);
            Assert.IsNotNull(Memory);
            Assert.IsNotNull(Encoder);
            Assert.IsNotNull(Filter);
            Assert.IsNotNull(SourceTable);
            Assert.IsNotNull(DataBase);
        }
        public TensorFloat[] Input2Tensor(Ops ops, IReadOnlyList<string> inputs)
        {
            var lastResponse = Memory.GetMessages(MessageRole.Bot).LastOrDefault()?.Content;
            if (lastResponse != null)
                inputTensors[1] = Encoder.Encode(ops, lastResponse);
            else
                inputTensors[1] = Encoder.Encode(ops, inputs[^1]);
            var contextTensor = Encoder.Encode(ops, inputs);
            TensorFloat contextTensorExpanded = contextTensor.ShallowReshape(contextTensor.shape.Unsqueeze(0)) as TensorFloat;
            inputTensors[0] = ops.ReduceMean(contextTensorExpanded, new(reduceAxis), false);
            return inputTensors;
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
        /// <summary>
        /// Score by semantic similarity
        /// </summary>
        /// <param name="inputTensors"></param>
        /// <returns></returns>
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
                mask[0, i] = Memory.TryGetMessage(MessageRole.Bot, DataBase.GetOutput(i), out _) ? 0 : 1;
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
            if (Verbose) Debug.Log("Pipeline start...");
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            NativeArray<int> ids = default;
            NativeArray<float> scores = default;
            try
            {
                AssertPipeline();
                if (Verbose) Debug.Log($"Pipeline convert inputs, batch size {context.input.Count}, inputs content: {string.Join('\n', context.input)}");
                TensorFloat[] inputTensors = Input2Tensor(ops, context.input);
                if (DataBase.Count > 0 && Filter.Filter(ops, ScoreDataBase(inputTensors), ref ids, ref scores))
                {
                    if (Verbose) Debug.Log($"Pipeline call selector");
                    context.flag |= 1 << 0;
                    context.flag |= 1 << 1;
                    await SelectorPostProcessing(inputTensors, ref ids, ref scores, context);
                }
                else
                {
                    context.flag |= 0 << 0;
                    if (Generator != null)
                    {
                        if (Verbose) Debug.Log($"Pipeline call generator");
                        if (await Generator.Generate(context, ct.Token))
                        {
                            if (Verbose) Debug.Log($"Generate content: {context.generatedContent}");
                            context.flag |= 1 << 1;
                            await GeneratorPostProcessing(inputTensors, context);
                        }
                        else
                        {
                            context.flag |= 0 << 1;
                            if (Verbose) Debug.LogError("Generation failed!");
                        }
                    }
                    else
                    {
                        context.flag |= 0 << 1;
                        if (Verbose) Debug.LogWarning("Generator is null!");
                    }
                }
            }
            finally
            {
                stopWatch.Stop();
                if (Verbose) Debug.Log($"Pipeline end, time used: {stopWatch.ElapsedMilliseconds}.");
                semaphore.Release();
                ids.DisposeSafe();
                scores.DisposeSafe();
                FreeMemoryAndReAllocate();
            }
        }
        public void Dispose()
        {
            ct?.Dispose();
            ops?.Dispose();
            allocator.Dispose();
        }
        private void FreeMemoryAndReAllocate()
        {
            ops?.Dispose();
            allocator.Dispose();
            allocator = new TensorCachingAllocator();
            ops = WorkerFactory.CreateOps(backendType, allocator);
        }
        #region  Build Methods
        public ChatPipeline SetBackend(BackendType backendType)
        {
            this.backendType = backendType;
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
        public ChatPipeline SetEncoder(IEncoder encoder)
        {
            Encoder = encoder;
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
        public ChatPipeline SetVerbose(bool verbose)
        {
            Verbose = verbose;
            return this;
        }
        public ChatPipeline SetMemory(IChatMemory memory)
        {
            Memory = memory;
            return this;
        }
        #endregion
        protected virtual UniTask SelectorPostProcessing(TensorFloat[] inputTensors, ref NativeArray<int> ids, ref NativeArray<float> scores, GenerateContext context)
        {
            //You selector implementation, in this case select first one
            if (SourceTable.TryGetEntry(DataBase.GetOutput(ids[0]), out var entry))
            {
                context.outputEntry = entry;
            }
            return UniTask.CompletedTask;
        }
        protected virtual async UniTask GeneratorPostProcessing(TensorFloat[] inputTensors, GenerateContext context)
        {
            if (StringPersister != null)
            {
                await Persist(inputTensors, context);
            }
        }
        private async UniTask<bool> Persist(TensorFloat[] inputTensors, GenerateContext context)
        {
            var pool = ListPool<string>.Get();
            pool.Add(context.generatedContent);
            var outputTensor = Encoder.Encode(ops, pool);
            ListPool<string>.Release(pool);

            await inputTensors.MakeReadableAsync();
            await outputTensor.MakeReadableAsync();

            //Calculate input hash by tensor
            uint inputHash = XXHash.CalculateHash(inputTensors[0]);
            //Calculate output hash by string
            uint outputHash = XXHash.CalculateHash(context.generatedContent);
            var inputEmb = new Embedding()
            {
                values = inputTensors[0].ToReadOnlyArray(),
            };
            var outputEmb = new Embedding()
            {
                values = outputTensor.ToReadOnlyArray(),
            };
            //Persist value
            if (!StringPersister.Persist(outputHash, context.generatedContent, outputEmb, out IEmbeddingEntry entry)) return false;
            //Update embedding table
            if (!SourceTable.AddEntry(entry)) return false;
            //Update embedding db
            DataBase.AddEdge(inputHash, inputEmb, outputHash, outputEmb);
            context.outputEntry = entry;
            return true;
        }
    }
}