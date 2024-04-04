using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Kurisu.NGDS.NLP;
using Newtonsoft.Json;
using Unity.Sentis;
using UnityEngine;
namespace Kurisu.UniChat
{
    public class ChatModelFile
    {
        public string fileName = "ChatModel";
        public int embeddingDim = 512;
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
        protected TPipeline pipeline;
        protected IGenerator generator;
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
            if (!string.IsNullOrEmpty(ChatFile.embeddingModelName))
            {
                var embeddingModelFolder = Path.Combine(PathUtil.ModelPath, ChatFile.embeddingModelName);
                Encoder = new TextEncoder(
                        ModelLoader.Load(Path.Combine(embeddingModelFolder, "model.sentis")),
                        new BertTokenizer(File.ReadAllText(Path.Combine(embeddingModelFolder, "tokenizer.json"))),
                        BackendType.GPUCompute
                    );
            }
            Splitter = new SlidingWindowSplitter(256);
        }
        public virtual void InitializePipeline(IGenerator generator, PipelineConfig config)
        {
            this.generator = generator;
            Debug.Log($"Initialize pipeline, use generator: {generator.GetType().Name}");
            pipeline?.Dispose();
            pipeline = new TPipeline()
                            .SetBackend(BackendType.GPUCompute)
                            .SetInputConvertor(new ChatPipeline.ContextConverter(Encoder))
                            .SetOutputConvertor(new MultiEncoderConverter(Encoder))
                            .SetGenerator(generator)
                            .SetSource(Table)
                            .SetEmbedding(DataBase)
                            .SetPersister(config.canWrite ? new TextEmbeddingTable.PersistHandler() : null)
                            .SetFilter(new TopSimilarityFilter(config.inputThreshold, config.outputThreshold)) as TPipeline;
        }
        public void ReleasePipeline()
        {
            pipeline?.Dispose();
            pipeline = null;
        }
        public void Dispose()
        {
            ReleasePipeline();
            Encoder?.Dispose();
            DataBase.Dispose();
        }
        public async UniTask<GenerateContext> RunPipeline()
        {
            var inputs = Splitter.Split(generator.GetHistoryContext());
            var context = new GenerateContext(inputs);
            await pipeline.Run(context);
            return context;
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