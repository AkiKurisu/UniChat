using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
namespace Kurisu.UniChat.TTS
{
    /// <summary>
    /// vits-simple-api client
    /// </summary>
    public abstract class VITSClient : ITextToSpeechModel
    {
        public class VITSSettings : TextToSpeechSettings
        {
            public int characterID;
        }
        private const string APIBase = "http://{0}:{1}/voice/{2}?text={3}&id={4}&lang={5}";
        public readonly string address;
        public readonly string port;
        public ITranslator Translator { get; set; }
        public readonly string api;
        public readonly string lang;
        public VITSClient(string address = "127.0.0.1", string port = "23456", string api = "vits", string lang = "auto")
        {
            this.address = address;
            this.api = api;
            this.port = port;
            this.lang = lang;
        }
        private string GetURL(string message, int characterID)
        {
            return string.Format(APIBase, address, port, api, message, characterID, lang);
        }
        public async UniTask<bool> TryRequest(string dummyInput = "你好", int timeoutSeconds = 15)
        {
            using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(GetURL(dummyInput, 0), AudioType.WAV);
            try
            {
                await www.SendWebRequest().ToUniTask().Timeout(new TimeSpan(timeoutSeconds * TimeSpan.TicksPerSecond));
                return true;
            }
            catch
            {
                return false;
            }
        }
        public async UniTask<AudioClip> SendRequestAsync(string message, int characterID, CancellationToken ct)
        {
            if (Translator != null)
            {
                message = await Translator.TranslateAsync(message, ct);
            }
            using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(GetURL(message, characterID), AudioType.WAV);
            await www.SendWebRequest().ToUniTask(cancellationToken: ct);
            return DownloadHandlerAudioClip.GetContent(www);
        }

        public async UniTask<AudioClip> GenerateSpeechAsync(string prompt, TextToSpeechSettings settings = null, CancellationToken cancellationToken = default)
        {
            return await SendRequestAsync(prompt, settings is VITSSettings vitsSetting ? vitsSetting.characterID : 0, cancellationToken);
        }
    }
    public class VITSModel : VITSClient
    {
        public VITSModel(string address = "127.0.0.1", string port = "23456", string lang = "auto") : base(address, port, "vits", lang)
        {
        }
    }
    public class BertVITS2Model : VITSClient
    {
        public BertVITS2Model(string address = "127.0.0.1", string port = "23456", string lang = "auto") : base(address, port, "bert-vits2", lang)
        {
        }
    }
}