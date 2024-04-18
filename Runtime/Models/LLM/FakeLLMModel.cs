using System.Threading;
using Cysharp.Threading.Tasks;
namespace Kurisu.UniChat.LLMs
{
    /// <summary>
    /// A fake llm model for debug purpose
    /// </summary>
    public class FakeLLMModel : ILargeLanguageModel
    {
        private struct FakeResponse : ILLMResponse
        {
            public readonly bool Status => true;
            public string Response { get; internal set; }
        }
        public FakeLLMModel() { }
        public FakeLLMModel(string response) 
        { 
            Response = response; 
        }
        public string Response { get; set; }
        public bool Verbose { get ; set ; }
        public async UniTask<ILLMResponse> GenerateAsync(ILLMRequest input, CancellationToken ct)
        {
            return await InternalCall(ct);
        }
        private UniTask<ILLMResponse> InternalCall(CancellationToken _)
        {
            return UniTask.FromResult<ILLMResponse>(new FakeResponse()
            {
                Response = Response
            });
        }
        public async UniTask<ILLMResponse> GenerateAsync(string inputPrompt, CancellationToken ct)
        {
            var response = await InternalCall(ct);
            return response;
        }
    }
}

