using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
namespace Kurisu.UniChat.LLMs
{
    public class OpenAIClient : ILargeLanguageModel
    {
        private struct GPTResponse : ILLMResponse
        {
            public bool Status { get; internal set; }
            public string Response { get; internal set; }
        }
        private readonly List<SendData> m_DataList = new();
        private readonly SemaphoreSlim semaphore = new(1, 1);
        public const string DefaultModel = "gpt-3.5-turbo";
        public const string DefaultAPI = "https://api.openai-proxy.com/v1/chat/completions";
        public string ChatAPI { get; set; } = DefaultAPI;
        public string GptModel { get; set; } = DefaultModel;
        public string ApiKey { get; set; }
        public MessageFormatter Formatter { get; set; } = new();
        public bool Verbose { get; set; } = false;
        public float Temperature { get; set; } = 0.5f;
        public float Top_p { get; set; } = 0.5f;
        public string SystemPrompt { get; set; } = "You are a helpful assistant. You can help me by answering my questions. You can also ask me questions.";
        public OpenAIClient(string url, string model, string apiKey)
        {
            ApiKey = apiKey;
            GptModel = model;
            if (string.IsNullOrEmpty(url))
                ChatAPI = DefaultAPI;
            else
                ChatAPI = url;
        }
        public async UniTask<ILLMResponse> GenerateAsync(ILLMRequest input, CancellationToken ct)
        {
            Format(input);
            PostData _postData = new()
            {
                model = GptModel,
                messages = m_DataList
            };
            return await InternalCall(JsonUtility.ToJson(_postData), ct);
        }
        private async UniTask<ILLMResponse> InternalCall(string input, CancellationToken ct)
        {
            await semaphore.WaitAsync();
            try
            {
                using UnityWebRequest request = new(ChatAPI, "POST");
                if (Verbose) Debug.Log($"Request {input}");
                byte[] data = System.Text.Encoding.UTF8.GetBytes(input);
                request.uploadHandler = new UploadHandlerRaw(data);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", string.Format("Bearer {0}", ApiKey));
                try
                {
                    await request.SendWebRequest().ToUniTask(cancellationToken: ct);
                }
                catch
                {
                    Debug.LogError(request.error);
                    return new GPTResponse()
                    {
                        Response = string.Empty,
                        Status = false
                    };
                }
                string _msg = request.downloadHandler.text;
                MessageBack messageBack = JsonUtility.FromJson<MessageBack>(_msg);
                string _backMsg = string.Empty;
                if (messageBack != null && messageBack.choices.Count > 0)
                {
                    _backMsg = messageBack.choices[0].message.content;
                    if (Verbose) Debug.Log($"Response {_backMsg}");
                }
                return new GPTResponse()
                {
                    Response = _backMsg,
                    Status = true
                };
            }
            finally
            {
                semaphore.Release();
            }
        }
        private void Format(ILLMRequest input)
        {
            m_DataList.Clear();
            foreach (var param in input.History)
            {
                if (param.Role == MessageRole.System) continue;
                string content = param.Content;
                var sendData = new SendData(GetOpenAIRole(param.Role), content);
                m_DataList.Add(sendData);
            }
            m_DataList.Insert(0, new SendData("system", string.IsNullOrEmpty(input.Context) ? SystemPrompt : input.Context));
        }
        public static string GetOpenAIRole(MessageRole role)
        {
            return role switch
            {
                MessageRole.User => "user",
                MessageRole.Bot => "assistant",
                MessageRole.System => "system",
                _ => throw new ArgumentOutOfRangeException(nameof(role)),
            };
        }
        public async UniTask<ILLMResponse> GenerateAsync(string inputPrompt, CancellationToken ct)
        {
            m_DataList.Clear();
            m_DataList.Add(new SendData("system", SystemPrompt));
            m_DataList.Add(new SendData("user", inputPrompt));
            PostData _postData = new()
            {
                model = GptModel,
                messages = m_DataList,
                temperature = Temperature,
                top_p = Top_p
            };
            string input = JsonUtility.ToJson(_postData);
            var response = await InternalCall(input, ct);
            if (response.Status)
            {
                m_DataList.Add(new SendData("assistant", response.Response));
            }
            return response;
        }
    }
}

