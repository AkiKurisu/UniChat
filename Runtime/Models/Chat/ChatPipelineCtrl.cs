using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Kurisu.NGDS.NLP;
using Newtonsoft.Json;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
namespace Kurisu.UniChat
{
    public class ChatModelFile
    {
        public string fileName = "ChatModel";
        /// <summary>
        /// Dim according to your embedding model
        /// </summary>
        public int embeddingDim = 512;
        /// <summary>
        /// Embedding model to use, should exist in models folder
        /// </summary>
        public string embeddingModelName = "bge-small-zh-v1.5";
        public string characterCardPath = "";
        public string tableFileName = "table.bin";
        public string graphFileName = "graph.bin";
        [JsonIgnore]
        public string FileFolder => Path.Combine(PathUtil.UserDataPath, fileName);
    }
    public class PipelineConfig
    {
        public bool canWrite;
        public float inputThreshold;
        public float outputThreshold;
    }
    public class ChatPipelineCtrl<TPipeline, KTable> : IDisposable
    where TPipeline : ChatPipeline, new()
    where KTable : ISerializable, IEmbeddingTable, new()
    {
        public ChaCardFile ChaFile { get; }
        public TextEncoder Encoder { get; protected set; }
        public ChatDataBase DataBase { get; protected set; }
        public KTable Table { get; protected set; }
        public ChatModelFile ChatFile { get; protected set; }
        public ISplitter Splitter { get; protected set; }
        public ChatGeneratorBase Generator { get; protected set; }
        protected TPipeline pipeline;
        public ChatPipelineCtrl(ChatModelFile chatFile)
        {
            ChatFile = chatFile;
            ChaFile = new();
            if (!string.IsNullOrEmpty(ChatFile.characterCardPath))
            {
                ChaFile.LoadCard(ChatFile.characterCardPath);
            }
            var networkPath = Path.Combine(ChatFile.FileFolder, ChatFile.graphFileName);
            if (File.Exists(networkPath))
            {
                DataBase = new(networkPath);
            }
            else
            {
                DataBase = new(ChatFile.embeddingDim);
            }
            var tablePath = Path.Combine(ChatFile.FileFolder, ChatFile.tableFileName);
            Table = new();
            if (File.Exists(tablePath))
            {
                Table.Load(tablePath);
            }
            var embeddingModelFolder = Path.Combine(PathUtil.ModelPath, ChatFile.embeddingModelName);
            Assert.IsTrue(Directory.Exists(embeddingModelFolder));
            Encoder = new TextEncoder(
                    ModelLoader.Load(Path.Combine(embeddingModelFolder, "model.sentis")),
                    new BertTokenizer(File.ReadAllText(Path.Combine(embeddingModelFolder, "tokenizer.json"))),
                    BackendType.GPUCompute
                );
            Splitter = new SlidingWindowSplitter(256);
        }
        public virtual void InitializePipeline(ChatGeneratorBase generator, PipelineConfig config)
        {
            Generator = generator;
            Debug.Log($"Initialize pipeline, use generator: {generator.GetType().Name}");
            pipeline?.Dispose();
            pipeline = new TPipeline()
                            .SetBackend(BackendType.GPUCompute)
                            .SetInputConvertor(new ChatPipeline.ContextConverter(Encoder, generator))
                            .SetOutputConvertor(new MultiEncoderConverter(Encoder))
                            .SetGenerator(generator)
                            .SetHistoryQuery(generator)
                            .SetSource(Table)
                            .SetEmbedding(DataBase)
                            .SetPersister(config.canWrite ? new TextEmbeddingTable.PersistHandler() : null)
                            .SetTemperature(config.outputThreshold)
                            .SetFilter(new TopSimilarityFilter(config.inputThreshold)) as TPipeline;
        }
        public void ReleasePipeline()
        {
            pipeline?.Dispose();
            pipeline = null;
        }
        public void Dispose()
        {
            ReleasePipeline();
            Encoder.Dispose();
            DataBase.Dispose();
        }
        public async UniTask<GenerateContext> RunPipeline()
        {
            var pool = ListPool<string>.Get();
            Splitter.Split(Generator.GetHistoryContext(), pool);
            var context = new GenerateContext(pool);
            try
            {
                await pipeline.Run(context);
                return context;
            }
            finally
            {
                ListPool<string>.Release(pool);
            }
        }
        public void SaveModel()
        {
            if (!Directory.Exists(ChatFile.FileFolder))
            {
                Directory.CreateDirectory(ChatFile.FileFolder);
            }
            File.WriteAllText(Path.Combine(ChatFile.FileFolder, "model.cfg"), JsonConvert.SerializeObject(ChatFile, Formatting.Indented));
            Table.Save(Path.Combine(ChatFile.FileFolder, ChatFile.tableFileName));
            DataBase.Save(Path.Combine(ChatFile.FileFolder, ChatFile.graphFileName));
        }

        public bool SaveCard(string path, bool savePng)
        {
            if (ChaFile.SaveCard(path, savePng))
            {
                ChatFile.characterCardPath = path;
                return true;
            }
            return false;
        }
    }
}