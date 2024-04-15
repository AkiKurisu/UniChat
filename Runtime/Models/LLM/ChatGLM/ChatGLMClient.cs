using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
namespace Kurisu.UniChat.LLMs
{
    /// <summary>
    /// Use ChatGLM with Normal API to generate text
    /// See https://github.com/THUDM/ChatGLM2-6B/blob/main/api.py
    /// </summary>
    public class ChatGLMClient : ILargeLanguageModel
    {
        private struct GLMResponse : ILLMResponse
        {
            public bool Status { get; internal set; }

            public string Response { get; internal set; }
        }
        public bool Verbose { get; set; }
        public string Uri { get; set; }
        public string SystemPrompt { get; set; }
        public GLMGenParams GenParams { get; set; } = new();
        public MessageFormatter Formatter { get; set; } = new();
        public ChatGLMClient(string address = "127.0.0.1", string port = "8000")
        {
            Uri = $"http://{address}:{port}/";
        }
        public async UniTask<ILLMResponse> GenerateAsync(ILLMRequest input, CancellationToken ct)
        {
            GenParams.Prompt = Formatter.Format(input);
            return await InternalCall(JsonConvert.SerializeObject(GenParams), ct);
        }
        public async UniTask<ILLMResponse> GenerateAsync(string input, CancellationToken ct)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(SystemPrompt)) sb.AppendLine(SystemPrompt);
            sb.AppendLine(input);
            GenParams.Prompt = sb.ToString();
            return await InternalCall(JsonConvert.SerializeObject(GenParams), ct);
        }
        private async UniTask<ILLMResponse> InternalCall(string input, CancellationToken ct)
        {
            if (Verbose) Debug.Log($"Request {input}");
            UnityWebRequest request = new(Uri, "POST")
            {
                uploadHandler =
                     new UploadHandlerRaw(new UTF8Encoding().GetBytes(input)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json");
            try
            {
                await request.SendWebRequest().ToUniTask(cancellationToken: ct);
            }
            catch
            {
                Debug.LogError(request.error);
                return new GLMResponse()
                {
                    Status = false,
                    Response = string.Empty
                };
            }
            string response = string.Empty;
            bool validate;
            try
            {
                var messageBack = JsonConvert.DeserializeObject<GLMMessageBack>(request.downloadHandler.text);
                response = messageBack.Response;
                GenParams.History = messageBack.History;
                validate = true;
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                validate = false;
            }
            if (Verbose) Debug.Log($"Response {response}");
            return new GLMResponse()
            {
                Response = response,
                Status = validate
            };
        }
    }
}
