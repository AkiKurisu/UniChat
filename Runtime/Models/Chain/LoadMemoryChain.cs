using System;
using Cysharp.Threading.Tasks;
using Kurisu.UniChat.Memory;
namespace Kurisu.UniChat.Chains
{
    public class LoadMemoryChain : StackableChain
    {
        private readonly ChatMemory chatMemory;
        public LoadMemoryChain(ChatMemory chatMemory, string outputKey)
        {
            this.chatMemory = chatMemory;
            OutputKeys = new[] { outputKey };
        }
        protected override UniTask<IChainValues> InternalCall(IChainValues values)
        {
            values = values ?? throw new ArgumentNullException(nameof(values));
            values.Value[OutputKeys[0]] = chatMemory.GetMemoryContext();
            return UniTask.FromResult(values);
        }
    }
}