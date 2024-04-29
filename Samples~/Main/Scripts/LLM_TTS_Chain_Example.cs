using Kurisu.UniChat.Chains;
using Kurisu.UniChat.LLMs;
using Kurisu.UniChat.Translators;
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
            var llm = new LLMFactory(settingsAsset).CreateLLM(LLMType.ChatGPT);
            var translator = new LLMTranslator(llm, "en", "ja");
            //Create chain
            var chain =
                Chain.Set(chatPrompt)
                | Chain.LLM(llm, outputKey: "chatResponse")
                | Chain.Translate(translator, inputKey: "chatResponse")
                | Chain.TTS(new VITSModel(lang: "ja"), outputKey: "audioClip");
            //Run chain
            (string result, AudioClip audioClip) = await chain.Trace(true, true).Run<string, AudioClip>("chatResponse", "audioClip");
            Debug.Log(result);
            audioSource.clip = audioClip;
            audioSource.Play();
        }
    }
}
