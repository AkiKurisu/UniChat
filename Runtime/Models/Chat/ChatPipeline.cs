using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using Kurisu.NGDS.NLP;
using Unity.Collections;
using Unity.Sentis;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;
namespace Kurisu.UniChat
{
    public class ChatPipeline : IDisposable
    {
        public class ContextConverter : ITensorConverter
        {
            private readonly IEncoder encoder;
            private static readonly int[] reduceAxis = new int[1] { 0 };
            private readonly TensorFloat[] inputTensors;
            private readonly List<string> inputs = new();
            public ContextConverter(IEncoder encoder)
            {
                this.encoder = encoder;
                inputTensors = new TensorFloat[2];
            }
            public TensorFloat[] Convert(Ops ops, IReadOnlyList<string> inputs)
            {
                inputTensors[1] = encoder.Encode(ops, inputs[^1]);
                var contextTensor = encoder.Encode(ops, inputs);
                TensorFloat contextTensorExpanded = contextTensor.ShallowReshape(inputTensors[1].shape.Unsqueeze(0)) as TensorFloat;
                inputTensors[0] = ops.ReduceMean(contextTensorExpanded, new ReadOnlySpan<int>(reduceAxis), false);
                return inputTensors;
            }

            public TensorFloat[] Convert(Ops ops, string input)
            {
                inputs.Clear();
                inputs.Add(input);
                return Convert(ops, inputs);
            }
        }
        private readonly SemaphoreSlim semaphore = new(1, 1);
        #region  Properties
        protected ITensorConverter InputConverter { get; set; }
        protected ITensorConverter OutputConverter { get; set; }
        protected IGenerator Generator { get; set; }
        protected IFilter Filter { get; set; }
        protected IEmbeddingTable SourceTable { get; set; }
        protected IPersistEmbeddingValue<string> StringPersister { get; set; }
        protected ChatDataBase DataBase { get; set; }
        #endregion
        private CancellationTokenSource ct;
        private readonly ITensorAllocator allocator = new TensorCachingAllocator();
        protected Ops ops;
        private void AssertPipeline()
        {
            Assert.IsNotNull(ops);
            Assert.IsNotNull(InputConverter);
            Assert.IsNotNull(OutputConverter);
            Assert.IsNotNull(Filter);
            Assert.IsNotNull(SourceTable);
            Assert.IsNotNull(DataBase);
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
                if (Filter.Filter(ops, inputTensors, DataBase, ref ids, ref scores))
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
            var outputTensors = OutputConverter.Convert(ops, persistStringValue);
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
            //Dispose
            inputTensors.Dispose();
            outputTensors.Dispose();
            return true;
        }
    }
}