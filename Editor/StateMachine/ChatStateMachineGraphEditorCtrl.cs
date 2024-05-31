using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Pool;
using static Kurisu.UniChat.StateMachine.ChatStateMachineGraph;
namespace Kurisu.UniChat.StateMachine.Editor
{
    public class ChatStateMachineGraphEditorCtrl : ScriptableObject
    {
        public Layer[] layers;
        private ChatStateMachineGraph graph = new();
        public void Save(string path)
        {
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var bw = new BinaryWriter(stream);
            graph.layers = layers;
            graph.layers.SelectMany(x => x.states).SelectMany(x => x.behaviors)
                        .ForEach(x =>
                        {
                            x.jsonData = JsonConvert.SerializeObject(x.container.Value);
                        });
            bw.Write(JsonConvert.SerializeObject(graph));
        }
        public bool Update()
        {
            bool isDirty = false;
            var list = ListPool<SerializedBehaviorWrapper>.Get();
            foreach (var layer in layers)
            {
                if (layer.states == null) continue;
                foreach (var state in layer.states)
                {
                    if (state.behaviors == null) continue;
                    foreach (var behavior in state.behaviors)
                    {
                        if (!behavior.container) continue;
                        if (list.Contains(behavior.container))
                        {
                            //Fix duplicated instance bug when click add in Editor which will copy last item's serialized value
                            behavior.container = null;
                            isDirty = true;
                        }
                        list.Add(behavior.container);
                    }
                }
            }
            ListPool<SerializedBehaviorWrapper>.Release(list);
            return isDirty;
        }
        public void Load(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(stream);
            string json = br.ReadString();
            graph = JsonConvert.DeserializeObject<ChatStateMachineGraph>(json);
            graph.layers.SelectMany(x => x.states).SelectMany(x => x.behaviors)
                        .ForEach(x =>
                        {
                            x.container = SerializedBehaviorUtils.Wrap(x.Deserialize());
                        });
            layers = graph.layers;
        }

        public void Reset()
        {
            layers = new Layer[1] { new(){
                states = new StateNode[1]{new()
                {
                    name="Start"
                }}
            }};
        }
    }
}