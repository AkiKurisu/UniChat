using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Kurisu.NGDS.NLP;
using Newtonsoft.Json;
using Unity.Sentis;
using UnityEngine.Pool;
namespace Kurisu.UniChat
{
    public class ChatModelFile
    {
        /// <summary>
        /// Override directory loading path
        /// </summary>
        [JsonIgnore]
        public string directoryOverridePath;
        public string fileName = "ChatModel";
        /// <summary>
        /// Dim according to your embedding model
        /// </summary>
        public int embeddingDim = 512;
        /// <summary>
        /// Embedding model to use
        /// </summary>
        public string embeddingModelName = "bge-small-zh-v1.5";
        /// <summary>
        /// Embedding model provider, default load from UserData/models
        /// </summary>
        public string modelProvider = ModelProviderFactory.UserDataProvider;
        public const string tableFileName = "table.bin";
        public const string graphFileName = "graph.bin";
        public const string configFileName = "model.cfg";
        [JsonIgnore]
        public string DirectoryPath => directoryOverridePath ?? Path.Combine(PathUtil.UserDataPath, fileName);
        [JsonIgnore]
        public string GraphPath => Path.Combine(DirectoryPath, graphFileName);
        [JsonIgnore]
        public string TablePath => Path.Combine(DirectoryPath, tableFileName);
        [JsonIgnore]
        public string ConfigPath => Path.Combine(DirectoryPath, configFileName);
        [JsonIgnore]
        public string ModelPath => $"{embeddingModelName}/model.sentis";
        [JsonIgnore]
        public string TokenizerPath => $"{embeddingModelName}/tokenizer.json";
    }
    public class PipelineConfig
    {
        public BackendType backendType;
        public bool canWrite;
        public float inputThreshold;
        public float outputThreshold;
    }
    public class ChatPipelineCtrl<TPipeline, KTable> : IDisposable
    where TPipeline : ChatPipeline, new()
    where KTable : ISerializable, IEmbeddingTable, new()
    {
        public TextEncoder Encoder { get; protected set; }
        public ChatDataBase DataBase { get; protected set; }
        public KTable Table { get; protected set; }
        public ChatModelFile ChatFile { get; protected set; }
        public ISplitter Splitter { get; protected set; }
        public IChatHistoryQuery HistoryQuery { get; protected set; }
        public IGenerator Generator { get; protected set; }
        public TPipeline Pipeline { get; protected set; }
        public ChatPipelineCtrl(ChatModelFile chatFile)
        {
            ChatFile = chatFile;
            var graphPath = ChatFile.GraphPath;
            if (File.Exists(graphPath))
            {
                DataBase = new(graphPath);
            }
            else
            {
                DataBase = new(ChatFile.embeddingDim);
            }
            var tablePath = ChatFile.TablePath;
            Table = new();
            if (File.Exists(tablePath))
            {
                Table.Load(tablePath);
            }
            Splitter = new SlidingWindowSplitter(256);
        }
        public virtual async UniTask InitializePipeline(IGenerator generator, IChatHistoryQuery historyQuery, PipelineConfig config)
        {
            Generator = generator;
            HistoryQuery = historyQuery;
            Encoder?.Dispose();
            Pipeline?.Dispose();
            ModelProvider provider = ModelProviderFactory.Instance.Create(ChatFile.modelProvider);
            Encoder = new TextEncoder(
                await provider.LoadModel(ChatFile.ModelPath),
                new BertTokenizer(await provider.LoadTokenizer(ChatFile.TokenizerPath)),
                config.backendType
            );
            Pipeline = new TPipeline()
                            .SetBackend(config.backendType)
                            .SetInputConvertor(new ChatPipeline.ContextConverter(Encoder, historyQuery))
                            .SetOutputConvertor(new MultiEncoderConverter(Encoder))
                            .SetGenerator(generator)
                            .SetHistoryQuery(historyQuery)
                            .SetSource(Table)
                            .SetEmbedding(DataBase)
                            .SetPersister(config.canWrite ? new TextEmbeddingTable.PersistHandler() : null)
                            .SetTemperature(config.outputThreshold)
                            .SetFilter(new TopSimilarityFilter(config.inputThreshold)) as TPipeline;
        }
        public void ReleasePipeline()
        {
            Pipeline?.Dispose();
            Pipeline = null;
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
            Splitter.Split(HistoryQuery.GetHistoryContext(), pool);
            var context = new GenerateContext(pool);
            try
            {
                await Pipeline.Run(context);
                return context;
            }
            finally
            {
                ListPool<string>.Release(pool);
            }
        }
        public void SaveModel()
        {
            if (!Directory.Exists(ChatFile.DirectoryPath))
            {
                Directory.CreateDirectory(ChatFile.DirectoryPath);
            }
            File.WriteAllText(ChatFile.ConfigPath, JsonConvert.SerializeObject(ChatFile, Formatting.Indented));
            Table.Save(ChatFile.TablePath);
            DataBase.Save(ChatFile.GraphPath);
        }
    }
}