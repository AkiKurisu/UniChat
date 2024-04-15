using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
namespace Kurisu.UniChat.Chains
{
    public class DoChain : StackableChain
    {
        private readonly Action<Dictionary<string, object>> _func;

        public DoChain(Action<Dictionary<string, object>> func)
        {
            _func = func;
        }

        protected override UniTask<IChainValues> InternalCall(IChainValues values)
        {
            values = values ?? throw new ArgumentNullException(nameof(values));
            _func(values.Value);
            return UniTask.FromResult(values);
        }
    }
}