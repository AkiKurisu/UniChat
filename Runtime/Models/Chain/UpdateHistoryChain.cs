using System;
using Cysharp.Threading.Tasks;
namespace Kurisu.UniChat.Chains
{
    public class UpdateHistoryChain : StackableChain
    {
        private readonly ChatHistory chatHistory;
        private readonly string requestKey;
        private readonly string responseKey;
        public UpdateHistoryChain(ChatHistory chatHistory, string requestKey = "query", string responseKey = "text")
        {
            this.chatHistory = chatHistory;
            this.requestKey = requestKey;
            this.responseKey = responseKey;
        }

        protected override UniTask<IChainValues> InternalCall(IChainValues values)
        {
            values = values ?? throw new ArgumentNullException(nameof(values));
            chatHistory.AppendUserMessage((string)values.Value[requestKey]);
            chatHistory.AppendBotMessage((string)values.Value[responseKey]);
            return UniTask.FromResult(values);
        }
    }
}