using System;
using Cysharp.Threading.Tasks;
namespace Kurisu.UniChat.Chains
{
    public class ChatPipelineChain : StackableChain
    {
        private readonly ChatPipelineCtrl chatPipelineCtrl;
        public ChatPipelineChain(ChatPipelineCtrl chatPipelineCtrl, string outputKey = "context")
        {
            this.chatPipelineCtrl = chatPipelineCtrl;
            OutputKeys = new[] { outputKey };
        }
        protected override async UniTask<IChainValues> InternalCall(IChainValues values)
        {
            values = values ?? throw new ArgumentNullException(nameof(values));
            var context = await chatPipelineCtrl.RunPipeline();
            values.Value[OutputKeys[0]] = context;
            return values;
        }
        public StackableChain CastValue<T>(string outputKey = "value")
        {
            return this | new ContextCastChain<T>(OutputKeys[0], outputKey);
        }
        public StackableChain CastStringValue(string outputKey = "stringValue")
        {
            return this | new ContextCastStringChain(OutputKeys[0], outputKey);
        }
    }
    public class ContextCastChain<T> : StackableChain
    {
        public ContextCastChain(string inputKey = "context", string outputKey = "value")
        {
            InputKeys = new[] { inputKey };
            OutputKeys = new[] { outputKey };
        }
        protected override UniTask<IChainValues> InternalCall(IChainValues values)
        {
            values = values ?? throw new ArgumentNullException(nameof(values));
            var context = values.Value[InputKeys[0]] as GenerateContext;
            values.Value[OutputKeys[0]] = context.CastOutputValue<T>();
            return UniTask.FromResult(values);
        }
    }
    public class ContextCastStringChain : StackableChain
    {
        public ContextCastStringChain(string inputKey = "context", string outputKey = "stringValue")
        {
            InputKeys = new[] { inputKey };
            OutputKeys = new[] { outputKey };
        }
        protected override UniTask<IChainValues> InternalCall(IChainValues values)
        {
            values = values ?? throw new ArgumentNullException(nameof(values));
            var context = values.Value[InputKeys[0]] as GenerateContext;
            values.Value[OutputKeys[0]] = context.CastStringValue();
            return UniTask.FromResult(values);
        }
    }
}