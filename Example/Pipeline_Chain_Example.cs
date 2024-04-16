using Cysharp.Threading.Tasks;
using Kurisu.UniChat.Chains;
using Kurisu.UniChat.LLMs;
using UnityEngine;
namespace Kurisu.UniChat.Example
{
    public class Pipeline_Chain_Example : MonoBehaviour
    {
        public LLMSettingsAsset settingsAsset;
        public AudioSource audioSource;
        private ChatPipelineCtrl pipelineCtrl;
        public async void Start()
        {
            //Create new chat model file with empty memory and embedding db
            var chatModelFile = new ChatModelFile() { fileName = "NewChatFile", modelProvider = ModelProvider.AddressableProvider };

            //Create an pipeline ctrl to run it
            pipelineCtrl = new ChatPipelineCtrl(chatModelFile, settingsAsset);
            pipelineCtrl.SwitchGenerator(ChatGeneratorIds.ChatGPT, true);

            //Init pipeline, set verbose to log status
            await pipelineCtrl.InitializePipeline(new PipelineConfig { verbose = true });
            pipelineCtrl.Memory.Context = "你是我的私人助理，你会解答我的各种问题";

            //Different api version
            await DoChain1();
            await DoChain2();
            await NoChain();
        }
        //1. Chain where input keys are inherited
        public async UniTask DoChain1()
        {
            //Create chain
            var chain = pipelineCtrl.ToChain(inputKey: "input")
                        .Input("怎么学习Unity?")
                        .CastStringValue(outputKey: "text")
                        .UpdateHistory();
            //Run chain
            Debug.Log(await chain.Run("text"));
        }
        //2. Normal chain
        public async UniTask DoChain2()
        {
            //Create chain
            var chain = Chain.Set("怎么学习Unreal?", "input") |
                        pipelineCtrl.ToChain(inputKey: "input", outputKey: "context") |
                        PipelineChain.CastStringValue(inputKey: "context", outputKey: "text") |
                        PipelineChain.UpdateHistory(pipelineCtrl.History, "input", "context");
            //Run chain
            Debug.Log(await chain.Run("text"));
        }
        //3. Without chain
        public async UniTask NoChain()
        {
            string input = "怎么学习Godot?";
            var context = await pipelineCtrl.RunPipeline(input);
            string text = context.CastStringValue();
            pipelineCtrl.History.AppendUserMessage(input);
            pipelineCtrl.History.AppendBotMessage(text);
            Debug.Log(text);
        }
    }
}
