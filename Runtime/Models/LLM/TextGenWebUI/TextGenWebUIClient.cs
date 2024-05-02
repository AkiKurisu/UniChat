using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
namespace Kurisu.UniChat.LLMs
{
    /// <summary>
    /// TODO: It seems that text-generation-webui completely using openAI api now
    /// </summary>
    public class TextGenWebUIClient : IChatModel
    {
        public bool Verbose { get; set; }
        public TextGenWebUIGenerateParams GenParams { get; set; } = new();
        public string Uri { get; set; }
        /// <summary>
        /// Set to automatically set stop sequence and replace keys using formatter's role prefix
        /// </summary>
        /// <value></value>
        public bool SetParamsFromFormatter { get; set; } = true;
        public TextGenWebUIClient(string address = "127.0.0.1", string port = "5000")
        {
            Uri = $"http://{address}:{port}";
        }
        public async UniTask<ILLMResponse> GenerateAsync(IChatRequest request, CancellationToken ct)
        {
            if (SetParamsFromFormatter)
            {
                GenParams.StopStrings.Clear();
                GenParams.StopStrings.Add($"\n{request.Formatter.UserPrefix} ");
                GenParams.StopStrings.Add($"{request.Formatter.UserPrefix}:");
                GenParams.StopStrings.Add($"{request.Formatter.UserPrefix}：");
                GenParams.ReplaceKey.Clear();
                GenParams.ReplaceKey.Add($"{request.Formatter.BotPrefix}:");
                GenParams.ReplaceKey.Add($"{request.Formatter.BotPrefix}：");
            }
            var sb = new StringBuilder();
            sb.Append(request.Formatter.Format(request));
            sb.Append($"{request.Formatter.BotPrefix}:");
            return await InternalCall(sb.ToString(), ct);
        }
        private async UniTask<ILLMResponse> InternalCall(string message, CancellationToken ct)
        {
            GenParams.Prompt = message;
            string input = GenParams.ToJson();
            if (Verbose) Debug.Log($"Request {input}");
            using UnityWebRequest request = new($"{Uri}/api/v1/generate", "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(input));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            await request.SendWebRequest().ToUniTask(cancellationToken: ct);
            if (Verbose) Debug.Log($"Response {request.downloadHandler.text}");
            var result = JsonConvert.DeserializeObject<TextGenWebUICompletionResponse>(request.downloadHandler.text.Trim());
            return new LLMResponse(FormatResponse(result.Results[0].Text));
        }
        public async UniTask<ILLMResponse> GenerateAsync(string input, CancellationToken ct)
        {
            return await InternalCall(input, ct);
        }
        private string FormatResponse(string response)
        {
            if (string.IsNullOrEmpty(response)) return string.Empty;
            response = LineBreakFormatter.Format(response);
            foreach (var keyword in GenParams.ReplaceKey)
            {
                response = response.Replace(keyword, string.Empty);
            }
            foreach (var keyword in GenParams.StopStrings)
            {
                response = response.Replace(keyword, string.Empty);
            }
            return response.Trim();
        }
    }
}
