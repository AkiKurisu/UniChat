using System;
using Kurisu.NGDS.NLP;
using Unity.Sentis;
using UnityEngine.Pool;
using UObject = UnityEngine.Object;
namespace Kurisu.UniChat.StateMachine
{
    public class ChatStateMachineCtrl : IDisposable
    {
        public ChatStateMachine[] stateMachines;
        private readonly TextEncoder encoder;
        private readonly ITensorAllocator allocator;
        private readonly Ops ops;
        public UObject HostObject { get; }
        public ChatStateMachineCtrl(TextEncoder encoder, UObject hostObject, int layer)
        {
            this.encoder = encoder;
            HostObject = hostObject;
            stateMachines = new ChatStateMachine[layer];
            allocator = new TensorCachingAllocator();
            ops = WorkerFactory.CreateOps(BackendType.GPUCompute, allocator);
        }
        /// <summary>
        /// Exit layer's current stateMachine and enter to new stateMachine
        /// </summary>
        /// <param name="stateMachine"></param>
        public void SetStateMachine(int layer, ChatStateMachine stateMachine)
        {
            stateMachines[layer]?.ExitStateMachine(HostObject);
            stateMachines[layer] = stateMachine;
            stateMachines[layer].EnterStateMachine(HostObject);
            //default enter first state
            stateMachines[layer].TransitionState(stateMachines[layer].states[0].uniqueId);
            EncodeTransitions(layer);
        }
        /// <summary>
        /// Execute all layers
        /// </summary>
        /// <param name="input"></param>
        public void Execute(string input)
        {
            for (int i = 0; i < stateMachines.Length; ++i)
            {
                Execute(i, input);
            }
        }
        /// <summary>
        /// Execute layer's stateMachine
        /// </summary>
        /// <param name="input"></param>
        public void Execute(int layer, string input)
        {
            var pool = ListPool<string>.Get();
            pool.Add(input);
            try
            {
                stateMachines[layer].Execute(ops, encoder.Encode_Mean_Pooling(ops, pool, true));
            }
            finally
            {
                ListPool<string>.Release(pool);
            }
        }
        /// <summary>
        /// Pre-encode transitions' embedding and set destination value
        /// </summary>
        public void EncodeTransitions(int layer)
        {
            foreach (var state in stateMachines[layer].states)
            {
                foreach (var transition in state.transitions)
                {
                    transition.EncodeConditions(ops, encoder);
                    if (!transition.lazyDestination.IsNull()) transition.destination = stateMachines[layer].GetState(transition.lazyDestination);
                }
            }
        }
        public void Dispose()
        {
            ops.Dispose();
            allocator.Dispose();
        }
    }
}