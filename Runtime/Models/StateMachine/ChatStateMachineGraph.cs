using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;
namespace Kurisu.UniChat.StateMachine
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class FormerlySerializedTypeAttribute : Attribute
    {
        private readonly string m_oldSerializedType;

        public string OldSerializedType => m_oldSerializedType;
        public FormerlySerializedTypeAttribute(string oldSerializedType)
        {
            m_oldSerializedType = oldSerializedType;
        }
    }
    public static class BehaviorUpdater
    {
        private static Dictionary<string, Type> updatableType;
        private static void Initialize()
        {
            updatableType = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                                    .Where(x => x.IsSubclassOf(typeof(ChatStateMachineBehavior)) && x.GetCustomAttribute<FormerlySerializedTypeAttribute>() != null)
                                    .ToDictionary(x => x.GetCustomAttribute<FormerlySerializedTypeAttribute>().OldSerializedType, x => x);
        }
        public static bool TryGetUpdateType(string stringType, out Type type)
        {
            if (updatableType == null) Initialize();
            return updatableType.TryGetValue(stringType, out type);
        }
    }
    [Serializable]
    public class ChatStateMachineGraph
    {
        [Serializable]
        public class StateNode
        {
            [HideInInspector]
            public int uniqueId;
            [Tooltip("State Unique Name")]
            public string name;
            [Tooltip("State's transitions")]
            public Edge[] transitions;
            [Tooltip("State's behaviors")]
            public BehaviorNode[] behaviors;
        }
        [Serializable]
        public class BehaviorNode
        {
            [HideInInspector]
            public string serializedType;
            [HideInInspector]
            public string jsonData;
#if UNITY_EDITOR
            [SerializeField]
            internal SerializedBehaviorWrapper container;
#endif
            public ChatStateMachineBehavior Deserialize()
            {
                var behaviorType = SerializedType.FromString(serializedType);
                if (behaviorType == null)
                {
                    if (!BehaviorUpdater.TryGetUpdateType(serializedType, out Type updateType))
                    {
                        behaviorType = typeof(InvalidStateMachineBehavior);
                        string missingType = serializedType;
                        serializedType = SerializedType.ToString(behaviorType);
                        Debug.LogWarning($"Missing type {missingType} when deserialize {nameof(ChatStateMachineBehavior)}");
                        return new InvalidStateMachineBehavior() { missingType = missingType, serializedData = jsonData };
                    }
                    serializedType = SerializedType.ToString(updateType);
                    behaviorType = updateType;
                }
                return JsonConvert.DeserializeObject(jsonData, behaviorType) as ChatStateMachineBehavior;
            }
        }
        /// <summary>
        /// Serialized object for custom state machine behavior.
        /// </summary>
        public abstract class SerializedBehaviorWrapper : ScriptableObject
        {
            public abstract object Value
            {
                get;
                set;
            }
        }
        [Serializable]
        public class Edge
        {
            [Tooltip("Destination state")]
            public string destination;
            [Tooltip("Transition conditions")]
            public ChatCondition[] conditions;
        }
        [Serializable]
        public class Layer
        {
            [Tooltip("Embedding dim of this layer")]
            public int dim = 512;
            public StateNode[] states;
            public ChatStateMachine Deserialize()
            {
                var stateMachine = new ChatStateMachine(dim);
                foreach (var stateNode in states)
                {
                    var state = new ChatState
                    {
                        name = stateNode.name,
                        //We only need state unique name for each state
                        uniqueId = XXHash.CalculateHash(stateNode.name),
                        transitions = CreateTransitions(stateNode.transitions),
                        behaviors = CreateBehaviors(stateNode.behaviors)
                    };
                    stateMachine.AddState(state);
                }
                return stateMachine;
            }

            public ChatStateMachineBehavior[] CreateBehaviors(BehaviorNode[] behaviors)
            {
                return behaviors.Select(b => b.Deserialize()).ToArray();
            }

            public ChatStateTransition[] CreateTransitions(Edge[] edges)
            {
                return edges.Select(e => new ChatStateTransition(e.destination)
                {
                    conditions = e.conditions
                }).ToArray();
            }
            public void Serialize(ChatStateMachine stateMachine)
            {
                int referenceId = 1000;
                var stateNodeMap = stateMachine.states.ToDictionary(x => x, x => new StateNode()
                {
                    uniqueId = referenceId++,
                    name = x.name
                });
                stateNodeMap.ForEach(kv =>
                {
                    kv.Value.behaviors = kv.Key.behaviors.Select(x => new BehaviorNode()
                    {
                        serializedType = SerializedType.ToString(x.GetType()),
                        jsonData = JsonConvert.SerializeObject(x)
                    }).ToArray();
                    kv.Value.transitions = kv.Key.transitions.Select(x => new Edge()
                    {
                        destination = x.destination.name,
                        conditions = x.conditions
                    }).ToArray();
                });
                dim = stateMachine.dim;
                states = stateNodeMap.Values.ToArray();
            }
        }
        public Layer[] layers;
        public ChatStateMachine[] Deserialize()
        {
            return layers.Select(x => x.Deserialize()).ToArray();
        }
        public void Serialize(ChatStateMachine[] stateMachines)
        {
            layers = new Layer[stateMachines.Length];
            for (int i = 0; i < layers.Length; ++i)
            {
                layers[i] = new();
                layers[i].Serialize(stateMachines[i]);
            }
        }
    }
}
