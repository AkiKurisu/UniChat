using System.IO;
using Kurisu.UniChat.Chains;
using Kurisu.UniChat.LLMs;
using Kurisu.UniChat.TTS;
using Unity.Sentis;
using UnityEngine;
namespace Kurisu.UniChat.Example
{
    public class Piper_TTS_Chain_Example : MonoBehaviour
    {
        public LLMSettingsAsset settingsAsset;
        public AudioSource audioSource;
        private PiperModel piperModel;
        public async void Start()
        {
            var chatPrompt = @"
                你是我的私人助理.
                User: 你好啊!
                Assistant:";
            var llm = new LLMFactory(settingsAsset).CreateLLM(LLMType.ChatGPT);

            var provider = ModelProviderFactory.Instance.Create(ModelProvider.AddressableProvider);

            //Load pre-trained Chinese piper model downloaded from https://github.com/rhasspy/piper/blob/master/VOICES.md
            piperModel = new(await provider.LoadModel("piper/zh_CN-huayan-medium.sentis"),
                               Path.Combine(Application.streamingAssetsPath, "espeak-ng-data"),
                               BackendType.GPUCompute);

            //Create chain
            var chain =
                Chain.Set(chatPrompt, outputKey: "prompt")
                | Chain.LLM(llm, inputKey: "prompt", outputKey: "chatResponse")
                | Chain.TTS(piperModel, new PiperModel.PiperSettings() { Voice = "cmn" }, inputKey: "chatResponse", outputKey: "audioClip");

            //Run chain
            (string result, AudioClip audioClip) = await chain.Trace(true, true).Run<string, AudioClip>("chatResponse", "audioClip");
            Debug.Log(result);
            audioSource.clip = audioClip;
            audioSource.Play();
        }
        private void OnDestroy()
        {
            piperModel.Dispose();
        }
    }
}
