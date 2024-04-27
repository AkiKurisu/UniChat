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
namespace Kurisu.UniChat.LLMs
{
    /// <summary>
    /// Model list see https://ollama.com/library
    /// </summary>
    public class OllamaModel
    {
        public const string Qwen = "qwen";
        public const string Llama2 = "llama2";
        public const string Llama2_Uncensored = "llama2-uncensored";
        public const string Llama3 = "llama3";
    }
    public abstract class OllamaClient : ILargeLanguageModel
    {
        protected struct OllamaResponse : ILLMResponse
        {
            public string Response { get; internal set; }
        }
        public string Model { get; set; } = OllamaModel.Llama3;
        public OllamaOptions Options { get; } = new();
        public bool UseJson { get; set; }
        public bool Verbose { get; set; }
        public string Uri { get; set; }
        protected OllamaClient(string address = "127.0.0.1", string port = "11434")
        {
            Uri = $"http://{address}:{port}/";
        }

        public async UniTask PullModel(string name)
        {
            var content = JsonConvert.SerializeObject(new Dictionary<string, object>() { { "name", name }, { "stream", false } });
            using UnityWebRequest request = new($"{Uri}api/pull", "POST")
            {
                uploadHandler = new UploadHandlerRaw(new UTF8Encoding().GetBytes(content)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json");
            await request.SendWebRequest().ToUniTask();
        }

        public async UniTask<IEnumerable<Model>> ListLocalModels()
        {
            using UnityWebRequest request = new($"{Uri}api/tags", "GET")
            {
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json");
            await request.SendWebRequest().ToUniTask();
            return JsonConvert.DeserializeObject<ListModelsResponse>(request.downloadHandler.text)?.Models ?? throw new InvalidOperationException("Response body was null");
        }

        public abstract UniTask<ILLMResponse> GenerateAsync(ILLMRequest input, CancellationToken ct = default);
        public abstract UniTask<ILLMResponse> GenerateAsync(string prompt, CancellationToken ct = default);
    }
    public class OllamaCompletion : OllamaClient
    {
        private readonly JsonSerializerSettings serializerSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore
        };
        public OllamaCompletion(string address = "127.0.0.1", string port = "11434") : base(address, port)
        {

        }
        private async UniTask<ILLMResponse> InternalCall(OllamaCompletionRequest generateRequest, CancellationToken ct)
        {
            generateRequest = generateRequest ?? throw new ArgumentNullException(nameof(generateRequest));

            var content = JsonConvert.SerializeObject(generateRequest, serializerSettings);

            if (Verbose) Debug.Log($"Request {content}");
            using UnityWebRequest request = new($"{Uri}api/generate", "POST")
            {
                uploadHandler = new UploadHandlerRaw(new UTF8Encoding().GetBytes(content)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json");
            await request.SendWebRequest().ToUniTask(cancellationToken: ct);
            if (Verbose) Debug.Log($"Response {request.downloadHandler.text}");
            var streamedResponse = JsonConvert.DeserializeObject<OllamaCompletionResponse>(request.downloadHandler.text);
            return new OllamaResponse()
            {
                Response = streamedResponse.Response
            };
        }

        public override async UniTask<ILLMResponse> GenerateAsync(ILLMRequest input, CancellationToken ct = default)
        {
            return await GenerateAsync(string.Join("\n", input.Context, ToPrompt(input.Messages)));
        }

        public override async UniTask<ILLMResponse> GenerateAsync(string prompt, CancellationToken ct = default)
        {
            var models = await ListLocalModels();
            if (!models.Any(x => x.Name == Model || x.Name == $"{Model}:latest"))
            {
                if (Verbose) Debug.Log($"Pull {Model}...");
                await PullModel(Model);
            }
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
        public List<string> StopWords { get; set; } = new();
        public float Temperature { get; set; } = 0.7f;
        public float Top_p { get; set; } = 1f;
        public string SystemPrompt { get; set; } = "You are a helpful assistant. You can help me by answering my questions. You can also ask me questions.";
        public OllamaChat(string address = "127.0.0.1", string port = "11434") : base(address, port)
        {

        }
        public override async UniTask<ILLMResponse> GenerateAsync(ILLMRequest input, CancellationToken ct)
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
        private async UniTask<ILLMResponse> InternalCall(List<SendData> m_DataList, CancellationToken ct)
        {
            OllamaChatRequest _postData = new()
            {
                model = Model,
                messages = m_DataList,
                temperature = Temperature,
                stop = StopWords,
                top_p = Top_p,
                stream = false
            };
            string input = JsonConvert.SerializeObject(_postData);
            using UnityWebRequest request = new($"{Uri}api/chat", "POST");
            if (Verbose) Debug.Log($"Request {input}");
            byte[] data = Encoding.UTF8.GetBytes(input);
            request.uploadHandler = new UploadHandlerRaw(data);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            await request.SendWebRequest().ToUniTask(cancellationToken: ct);
            string _msg = request.downloadHandler.text;
            if (Verbose) Debug.Log($"Response {_msg}");
            OllamaChatResponse response = JsonConvert.DeserializeObject<OllamaChatResponse>(_msg);
            return new OllamaResponse()
            {
                Response = response.message.content
            };
        }
        private void Format(ILLMRequest input, List<SendData> m_DataList)
        {
            m_DataList.Clear();
            foreach (var param in input.Messages)
            {
                if (param.Role == MessageRole.System) continue;
                string content = param.Content;
                var sendData = new SendData(OpenAIClient.GetOpenAIRole(param.Role), content);
                m_DataList.Add(sendData);
            }
            m_DataList.Insert(0, new SendData("system", string.IsNullOrEmpty(input.Context) ? SystemPrompt : input.Context));
        }
        public override async UniTask<ILLMResponse> GenerateAsync(string inputPrompt, CancellationToken ct)
        {
            var list = ListPool<SendData>.Get();
            try
            {
                list.Add(new SendData("system", SystemPrompt));
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
