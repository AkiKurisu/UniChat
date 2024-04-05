using System.Threading;
using Cysharp.Threading.Tasks;
using Kurisu.NGDS;
using UnityEngine;
using UnityEngine.Networking;
namespace Kurisu.UniChat
{
    public class VITSClient
    {
        private const string APIBase = "http://{0}:{1}/voice/{2}?text={3}&id={4}&lang={5}";
        private readonly string address;
        private readonly string port;
        public ITranslator Translator { get; set; }
        private readonly string api;
        private readonly string lang;
        public VITSClient(string address = "127.0.0.1", string api = "vits", string port = "23456", string lang = "auto")
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
        public async UniTask<bool> TryRequest()
        {
            using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(GetURL("你好", 0), AudioType.WAV);
            try
            {
                await www.SendWebRequest().ToUniTask(default);
            }
            catch (UnityWebRequestException)
            {
                return false;
            }
            return true;
        }
        public async UniTask<AudioClip> SendRequestAsync(string message, int characterID, CancellationToken ct)
        {
            if (Translator != null)
            {
                message = await Translator.Translate(message, ct);
            }
            using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(GetURL(message, characterID), AudioType.WAV);
            try
            {
                await www.SendWebRequest().ToUniTask(cancellationToken: ct);
            }
            catch (UnityWebRequestException)
            {
                Debug.LogError($"[VITS] {www.error}");
                return default;
            }
            AudioClip audioClip = null;
            try
            {
                audioClip = DownloadHandlerAudioClip.GetContent(www);
                return audioClip;
            }
            catch
            {
                return default;
            }
        }
    }
}