using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using System.Linq;
using UnityEngine.Networking;
using UnityEngine.Pool;
namespace UniChat.LLMs
{
    /// <summary>
    /// Model list see https://ollama.com/library
    /// </summary>
    public class OllamaModels
    {
        public const string Qwen = "qwen";
        
        public const string Llama2 = "llama2";
        
        public const string Llama2_Uncensored = "llama2-uncensored";
        
        public const string Llama3 = "llama3";
        
        public const string DeepSeek_R1_8B = "deepseek-r1:8b";
        
        public const string DeepSeek_R1_14B = "deepseek-r1:14b";
    }
    
    public abstract class OllamaClient : IChatModel
    {
        public string Model { get; set; } = OllamaModels.Llama3;
        
        public OllamaOptions Options { get; } = new();
        
        public bool UseJson { get; set; }
        
        public bool Verbose { get; set; }
        
        protected readonly string Uri;
        
        protected OllamaClient(string address = "127.0.0.1", string port = "11434")
        {
            Uri = $"http://{address}:{port}";
        }

        public async UniTask PullModel(string name)
        {
            var content = JsonConvert.SerializeObject(new Dictionary<string, object> { { "name", name }, { "stream", false } });
            using UnityWebRequest request = new($"{Uri}/api/pull", "POST");
            request.uploadHandler = new UploadHandlerRaw(new UTF8Encoding().GetBytes(content));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            await request.SendWebRequest().ToUniTask();
        }

        public async UniTask<OllamaListModelsResponse.Model[]> ListLocalModels()
        {
            using UnityWebRequest request = new($"{Uri}/api/tags", "GET");
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            await request.SendWebRequest().ToUniTask();
            return JsonConvert.DeserializeObject<OllamaListModelsResponse>(request.downloadHandler.text)?.Models ?? throw new InvalidOperationException("Response body was null");
        }

        public abstract UniTask<ILLMResponse> GenerateAsync(IChatRequest input, CancellationToken ct = default);
        
        public abstract UniTask<ILLMResponse> GenerateAsync(string prompt, CancellationToken ct = default);
    }
    
    public class OllamaCompletion : OllamaClient
    {
        private readonly JsonSerializerSettings _serializerSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore
        };
        
        public OllamaCompletion(string address = "127.0.0.1", string port = "11434") : base(address, port)
        {

        }
        
        private async UniTask<ILLMResponse> InternalCall(OllamaCompletionRequest generateRequest, CancellationToken ct)
        {
            generateRequest = generateRequest ?? throw new ArgumentNullException(nameof(generateRequest));
            var models = await ListLocalModels();
            if (!models.Any(x => x.Name == Model || x.Name == $"{Model}:latest"))
            {
                if (Verbose) Debug.Log($"Pull {Model}...");
                await PullModel(Model);
            }
            var content = JsonConvert.SerializeObject(generateRequest, _serializerSettings);

            if (Verbose) Debug.Log($"Request {content}");
            using UnityWebRequest request = new($"{Uri}/api/generate", "POST");
            request.uploadHandler = new UploadHandlerRaw(new UTF8Encoding().GetBytes(content));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            await request.SendWebRequest().ToUniTask(cancellationToken: ct);
            if (Verbose) Debug.Log($"Response {request.downloadHandler.text}");
            var streamedResponse = JsonConvert.DeserializeObject<OllamaCompletionResponse>(request.downloadHandler.text);
            return new LLMResponse(streamedResponse.Response);
        }

        public override async UniTask<ILLMResponse> GenerateAsync(IChatRequest input, CancellationToken ct = default)
        {
            return await GenerateAsync(string.Join("\n", input.Context, ToPrompt(input.Messages)), ct);
        }

        public override async UniTask<ILLMResponse> GenerateAsync(string prompt, CancellationToken ct = default)
        {
            return await InternalCall(new OllamaCompletionRequest()
            {
                Prompt = prompt,
                Model = Model,
                Options = Options,
                Stream = false,
                Raw = true,
                Format = UseJson ? "json" : string.Empty,
            }, ct);
        }
        
        private static string ConvertRole(MessageRole role)
        {
            return role switch
            {
                MessageRole.User => "Human: ",
                MessageRole.Bot => "Assistant: ",
                MessageRole.System => "",
                _ => throw new NotSupportedException($"the role {role} is not supported")
            };
        }

        private static string ConvertMessage(IMessage message)
        {
            return $"{ConvertRole(message.Role)}{message.Content}";
        }

        private static string ToPrompt(IEnumerable<IMessage> messages)
        {
            return string.Join("\n", messages.Select(ConvertMessage).ToArray());
        }
    }
    
    public class OllamaChat : OllamaClient
    {
        private readonly JsonSerializerSettings _serializerSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore
        };
        
        public OllamaChat(string address = "127.0.0.1", string port = "11434") : base(address, port)
        {

        }
        
        public override async UniTask<ILLMResponse> GenerateAsync(IChatRequest input, CancellationToken ct = default)
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
            var models = await ListLocalModels();
            if (models.All(x => x.Name != Model && x.GetModelName() != Model))
            {
                if (Verbose)
                {
                    var modelNames = models.Select(x => x.Name).ToArray();
                    Debug.Log($"[Ollama] Request {Model} not exist, current models: {string.Join(", ", modelNames)}");
                    Debug.Log($"[Ollama] Pull {Model}...");
                }
                await PullModel(Model);
            }
            OllamaChatRequest postData = new()
            {
                model = Model,
                messages = dataList,
                temperature = Options.Temperature,
                stop = Options.Stop,
                top_p = Options.TopP,
                stream = false
            };
            string input = JsonConvert.SerializeObject(postData, _serializerSettings);
            using UnityWebRequest request = new($"{Uri}/api/chat", "POST");
            if (Verbose) Debug.Log($"Request {input}");
            byte[] data = Encoding.UTF8.GetBytes(input);
            request.uploadHandler = new UploadHandlerRaw(data);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            await request.SendWebRequest().ToUniTask(cancellationToken: ct);
            string msg = request.downloadHandler.text;
            if (Verbose) Debug.Log($"Response {msg}");
            OllamaChatResponse response = JsonConvert.DeserializeObject<OllamaChatResponse>(msg);
            return new LLMResponse(response.message.content);
        }
        
        private static void Format(IChatRequest input, List<SendData> dataList)
        {
            dataList.Clear();
            dataList.AddRange(from param in input.Messages where param.Role != MessageRole.System let content = param.Content select new SendData(OpenAIClient.GetOpenAIRole(param.Role), content));
            dataList.Insert(0, new SendData("system", input.Context));
        }
        
        public override async UniTask<ILLMResponse> GenerateAsync(string inputPrompt, CancellationToken ct = default)
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
