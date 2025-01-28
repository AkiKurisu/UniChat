using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
namespace UniChat.LLMs
{
    /// <summary>
    /// Use ChatGLM with Normal API to generate text
    /// See https://github.com/THUDM/ChatGLM2-6B/blob/main/api.py
    /// </summary>
    public class ChatGLMClient : IChatModel
    {
        public bool Verbose { get; set; }
        private readonly string uri;
        public GLMGenParams GenParams { get; set; } = new();
        public ChatGLMClient(string address = "127.0.0.1", string port = "8000")
        {
            uri = $"http://{address}:{port}/";
        }
        public async UniTask<ILLMResponse> GenerateAsync(IChatRequest input, CancellationToken ct)
        {
            return await InternalCall(input.Formatter.Format(input), ct);
        }
        public async UniTask<ILLMResponse> GenerateAsync(string input, CancellationToken ct)
        {
            return await InternalCall(input, ct);
        }
        private async UniTask<ILLMResponse> InternalCall(string message, CancellationToken ct)
        {
            GenParams.Prompt = message;
            var input = JsonConvert.SerializeObject(GenParams);
            if (Verbose) Debug.Log($"Request {input}");
            using UnityWebRequest request = new(uri, "POST")
            {
                uploadHandler = new UploadHandlerRaw(new UTF8Encoding().GetBytes(input)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json");
            await request.SendWebRequest().ToUniTask(cancellationToken: ct);
            string response = string.Empty;

            var messageBack = JsonConvert.DeserializeObject<GLMMessageBack>(request.downloadHandler.text);
            response = messageBack.Response;
            GenParams.History = messageBack.History;
            if (Verbose) Debug.Log($"Response {response}");
            return new LLMResponse(response);
        }
    }
}
