using System;
using System.IO;
using Newtonsoft.Json;
using UniChat.Memory;
using UniChat.NLP;

namespace UniChat
{
    [Serializable]
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
        public string modelProvider = ModelProvider.UserDataProvider;
        
        public string splitter = nameof(SlidingWindowSplitter);
        
        public string splitterPattern = "";
        
        public string memory = nameof(ChatBufferMemory);
        
        public string memoryPattern = "";
        
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
}