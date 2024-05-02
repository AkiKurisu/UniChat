using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using Newtonsoft.Json;
namespace Kurisu.UniChat.LLMs
{
    public class KoboldCppClient : IChatModel
    {
        public KoboldGenParams GenParams { get; set; } = new();
        public string Uri { get; set; }
        public bool Verbose { get; set; }
        /// <summary>
        /// Set to automatically set stop sequence and replace keys using formatter's role prefix
        /// </summary>
        /// <value></value>
        public bool SetParamsFromFormatter { get; set; } = true;
        public KoboldCppClient(string address = "127.0.0.1", string port = "5001")
        {
            Uri = $"http://{address}:{port}";
        }
        public async UniTask<ILLMResponse> GenerateAsync(IChatRequest request, CancellationToken ct)
        {
            if (SetParamsFromFormatter)
            {
                GenParams.StopSequence.Clear();
                GenParams.StopSequence.Add($"{request.Formatter.UserPrefix}:");
                GenParams.StopSequence.Add($"{request.Formatter.UserPrefix}：");
                GenParams.StopSequence.Add($"\n{request.Formatter.UserPrefix} ");
                GenParams.ReplaceKey.Clear();
                GenParams.ReplaceKey.Add($"{request.Formatter.BotPrefix}:");
                GenParams.ReplaceKey.Add($"{request.Formatter.BotPrefix}：");
            }
            return await InternalCall(request.Formatter.Format(request), ct);
        }
        public async UniTask<ILLMResponse> GenerateAsync(string input, CancellationToken ct)
        {
            return await InternalCall(input, ct);
        }
        private async Task<ILLMResponse> InternalCall(string prompt, CancellationToken ct)
        {
            GenParams.Prompt = prompt;

            string input = GenParams.ToJson();
            if (Verbose) Debug.Log($"Request {input}");
            //Use KoboldCpp core api see https://lite.koboldai.net/koboldcpp_api
            using UnityWebRequest request = new($"{Uri}/api/v1/generate", "POST")
            {
                uploadHandler = new UploadHandlerRaw(new UTF8Encoding().GetBytes(input)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json");
            await request.SendWebRequest().ToUniTask(cancellationToken: ct);
            var messageBack = JsonConvert.DeserializeObject<KoboldResponse>(request.downloadHandler.text);
            if (Verbose) Debug.Log($"Response {messageBack}");
            return new LLMResponse(FormatResponse(messageBack.Results[0].Text));
        }
        private string FormatResponse(string response)
        {
            if (string.IsNullOrEmpty(response)) return string.Empty;
            response = LineBreakFormatter.Format(response);
            foreach (var keyword in GenParams.ReplaceKey)
            {
                response = response.Replace(keyword, string.Empty);
            }
            return response.Trim();
        }
    }
}
