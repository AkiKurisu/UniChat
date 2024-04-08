using System;
using Kurisu.NGDS.NLP;
using Unity.Collections;
using Unity.Sentis;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using UObject = UnityEngine.Object;
namespace Kurisu.UniChat.StateMachine
{
    public class ChatState
    {
        public string name;
        public uint uniqueId;
        public ChatStateTransition[] transitions = new ChatStateTransition[0];
        public ChatStateMachineBehavior[] behaviors = new ChatStateMachineBehavior[0];
        public void CollectEmbedding(
            int embedding_dim,
            ref NativeArray<uint> ids,
            ref NativeArray<byte> modes,
            ref NativeArray<float> thresholds,
            ref NativeArray<float> embeddings
        )
        {
            int length = 0;
            for (int i = 0; i < transitions.Length; ++i)
            {
                length += transitions[i].conditions.Length;
            }
            ids.Resize(length);
            modes.Resize(length);
            thresholds.Resize(length);
            embeddings.Resize(length * embedding_dim);
            int id = 0;
            for (int i = 0; i < transitions.Length; ++i)
            {
                for (int j = 0; j < transitions[i].conditions.Length; ++j)
                {
                    Assert.IsTrue(transitions[i].embeddings[j].values.Length == embedding_dim);
                    ids[id] = transitions[i].destination.uniqueId;
                    modes[id] = (byte)transitions[i].conditions[j].mode;
                    thresholds[id] = transitions[i].conditions[j].threshold;
                    NativeArray<float>.Copy(transitions[i].embeddings[j].values, 0, embeddings, id * embedding_dim, embedding_dim);
                    ++id;
                }
            }
        }
        public void EnterState()
        {
            for (int i = 0; i < behaviors.Length; ++i)
            {
                behaviors[i].OnStateEnter();
            }
        }
        public void UpdateState()
        {
            for (int i = 0; i < behaviors.Length; ++i)
            {
                behaviors[i].OnStateUpdate();
            }
        }
        public void ExistState()
        {
            for (int i = 0; i < behaviors.Length; ++i)
            {
                behaviors[i].OnStateExit();
            }
        }
        public void AddTransition(ChatStateTransition transition)
        {
            ChatStateTransition[] transitionsVector = transitions;
            ArrayUtils.Add(ref transitionsVector, transition);
            transitions = transitionsVector;
        }

        public void RemoveTransition(ChatStateTransition transition)
        {
            ChatStateTransition[] transitionsVector = transitions;
            ArrayUtils.Remove(ref transitionsVector, transition);
            transitions = transitionsVector;
        }
        public void AddBehavior<T>() where T : ChatStateMachineBehavior, new()
        {
            ChatStateMachineBehavior behavior = new T();
            AddBehavior(behavior);
        }
        public void RemoveBehavior<T>() where T : ChatStateMachineBehavior, new()
        {
            ChatStateMachineBehavior behavior = ArrayUtils.Find(behaviors, x => x.GetType() == typeof(T));
            if (behavior != null)
                RemoveBehavior(behavior);
        }
        public void AddBehavior(ChatStateMachineBehavior behavior)
        {
            ChatStateMachineBehavior[] behaviorsVector = behaviors;
            ArrayUtils.Add(ref behaviorsVector, behavior);
            behaviors = behaviorsVector;
        }
        public void RemoveBehavior(ChatStateMachineBehavior behavior)
        {
            ChatStateMachineBehavior[] behaviorsVector = behaviors;
            ArrayUtils.Remove(ref behaviorsVector, behavior);
            behaviors = behaviorsVector;
        }
    }
    /// <summary>
    /// Deferred set chatState value
    /// </summary>
    public readonly struct LazyStateReference
    {
        public readonly uint uniqueId;
        public LazyStateReference(string stateName)
        {
            uniqueId = XXHash.CalculateHash(stateName);
        }
        public readonly bool IsNull() => uniqueId == default;
        public static implicit operator uint(LazyStateReference lazyStateReference)
        {
            return lazyStateReference.uniqueId;
        }
    }
    public class ChatStateTransition
    {
        public LazyStateReference lazyDestination;
        public ChatState destination;
        public ChatCondition[] conditions = new ChatCondition[0];
        public Embedding[] embeddings = new Embedding[0];
        public ChatStateTransition() { }
        public ChatStateTransition(LazyStateReference lazyDestination)
        {
            this.lazyDestination = lazyDestination;
        }
        public void AddCondition(ChatConditionMode mode, float threshold, string parameter)
        {
            ChatCondition[] conditionVector = conditions;
            ChatCondition newCondition = new()
            {
                mode = mode,
                parameter = parameter,
                threshold = threshold
            };
            ArrayUtils.Add(ref conditionVector, newCondition);
            conditions = conditionVector;
        }
        public void RemoveCondition(ChatCondition condition)
        {
            ChatCondition[] conditionVector = conditions;
            ArrayUtils.Remove(ref conditionVector, condition);
            conditions = conditionVector;
        }
        public void EncodeConditions(Ops ops, TextEncoder encoder)
        {
            var pool = ListPool<string>.Get();
            foreach (var condition in conditions)
            {
                pool.Add(condition.parameter);
            }
            var tensors = encoder.Encode_Mean_Pooling(ops, pool, true);
            ListPool<string>.Release(pool);
            var embeddingVector = embeddings;
            Array.Resize(ref embeddingVector, conditions.Length);
            for (int i = 0; i < conditions.Length; ++i)
            {
                embeddingVector[i] = new Embedding() { values = tensors.ToArray(i) };
            }
            embeddings = embeddingVector;
        }
    }
    public enum ChatConditionMode : byte
    {
        //skip evaluation as direct transition
        None = 0,
        Greater = 1,
        GreaterOrEqual = 2,
        Less = 3,
        LessOrEqual = 4,
        Equals = 5,
        NotEqual = 6,
    }
    public class ChatCondition
    {
        public ChatConditionMode mode;
        public float threshold;
        public string parameter;
    }
    public class ChatStateMachineBehavior
    {
        public virtual void OnStateMachineEnter(UObject hostObject) { }
        public virtual void OnStateMachineExit(UObject hostObject) { }
        public virtual void OnStateEnter() { }
        public virtual void OnStateUpdate() { }
        public virtual void OnStateExit() { }
    }
}