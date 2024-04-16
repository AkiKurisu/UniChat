using System;
using Cysharp.Threading.Tasks;
using Kurisu.UniChat.StateMachine;
namespace Kurisu.UniChat.Chains
{
    public class StateMachineChain : StackableChain
    {
        public StateMachineChain(ChatStateMachineCtrl chatStateMachineCtrl, string inputKey = "text")
        {
            InputKeys = new[] { inputKey };
            ChatStateMachineCtrl = chatStateMachineCtrl;
        }
        public ChatStateMachineCtrl ChatStateMachineCtrl { get; set; }
        protected override async UniTask<IChainValues> InternalCall(IChainValues values)
        {
            values = values ?? throw new ArgumentNullException(nameof(values));
            await ChatStateMachineCtrl.Execute((string)values.Value[InputKeys[0]]);
            return values;
        }
    }
}