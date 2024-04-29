using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
namespace Kurisu.UniChat.LLMs
{
    public class OobaboogaClient : IChatModel
    {
        public bool Verbose { get; set; }
        public MessageFormatter Formatter { get; set; } = new();
        public OobaboogaGenerateParams GenParams { get; set; } = new();
        public string Uri { get; set; }
        public static string[] replaceKeyWords = new string[]
        {
            "<START>"
        };
        public OobaboogaClient(string address = "127.0.0.1", string port = "5000")
        {
            Uri = $"http://{address}:{port}";
        }
        public void SetStopCharacter(string char_name)
        {
            GenParams.StopStrings = new() { char_name, $"\n{char_name} " };
        }
        public async UniTask<ILLMResponse> GenerateAsync(IChatRequest input, CancellationToken ct)
        {
            var sb = new StringBuilder();
            sb.Append(Formatter.Format(input));
            sb.Append($"{Formatter.BotPrefix}:");
            return await InternalCall(Formatter.Format(input), ct);
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
            var result = JsonConvert.DeserializeObject<OobaboogaCompletionResponse>(request.downloadHandler.text.Trim());
            return new LLMResponse(FormatResponse(result.Results[0].Text));
        }
        public async UniTask<ILLMResponse> GenerateAsync(string input, CancellationToken ct)
        {
            return await InternalCall(input, ct);
        }
        private string FormatResponse(string response)
        {
            if (string.IsNullOrEmpty(response)) return string.Empty;
            response = LineBreakHelper.Format(response);
            foreach (var keyword in replaceKeyWords)
            {
                response = response.Replace(keyword, string.Empty);
            }
            foreach (var stopWord in GenParams.StopStrings)
            {
                response = response.Replace(stopWord, string.Empty);
            }
            return response;
        }
    }
    public class LineBreakHelper
    {
        public static string Format(string input)
        {
            int startIndex = 0;
            int endIndex = 0;
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] != '\n')
                {
                    startIndex = i;
                    break;
                }
            }
            for (int i = input.Length - 1; i >= 0; i--)
            {
                if (input[i] != '\n')
                {
                    endIndex = i;
                    break;
                }
            }
            if (startIndex > endIndex) return string.Empty;
            return input.Substring(startIndex, endIndex - startIndex + 1);
        }
    }
}
