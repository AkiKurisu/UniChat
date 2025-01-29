using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Pool;
namespace UniChat.LLMs
{
    public class OpenAIModels
    {
        public const string ChatGPT3 = "gpt-3.5-turbo";
        
        public const string ChatGPT4 = "gpt-4";
    }
    
    public class OpenAIClient : IChatModel
    {
        public const string DefaultAPI = "https://api.openai-proxy.com/v1/chat/completions";
        
        public string ChatAPI { get; set; } = DefaultAPI;
        
        public string GptModel { get; set; } = OpenAIModels.ChatGPT3;
        
        public string ApiKey { get; set; }
        
        public List<string> StopWords { get; set; } = new();
        
        public bool Verbose { get; set; } = false;
        
        public float Temperature { get; set; } = 0.7f;
        
        public float Top_p { get; set; } = 1f;
        
        public OpenAIClient(string url, string model, string apiKey)
        {
            ApiKey = apiKey;
            GptModel = string.IsNullOrEmpty(model) ? OpenAIModels.ChatGPT3 : model;
            ChatAPI = string.IsNullOrEmpty(url) ? DefaultAPI : url;
        }
        
        public async UniTask<ILLMResponse> GenerateAsync(IChatRequest input, CancellationToken ct)
        {
            var list = ListPool<SendData>.Get();
            try
            {
                Format(input, list);
                return await InternalCall(list, ct);
            }
            finally
            {
                ListPool<SendData>.Release(list);
            }
        }
        
        private async UniTask<ILLMResponse> InternalCall(List<SendData> dataList, CancellationToken ct)
        {
            PostData postData = new()
            {
                model = GptModel,
                messages = dataList,
                temperature = Temperature,
                stop = StopWords,
                top_p = Top_p
            };
            string input = JsonConvert.SerializeObject(postData);
            using UnityWebRequest request = new(ChatAPI, "POST");
            if (Verbose) Debug.Log($"Request {input}");
            byte[] data = System.Text.Encoding.UTF8.GetBytes(input);
            request.uploadHandler = new UploadHandlerRaw(data);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", string.Format("Bearer {0}", ApiKey));
            await request.SendWebRequest().ToUniTask(cancellationToken: ct);
            
            string msg = request.downloadHandler.text;
            if (Verbose) Debug.Log($"Response {msg}");
            MessageBack messageBack = JsonConvert.DeserializeObject<MessageBack>(msg);
            return new LLMResponse(messageBack.choices[0].message.content);
        }
        
        private void Format(IChatRequest input, List<SendData> dataList)
        {
            dataList.Clear();
            foreach (var param in input.Messages)
            {
                if (param.Role == MessageRole.System) continue;
                string content = param.Content;
                var sendData = new SendData(GetOpenAIRole(param.Role), content);
                dataList.Add(sendData);
            }
            dataList.Insert(0, new SendData("system", input.Context));
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
            var list = ListPool<SendData>.Get();
            try
            {
                list.Add(new SendData("user", inputPrompt));
                var response = await InternalCall(list, ct);
                return response;
            }
            finally
            {
                ListPool<SendData>.Release(list);
            }
        }
    }
}

