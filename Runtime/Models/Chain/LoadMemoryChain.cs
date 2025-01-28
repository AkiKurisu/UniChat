using System;
using Cysharp.Threading.Tasks;
using UniChat.Memory;

namespace UniChat.Chains
{
    public class LoadMemoryChain : StackableChain
    {
        private readonly ChatMemory _chatMemory;
        
        public LoadMemoryChain(ChatMemory chatMemory, string outputKey)
        {
            _chatMemory = chatMemory;
            OutputKeys = new[] { outputKey };
        }
        
        protected override UniTask<IChainValues> InternalCall(IChainValues values)
        {
            values = values ?? throw new ArgumentNullException(nameof(values));
            values.Value[OutputKeys[0]] = _chatMemory.GetMemoryContext();
            return UniTask.FromResult(values);
        }
    }
}