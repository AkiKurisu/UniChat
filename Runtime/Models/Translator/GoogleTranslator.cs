using UnityEngine;
using System;
using System.Threading;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;
using System.Text;
using Cysharp.Threading.Tasks;
namespace Kurisu.UniChat.Translators
{
    public class GoogleTranslator : ITranslator
    {
        public string SourceLanguageCode { get; set; }
        public string TargetLanguageCode { get; set; }
        public GoogleTranslator() { }
        public GoogleTranslator(string sourceLanguageCode, string targetLanguageCode)
        {
            SourceLanguageCode = sourceLanguageCode;
            TargetLanguageCode = targetLanguageCode;
        }
        public async UniTask<string> TranslateAsync(string input, CancellationToken ct)
        {
            if (SourceLanguageCode == TargetLanguageCode) return input;
            return await GoogleTranslateHelper.TranslateTextAsync(SourceLanguageCode, TargetLanguageCode, input, ct);
        }
    }
    public class GoogleTranslateHelper
    {
        private const string DefaultSL = "auto";
        public static async UniTask<string> TranslateTextAsync(string sourceLanguage, string targetLanguage, string input, CancellationToken ct)
        {
            StringBuilder stringBuilder = new();
            string url;
            if (string.IsNullOrEmpty(sourceLanguage)) sourceLanguage = DefaultSL;
            url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={sourceLanguage}&tl={targetLanguage}&dt=t&q={UnityWebRequest.EscapeURL(input)}";
            var request = UnityWebRequest.Get(url);
            await request.SendWebRequest().ToUniTask(cancellationToken: ct);
            JToken parsedTexts = JToken.Parse(request.downloadHandler.text);
            if (parsedTexts != null && parsedTexts[0] != null)
            {
                var jsonArray = parsedTexts[0].AsJEnumerable();

                if (jsonArray != null)
                {
                    foreach (JToken innerArray in jsonArray)
                    {
                        JToken text = innerArray[0];

                        if (text != null)
                        {
                            stringBuilder.Append(text);
                            stringBuilder.Append(' ');
                        }
                    }
                }
            }
            return stringBuilder.ToString().Trim();
        }
    }
}
