using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Kurisu.UniChat.Chains;
using Kurisu.UniChat.LLMs;
using Kurisu.UniChat.NLP;
using Kurisu.UniChat.Translators;
using Kurisu.UniChat.TTS;
using UnityEngine;
namespace Kurisu.UniChat.Example
{
    public class Complex_Pipeline_Chain_Example : MonoBehaviour
    {
        public LLMSettingsAsset settingsAsset;
        public AudioSource audioSource;
        public async void Start()
        {
            //Create new chat model file with empty memory and embedding db
            var chatModelFile = new ChatModelFile() { fileName = "NewChatFile", modelProvider = ModelProvider.AddressableProvider };

            //Create an pipeline ctrl to run it
            var pipelineCtrl = new ChatPipelineCtrl(chatModelFile, settingsAsset);
            pipelineCtrl.SwitchGenerator(ChatGeneratorIds.ChatGPT, true);

            //Init pipeline, set verbose to log status
            await pipelineCtrl.InitializePipeline(new PipelineConfig { verbose = true });
            var vitsClient = new VITSClient(lang: "ja");

            //Add some chat messages
            pipelineCtrl.Memory.Context = "你是我的私人助理，你会解答我的各种问题";
            pipelineCtrl.History.AppendUserMessage("你好!");
            pipelineCtrl.History.AppendBotMessage("请问我要如何帮助你呢？");

            //Create cache to cache audioClips and translated texts
            var audioCache = AudioCache.CreateCache(chatModelFile.DirectoryPath);
            var textCache = TextMemoryCache.CreateCache(chatModelFile.DirectoryPath);

            //Create chain
            var chain = pipelineCtrl
                        .ToChain(inputKey: "input")
                        .Input("如何学习Unity?")
                        .CastStringValue(outputKey: "text")
                        .UpdateHistory()
                        .SaveModel()
                        .SaveSession(Path.Combine(PathUtil.SessionPath, "Example_Session.json"))
                        //Translate to japanese
                        | Chain.Translate(new GoogleTranslator("zh", "ja")).UseCache(textCache)
                        //Split them
                        | Chain.Split(new RegexSplitter(@"(?<=[。！？! ?])"), inputKey: "translated_text")
                        //Auto batched
                        | Chain.TTS(vitsClient, inputKey: "splitted_text").UseCache(audioCache).Verbose(true);

            //Run chain
            (IReadOnlyList<string> segments, IReadOnlyList<AudioClip> audioClips)
                = await chain.Trace(true, true).Run<IReadOnlyList<string>, IReadOnlyList<AudioClip>>("splitted_text", "audio");

            //Play audios
            for (int i = 0; i < audioClips.Count; ++i)
            {
                Debug.Log(segments[i]);
                audioSource.clip = audioClips[i];
                audioSource.Play();
                await UniTask.WaitUntil(() => !audioSource.isPlaying);
            }
        }
    }
}
