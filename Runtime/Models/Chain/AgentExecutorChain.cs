using System;
using Cysharp.Threading.Tasks;
using Kurisu.UniChat.Chains;
namespace Kurisu.UniChat.Tools
{
    public class AgentExecutorChain : StackableChain
    {
        public string HistoryKey { get; }
        private readonly StackableChain _originalChain;
        private StackableChain _chainWithHistory;
        public AgentExecutorChain(
           StackableChain originalChain,
            string name,
            string historyKey = "history",
            string outputKey = "final_answer")
        {
            Name = name;
            HistoryKey = historyKey;
            _originalChain = originalChain;

            InputKeys = new[] { historyKey };
            OutputKeys = new[] { outputKey };

            SetHistory("");
        }

        public void SetHistory(string history)
        {
            _chainWithHistory =
                Chain.Set(history, HistoryKey) |
                _originalChain;
        }
        protected override async UniTask<IChainValues> InternalCall(IChainValues values)
        {
            if (_chainWithHistory == null)
            {
                throw new InvalidOperationException("History is not set");
            }

            return await _chainWithHistory.CallAsync(values);
        }
    }
}
