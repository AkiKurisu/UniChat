using System;
using Cysharp.Threading.Tasks;
namespace UniChat.Chains
{
    public class SetLambdaChain : StackableChain
    {
        public SetLambdaChain(Func<string> queryGetter, string outputKey = "query")
        {
            OutputKeys = new[] { outputKey };
            QueryGetter = queryGetter;
        }

        public Func<string> QueryGetter { get; set; }
        protected override UniTask<IChainValues> InternalCall(IChainValues values)
        {
            values = values ?? throw new ArgumentNullException(nameof(values));

            values.Value[OutputKeys[0]] = QueryGetter();
            return UniTask.FromResult(values);
        }
    }
}