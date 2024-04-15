using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
namespace Kurisu.UniChat.Chains
{
    public class SplitChain : StackableChain
    {
        public SplitChain(ISplitter splitter, string inputKey = "text", string outputKey = "splitted_text")
        {
            InputKeys = new[] { inputKey };
            OutputKeys = new[] { outputKey };
            Splitter = splitter;
        }

        public ISplitter Splitter { get; set; }
        private readonly List<string> splitted_text = new();
        protected override UniTask<IChainValues> InternalCall(IChainValues values)
        {
            values = values ?? throw new ArgumentNullException(nameof(values));
            var input = values.Value[InputKeys[0]];
            if (input is not string stringValue) throw new ArgumentException(nameof(input));
            splitted_text.Clear();
            Splitter.Split(stringValue, splitted_text);
            values.Value[OutputKeys[0]] = splitted_text;
            return UniTask.FromResult(values);
        }
    }
}