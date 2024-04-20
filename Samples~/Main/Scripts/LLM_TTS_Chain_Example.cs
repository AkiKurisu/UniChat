using Kurisu.UniChat.Chains;
using Kurisu.UniChat.LLMs;
using Kurisu.UniChat.TTS;
using UnityEngine;
namespace Kurisu.UniChat.Example
{
    public class LLM_TTS_Chain_Example : MonoBehaviour
    {
        public LLMSettingsAsset settingsAsset;
        public AudioSource audioSource;
        public async void Start()
        {
            var chatPrompt = @"
                You are an AI assistant that greets the world.
                User: Hello !
                Assistant:";
            var translatePrompt = @"
                You are an translator to translate English to Japanese.
                User: {chatResponse}
                Translation:";
            var llm = LLMFactory.Create(LLMType.ChatGPT, settingsAsset);

            //Create chain
            var chain =
                Chain.Set(chatPrompt, outputKey: "prompt")
                | Chain.LLM(llm, inputKey: "prompt", outputKey: "chatResponse")
                | Chain.Template(translatePrompt, outputKey: "prompt")
                | Chain.LLM(llm, inputKey: "prompt", outputKey: "ttsInput")
                | Chain.TTS(new VITSClient(lang: "ja"), inputKey: "ttsInput", outputKey: "audioClip");

            //Run chain
            (string result, AudioClip audioClip) = await chain.Trace(true, true).Run<string, AudioClip>("chatResponse", "audioClip");
            Debug.Log(result);
            audioSource.clip = audioClip;
            audioSource.Play();
        }
    }
}
