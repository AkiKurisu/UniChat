using System;
using Cysharp.Threading.Tasks;
namespace UniChat.Chains
{
    public class SetChain : StackableChain
    {
        public SetChain(object value, string outputKey = "query")
        {
            OutputKeys = new[] { outputKey };
            Value = value;
        }

        public object Value { get; set; }
        protected override UniTask<IChainValues> InternalCall(IChainValues values)
        {
            values = values ?? throw new ArgumentNullException(nameof(values));
            values.Value[OutputKeys[0]] = Value;
            return UniTask.FromResult(values);
        }
    }
}