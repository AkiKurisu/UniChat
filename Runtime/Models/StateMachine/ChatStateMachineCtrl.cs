using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Kurisu.UniChat.NLP;
using Newtonsoft.Json;
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
        /// Set stateMachines from graph
        /// </summary>
        /// <param name="graph"></param>
        public void SetGraph(ChatStateMachineGraph graph)
        {
            var sms = graph.Deserialize();
            Array.Resize(ref stateMachines, sms.Length);
            for (int i = 0; i < sms.Length; ++i)
            {
                SetStateMachine(i, sms[i]);
            }
        }
        /// <summary>
        /// Execute all layers
        /// </summary>
        /// <param name="input"></param>
        public async UniTask Execute(string input)
        {
            var pool = ListPool<string>.Get();
            pool.Add(input);
            try
            {
                TensorFloat inputTensor = encoder.Encode_Mean_Pooling(ops, pool, true);
                for (int i = 0; i < stateMachines.Length; ++i)
                {
                    await stateMachines[i].Execute(ops, inputTensor);
                }
            }
            finally
            {
                ListPool<string>.Release(pool);
            }
        }
        /// <summary>
        /// Execute layer's stateMachine
        /// </summary>
        /// <param name="input"></param>
        public async UniTask Execute(int layer, string input)
        {
            var pool = ListPool<string>.Get();
            pool.Add(input);
            try
            {
                await stateMachines[layer].Execute(ops, encoder.Encode_Mean_Pooling(ops, pool, true));
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
        /// <summary>
        /// Save stateMachine to graph bytes
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
            Save(stream);
        }
        public void Save(Stream stream)
        {
            using var bw = new BinaryWriter(stream);
            Save(bw);
        }
        public void Save(BinaryWriter bw)
        {
            var graph = new ChatStateMachineGraph();
            graph.Serialize(stateMachines);
            bw.Write(JsonConvert.SerializeObject(graph));
        }
        /// <summary>
        /// Load stateMachines from graph bytes
        /// </summary>
        /// <param name="path"></param>
        public void Load(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            Load(stream);
        }
        public void Load(Stream stream)
        {
            using var br = new BinaryReader(stream);
            Load(br);
        }
        public void Load(BinaryReader br)
        {
            string json = br.ReadString();
            var graph = JsonConvert.DeserializeObject<ChatStateMachineGraph>(json);
            SetGraph(graph);
        }
        public void Dispose()
        {
            ops.Dispose();
            allocator.Dispose();
        }
    }
}