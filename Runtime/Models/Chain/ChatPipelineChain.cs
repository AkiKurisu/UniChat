using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;

namespace UniChat.Chains
{
    public class ChatPipelineChain : StackableChain
    {
        public ChatPipelineCtrl ChatPipelineCtrl { get; }
        
        public ChatPipelineChain(ChatPipelineCtrl chatPipelineCtrl, string inputKey = "input", string outputKey = "context")
        {
            ChatPipelineCtrl = chatPipelineCtrl;
            InputKeys = new string[1] { inputKey };
            OutputKeys = new[] { outputKey };
        }
        
        protected override async UniTask<IChainValues> InternalCall(IChainValues values)
        {
            values = values ?? throw new ArgumentNullException(nameof(values));
            var context = await ChatPipelineCtrl.RunPipeline((string)values.Value[InputKeys[0]]);
            values.Value[OutputKeys[0]] = context;
            return values;
        }
        
        /// <summary>
        /// Set input from external
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public PipelineStackChain Input(string input)
        {
            return new PipelineStackChain(this, new SetChain(input, InputKeys[0]), this);
        }
        
        public PipelineStackChain CastOutputValue<T>(string outputKey = "value")
        {
            return new PipelineStackChain(this, this, PipelineChain.CastOutputValue<T>(OutputKeys[0], outputKey));
        }
        
        public PipelineStackChain CastStringValue(string outputKey = "stringValue")
        {
            return new PipelineStackChain(this, this, PipelineChain.CastStringValue(OutputKeys[0], outputKey));
        }
        
        public PipelineStackChain SaveModel()
        {
            return new PipelineStackChain(this, this, PipelineChain.SaveModel(ChatPipelineCtrl));
        }
        
        public PipelineStackChain UpdateHistory()
        {
            return new PipelineStackChain(this, this, PipelineChain.UpdateHistory(ChatPipelineCtrl.History, InputKeys[0], OutputKeys[0]));
        }
        
        public PipelineStackChain SaveSession(string savePath = null)
        {
            return new PipelineStackChain(this, this, PipelineChain.SaveSession(ChatPipelineCtrl, savePath));
        }
    }
    
    /// <summary>
    /// A stable stack ensure has pipeline in left and align input output keys
    /// </summary>
    public class PipelineStackChain : StackChain
    {
        public ChatPipelineChain Root { get; }
        
        public PipelineStackChain(ChatPipelineChain root, StackableChain left, StackableChain right) : base(left, right)
        {
            Root = root;
        }
        
        public PipelineStackChain CastOutputValue<T>(string outputKey = "value")
        {
            return new PipelineStackChain(Root, this, PipelineChain.CastOutputValue<T>(Root.OutputKeys[0], outputKey));
        }
        
        public PipelineStackChain CastStringValue(string outputKey = "stringValue")
        {
            return new PipelineStackChain(Root, this, PipelineChain.CastStringValue(Root.OutputKeys[0], outputKey));
        }
        
        public PipelineStackChain UpdateHistory()
        {
            return new PipelineStackChain(Root, this, PipelineChain.UpdateHistory(Root.ChatPipelineCtrl.History, Root.InputKeys[0], Root.OutputKeys[0]));
        }
        
        public PipelineStackChain SaveModel()
        {
            return new PipelineStackChain(Root, this, PipelineChain.SaveModel(Root.ChatPipelineCtrl));
        }
        
        public PipelineStackChain SaveSession(string savePath = null)
        {
            return new PipelineStackChain(Root, this, PipelineChain.SaveSession(Root.ChatPipelineCtrl, savePath));
        }
    }
    
    public class PipelineChain
    {
        public static PipelineCastOutputChain<T> CastOutputValue<T>(
           string inputKey = "context",
           string outputKey = "value")
        {
            return new PipelineCastOutputChain<T>(inputKey, outputKey);
        }
        
        public static PipelineCastStringChain CastStringValue(
           string inputKey = "context",
           string outputKey = "stringValue")
        {
            return new PipelineCastStringChain(inputKey, outputKey);
        }
        
        public static PipelineUpdateHistoryChain UpdateHistory(
            ChatHistory chatHistory,
           string inputKey = "context",
           string outputKey = "value")
        {
            return new PipelineUpdateHistoryChain(chatHistory, inputKey, outputKey);
        }
        
        public static DoChain SaveModel(
            ChatPipelineCtrl chatPipelineCtrl)
        {
            return new DoChain(e => chatPipelineCtrl.SaveModel());
        }
        
        public static DoChain SaveSession(
            ChatPipelineCtrl chatPipelineCtrl, string savePath = null)
        {
            return new DoChain(e => chatPipelineCtrl.SaveSession(string.IsNullOrEmpty(savePath) ? Path.Combine(PathUtil.SessionPath, $"Session_{chatPipelineCtrl.BotName}_{DateTime.Now:yyyyMMddHHmmssfff}.json") : savePath));
        }
    }

    public class PipelineCastOutputChain<T> : StackableChain
    {
        public PipelineCastOutputChain(string inputKey = "context", string outputKey = "value")
        {
            InputKeys = new[] { inputKey };
            OutputKeys = new[] { outputKey };
        }
        
        protected override UniTask<IChainValues> InternalCall(IChainValues values)
        {
            values = values ?? throw new ArgumentNullException(nameof(values));
            var context = (GenerateContext)values.Value[InputKeys[0]];
            values.Value[OutputKeys[0]] = context.CastOutputValue<T>();
            return UniTask.FromResult(values);
        }
    }
    
    public class PipelineCastStringChain : StackableChain
    {
        public PipelineCastStringChain(string inputKey = "context", string outputKey = "stringValue")
        {
            InputKeys = new[] { inputKey };
            OutputKeys = new[] { outputKey };
        }
        
        protected override UniTask<IChainValues> InternalCall(IChainValues values)
        {
            values = values ?? throw new ArgumentNullException(nameof(values));
            var context = values.Value[InputKeys[0]] as GenerateContext;
            Assert.IsNotNull(context);
            values.Value[OutputKeys[0]] = context.CastStringValue();
            return UniTask.FromResult(values);
        }
    }
    
    public class PipelineUpdateHistoryChain : StackableChain
    {
        private readonly ChatHistory _chatHistory;
        
        private readonly string _inputKey;
        
        private readonly string _outputKey;
        
        public PipelineUpdateHistoryChain(ChatHistory chatHistory, string inputKey = "input", string outputKey = "context")
        {
            _chatHistory = chatHistory;
            _inputKey = inputKey;
            _outputKey = outputKey;
        }
        
        protected override UniTask<IChainValues> InternalCall(IChainValues values)
        {
            values = values ?? throw new ArgumentNullException(nameof(values));
            _chatHistory.AppendUserMessage((string)values.Value[_inputKey]);
            var context = values.Value[_outputKey] as GenerateContext;
            Assert.IsNotNull(context);
            _chatHistory.AppendBotMessage(context.CastStringValue());
            return UniTask.FromResult(values);
        }
    }
}