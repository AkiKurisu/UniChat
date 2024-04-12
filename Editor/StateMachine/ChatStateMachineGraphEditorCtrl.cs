using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
namespace Kurisu.UniChat.StateMachine.Editor
{
    public class ChatStateMachineGraphEditorCtrl : ScriptableObject
    {
        public ChatStateMachineGraph.Layer[] layers;
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
            layers = new ChatStateMachineGraph.Layer[1] { new(){
                states = new ChatStateMachineGraph.StateNode[1]{new()
                {
                    name="Start"
                }}
            }};
        }
    }
}