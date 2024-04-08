using System;
using Unity.Collections;
using Unity.Sentis;
using UnityEngine;
using UObject = UnityEngine.Object;
namespace Kurisu.UniChat.StateMachine
{
    public class ChatStateMachine
    {
        public ChatState[] states = new ChatState[0];
        public ChatState CurrentState { get; private set; }
        private readonly int dim;
        public event Action<uint, uint> OnStateChanged;
        public ChatStateMachine(int dim)
        {
            this.dim = dim;
        }
        public ChatState GetState(uint id)
        {
            for (int i = 0; i < states.Length; ++i)
            {
                if (states[i].uniqueId == id) return states[i];
            }
            return null;
        }
        public void Execute(Ops ops, TensorFloat inputTensor)
        {
            if (TryGetNextState(ops, inputTensor, out uint id))
            {
                TransitionState(id);
            }
            //Check state has direct transitions
            const int stackCount = 10;
            int count = 0;
            //Prevent stack overflow
            while (count < stackCount && TryGetNextState(out id))
            {
                count++;
                TransitionState(id);
            }
        }
        public bool TryGetNextState(Ops ops, TensorFloat inputTensor, out uint id)
        {
            NativeArray<uint> ids = default;
            NativeArray<float> thresholds = default;
            NativeArray<float> embeddings = default;
            NativeArray<byte> modes = default;
            id = 0;
            try
            {
                CurrentState.CollectEmbedding(dim, ref ids, ref modes, ref thresholds, ref embeddings);
                TensorFloat comparedTensor = new(new TensorShape(ids.Length, dim), embeddings);
                //TODO: Consider to use reranker model if has priority
                TensorFloat scores = ops.CosineSimilarity(inputTensor, comparedTensor);
                scores.MakeReadable();
                for (int i = 0; i < ids.Length; ++i)
                {
                    if (EvaluateCondition((ChatConditionMode)modes[i], scores[i], thresholds[i]))
                    {
                        id = ids[i];
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                ids.Dispose();
                thresholds.Dispose();
                embeddings.Dispose();
                modes.Dispose();
            }
        }
        public bool TryGetNextState(out uint id)
        {
            id = 0;
            foreach (var transition in CurrentState.transitions)
            {
                foreach (var condition in transition.conditions)
                {
                    if (condition.mode == ChatConditionMode.None)
                    {
                        id = transition.destination.uniqueId;
                        return true;
                    }
                }
            }
            return false;
        }
        private static bool EvaluateCondition(ChatConditionMode mode, float input, float threshold)
        {
            return mode switch
            {
                ChatConditionMode.None => true,
                ChatConditionMode.Greater => input > threshold,
                ChatConditionMode.GreaterOrEqual => input >= threshold,
                ChatConditionMode.Less => input < threshold,
                ChatConditionMode.LessOrEqual => input <= threshold,
                ChatConditionMode.Equals => input == threshold,
                ChatConditionMode.NotEqual => input != threshold,
                _ => throw new ArgumentOutOfRangeException(nameof(mode)),
            };
        }
        public void EnterStateMachine(UObject hostObject)
        {
            foreach (var state in states)
            {
                foreach (var behavior in state.behaviors)
                {
                    behavior.OnStateMachineEnter(hostObject);
                }
            }
        }
        public void ExitStateMachine(UObject hostObject)
        {
            foreach (var state in states)
            {
                foreach (var behavior in state.behaviors)
                {
                    behavior.OnStateMachineExit(hostObject);
                }
            }
        }
        public void TransitionState(string stateName)
        {
            TransitionState(XXHash.CalculateHash(stateName));
        }
        public void TransitionState(uint id)
        {
            var state = GetState(id);
            if (state == null)
            {
                Debug.LogWarning($"State {id} not exists in state machine.");
                return;
            }
            TransitionState_Imp(state);
        }
        private void TransitionState_Imp(ChatState newState)
        {
            uint source = CurrentState?.uniqueId ?? default;
            CurrentState?.ExistState();
            CurrentState = newState;
            CurrentState.EnterState();
            uint destination = CurrentState.uniqueId;
            OnStateChanged?.Invoke(source, destination);
        }
        public void AddState(string stateName)
        {
            AddState(new ChatState() { name = stateName, uniqueId = XXHash.CalculateHash(stateName) });
        }
        public void AddState(ChatState state)
        {
            ChatState[] childStates = states;
            if (Array.Exists(childStates, childState => childState.uniqueId == state.uniqueId))
            {
                Debug.LogWarning(string.Format("State '{0}' already exists in state machine, discarding new state.", state.name));
                return;
            }
            ArrayUtils.Add(ref childStates, state);
            states = childStates;
        }
        public void RemoveState(ChatState state)
        {
            ChatState[] childStates = states;
            ArrayUtils.Remove(ref childStates, state);
            states = childStates;
        }
    }
}