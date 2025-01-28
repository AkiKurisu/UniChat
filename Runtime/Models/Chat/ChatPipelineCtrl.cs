using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UniChat.LLMs;
using UniChat.Memory;
using UniChat.NLP;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
namespace UniChat
{
    [Serializable]
    public class PipelineConfig
    {
        /// <summary>
        /// Run pipeline on which backend
        /// </summary>
        public BackendType backendType = BackendType.GPUCompute;
        
        /// <summary>
        /// Log pipeline status
        /// </summary>
        public bool verbose;
        
        /// <summary>
        /// Whether write data to source table and dataBase
        /// </summary>
        public bool canWrite = true;
        
        /// <summary>
        /// Pipeline input score threshold
        /// </summary>
        public float inputThreshold = 0.85f;
        
        /// <summary>
        /// Pipeline output score threshold
        /// </summary>
        public float outputThreshold = 0.85f;
        
        public static readonly PipelineConfig Default = new()
        {
            backendType = BackendType.GPUCompute,
            canWrite = true,
            verbose = false,
            inputThreshold = 0.85f,
            outputThreshold = 0.85f
        };
    }
    
    public class InputGenerationRequest
    {
        public readonly GenerateContext GenerateContext;
        
        public UniTaskCompletionSource<bool> WaitSource;
        
        public InputGenerationRequest(GenerateContext generateContext)
        {
            GenerateContext = generateContext;
            WaitSource = new UniTaskCompletionSource<bool>();
        }
        
        public void SetResult(string generatedContent)
        {
            GenerateContext.generatedContent = generatedContent;
            WaitSource?.TrySetResult(true);
            WaitSource = null;
        }
        
        public void Cancel()
        {
            WaitSource?.TrySetResult(false);
            WaitSource = null;
        }
    }
    
    public static class ChatGeneratorIds
    {
        public const uint Input = 0;
        
        public const uint OpenAI = 1;
        
        public const uint TextGenWebUI = 2;
        
        public const uint Ollama = 3;
        
        public const uint KoboldCpp = 4;
    }
    
    public static class SplitterFactory
    {
        public static ISplitter CreateSplitter(string splitter, string pattern)
        {
            Type splitterType = splitter switch
            {
                nameof(SlidingWindowSplitter) => typeof(SlidingWindowSplitter),
                nameof(RegexSplitter) => typeof(RegexSplitter),
                nameof(RecursiveCharacterTextSplitter) => typeof(RecursiveCharacterTextSplitter),
                _ => throw new ArgumentOutOfRangeException(nameof(splitterType)),
            };
            if (!string.IsNullOrEmpty(pattern))
                return JsonConvert.DeserializeObject(pattern, splitterType) as ISplitter;
            return Activator.CreateInstance(splitterType) as ISplitter;
        }
    }
    
    public static class MemoryFactory
    {
        public static ChatMemory CreateMemory(string memory, string pattern)
        {
            Type memoryType = memory switch
            {
                nameof(ChatBufferMemory) => typeof(ChatBufferMemory),
                nameof(ChatWindowBufferMemory) => typeof(ChatWindowBufferMemory),
                _ => throw new ArgumentOutOfRangeException(nameof(memoryType)),
            };
            if (!string.IsNullOrEmpty(pattern))
                return JsonConvert.DeserializeObject(pattern, memoryType) as ChatMemory;
            else
                return Activator.CreateInstance(memoryType) as ChatMemory;
        }
    }
    
    public class ChatPipelineCtrl<TPipeline, TKTable> : IDisposable, IGenerator
    where TPipeline : ChatPipeline, new()
    where TKTable : ISerializable, IEmbeddingTable, IPersistHandlerFactory<string>, new()
    {
        #region Public Properties
        public ChatDataBase DataBase { get; protected set; }
        
        public TKTable Table { get; protected set; }
        
        public ChatModelFile ChatFile { get; protected set; }
        
        public ChatHistory History { get; } = new();
        
        #endregion
        
        // Properties that may change in pipeline ctrl lifetime scope
        #region Protected Properties
        protected BertEncoder Encoder { get; set; }
        
        protected ISplitter Splitter { get; set; }
        
        protected TPipeline Pipeline { get; set; }
        
        protected ChatMemory Memory { get; set; }
        
        #endregion
        
        public string Context { get => Memory.Context; set => Memory.Context = value; }
        
        public string UserName { get => Memory.UserName; set => Memory.UserName = value; }
        
        public string BotName { get => Memory.BotName; set => Memory.BotName = value; }
        
        public Action<InputGenerationRequest> OnCallGeneration;
        
        private readonly Dictionary<uint, IChatModel> _chatModelCache = new();
        
        private PipelineConfig _config;
        
        private readonly ChatModelFactory _chatModelFactory;
        
        private bool _isDirty;
        
        private uint _generatorId = ChatGeneratorIds.Input;
        
        public ChatPipelineCtrl(ChatModelFile chatFile, ILLMSettings llmSettings)
        {
            ChatFile = chatFile;
            var graphPath = ChatFile.GraphPath;
            if (File.Exists(graphPath))
            {
                DataBase = new ChatDataBase(graphPath);
            }
            else
            {
                DataBase = new ChatDataBase(ChatFile.embeddingDim);
            }
            var tablePath = ChatFile.TablePath;
            Table = new TKTable();
            if (File.Exists(tablePath))
            {
                Table.Load(tablePath);
            }
            _chatModelFactory = new ChatModelFactory(llmSettings);
            Splitter = SplitterFactory.CreateSplitter(chatFile.splitter, chatFile.splitterPattern);
            Assert.IsNotNull(Splitter);
            Memory = MemoryFactory.CreateMemory(chatFile.memory, chatFile.memoryPattern);
            Assert.IsNotNull(Memory);
            Memory.ChatHistory = History;
        }
        
        /// <summary>
        /// Set and save splitter
        /// </summary>
        /// <param name="splitter"></param>
        public void SetSplitter(ISplitter splitter)
        {
            Splitter = splitter;
            ChatFile.splitter = splitter.GetType().Name;
            ChatFile.splitterPattern = JsonConvert.SerializeObject(splitter);
            SetDirty(true);
        }
        
        /// <summary>
        /// Save and set memory
        /// </summary>
        /// <param name="memory"></param>
        public void SetMemory(ChatMemory memory)
        {
            Memory = memory;
            Memory.ChatHistory = History;
            ChatFile.memory = memory.GetType().Name;
            ChatFile.memoryPattern = JsonConvert.SerializeObject(memory);
            SetDirty(true);
        }
        
        /// <summary>
        /// Initialize pipeline
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public virtual async UniTask InitializePipeline(PipelineConfig config = null)
        {
            config ??= PipelineConfig.Default;
            _config = config;
            Encoder?.Dispose();
            Pipeline?.Dispose();
            ModelProvider provider = ModelProviderFactory.Instance.Create(ChatFile.modelProvider);
            Encoder = new BertEncoder(
                await provider.LoadModel(ChatFile.ModelPath),
                new BertTokenizer(await provider.LoadTokenizer(ChatFile.TokenizerPath)),
                config.backendType
            );
            Pipeline = new TPipeline()
                            .SetBackend(config.backendType)
                            .SetEncoder(Encoder)
                            .SetGenerator(this)
                            .SetMemory(Memory)
                            .SetSource(Table)
                            .SetEmbedding(DataBase)
                            .SetPersister(config.canWrite ? Table.CreatePersistHandler() : null)
                            .SetTemperature(config.outputThreshold)
                            .SetVerbose(config.verbose)
                            .SetFilter(new TopSimilarityFilter(config.inputThreshold)) as TPipeline;
            SetDirty(false);
        }

        /// <summary>
        /// Reinitialize pipeline if property changed
        /// </summary>
        public async UniTask ReinitializeIfNeed()
        {
            if (!_isDirty) return;
            await InitializePipeline(_config);
        }
        
        /// <summary>
        /// Set dirty flag to notify pipeline need reinitialize
        /// </summary>
        /// <param name="isDirty"></param>
        public void SetDirty(bool isDirty)
        {
            _isDirty = isDirty;
        }
        
        public void Dispose()
        {
            Pipeline?.Dispose();
            Encoder?.Dispose();
            DataBase?.Dispose();
        }
        
        /// <summary>
        /// Run pipeline using history context
        /// </summary>
        /// <returns></returns>
        public async UniTask<GenerateContext> RunPipeline()
        {
            await ReinitializeIfNeed();
            var pool = ListPool<string>.Get();
            Splitter.Split(Memory.GetMemoryContext(), pool);
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
        
        /// <summary>
        /// Run pipeline using history context and new input
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async UniTask<GenerateContext> RunPipeline(string input)
        {
            await ReinitializeIfNeed();
            var pool = ListPool<string>.Get();
            History.AppendUserMessage(input);
            Splitter.Split(Memory.GetMemoryContext(), pool);
            var context = new GenerateContext(pool);
            try
            {
                await Pipeline.Run(context);
                return context;
            }
            finally
            {
                //This message is temporary and should be added manually after pipeline
                History.RemoveLastUserMessage();
                ListPool<string>.Release(pool);
            }
        }
        
        /// <summary>
        /// Change generator mode
        /// </summary>
        /// <param name="generatorId"></param>
        /// <param name="forceNewChatModel"></param>
        public void SwitchGenerator(uint generatorId, bool forceNewChatModel)
        {
            this._generatorId = generatorId;
            if (generatorId > ChatGeneratorIds.Input)
            {
                var llmType = generatorId switch
                {
                    ChatGeneratorIds.OpenAI => LLMType.OpenAI,
                    ChatGeneratorIds.TextGenWebUI => LLMType.TextGenWebUI,
                    ChatGeneratorIds.Ollama => LLMType.Ollama_Chat,
                    ChatGeneratorIds.KoboldCpp => LLMType.KoboldCpp,
                    _ => throw new ArgumentOutOfRangeException(nameof(generatorId))
                };
                if (forceNewChatModel || !_chatModelCache.ContainsKey(generatorId))
                {
                    _chatModelCache[generatorId] = _chatModelFactory.CreateChatModel(llmType);
                }
            }
        }
        
        private UniTaskCompletionSource<bool> OnInputGeneration(GenerateContext generateContext)
        {
            var request = new InputGenerationRequest(generateContext);
            OnCallGeneration?.Invoke(request);
            return request.WaitSource;
        }
        
        /// <summary>
        /// Generate content directly
        /// </summary>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async UniTask<bool> Generate(GenerateContext context, CancellationToken ct)
        {
            try
            {
                if (_generatorId == ChatGeneratorIds.Input) return await OnInputGeneration(context).Task;
                var llmData = await _chatModelCache[_generatorId].GenerateAsync(Memory, ct);
                context.generatedContent = llmData.Response;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Save chat model
        /// </summary>
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
        
        /// <summary>
        /// Save chat session
        /// </summary>
        /// <param name="filePath"></param>
        public void SaveSession(string filePath)
        {
            File.WriteAllText(filePath, JsonConvert.SerializeObject(History.SaveSession(), Formatting.Indented));
        }
        
        /// <summary>
        /// Load chat session
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool LoadSession(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }
            var session = JsonConvert.DeserializeObject<ChatSession>(File.ReadAllText(filePath));
            History.LoadSession(session); ;
            return true;
        }
        
        /// <summary>
        /// Load chat session
        /// </summary>
        /// <param name="chatSession"></param>
        /// <returns></returns>
        public bool LoadSession(ChatSession chatSession)
        {
            History.LoadSession(chatSession);
            return true;
        }
        
        /// <summary>
        /// Embed from chat session, need initialize pipeline first
        /// </summary>
        /// <param name="chatSession"></param>
        /// <param name="memory"></param>
        /// <returns></returns>
        public async UniTask<bool> EmbedSession(ChatSession chatSession, ChatMemory memory = null)
        {
            if (Pipeline == null)
            {
                Debug.LogWarning("Pipeline should be initialized before embedding session");
                return false;
            }
            await ReinitializeIfNeed();
            memory ??= MemoryFactory.CreateMemory(ChatFile.memory, ChatFile.memoryPattern);
            memory.ChatHistory = new();
            memory.Context = Context;
            memory.BotName = BotName;
            memory.UserName = UserName;
            using var sessionPipeline = new SessionPipeline()
                            .SetEncoder(Encoder)
                            .SetSource(Table)
                            .SetPersister(Table.CreatePersistHandler())
                            .SetEmbedding(DataBase)
                            .SetSplitter(Splitter)
                            .SetMemory(memory)
                            .SetBackend(_config.backendType);
            await sessionPipeline.Run(chatSession);
            return true;
        }
        
        /// <summary>
        /// Embed from chat session, need initialize pipeline first. 
        /// You can directly use session file from Oobabooga to embed.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="memory"></param>
        /// <returns></returns>
        public async UniTask<bool> EmbedSession(string filePath, ChatMemory memory = null)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }
            var session = JsonConvert.DeserializeObject<ChatSession>(File.ReadAllText(filePath));
            return await EmbedSession(session, memory);
        }
    }
    
    /// <summary>
    /// Default pipeline ctrl for <see cref="TextEmbeddingTable"/>
    /// </summary>
    public class ChatPipelineCtrl : ChatPipelineCtrl<ChatPipeline, TextEmbeddingTable>
    {
        public ChatPipelineCtrl(ChatModelFile chatFile, ILLMSettings llmSettings) : base(chatFile, llmSettings)
        {
        }
    }
}