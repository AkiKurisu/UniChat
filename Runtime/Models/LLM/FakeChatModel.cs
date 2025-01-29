using System.Threading;
using Cysharp.Threading.Tasks;
namespace UniChat.LLMs
{
    /// <summary>
    /// A fake chat model for debug purpose
    /// </summary>
    public class FakeChatModel : IChatModel
    {
        public FakeChatModel() { }
        
        public FakeChatModel(string response)
        {
            Response = response;
        }
        
        public string Response { get; set; }
        
        public bool Verbose { get; set; }
        
        public string SystemPrompt { get; set; }
        
        public async UniTask<ILLMResponse> GenerateAsync(IChatRequest input, CancellationToken ct)
        {
            return await InternalCall(ct);
        }
        
        private UniTask<ILLMResponse> InternalCall(CancellationToken _)
        {
            return UniTask.FromResult<ILLMResponse>(new LLMResponse(Response));
        }
        
        public async UniTask<ILLMResponse> GenerateAsync(string inputPrompt, CancellationToken ct)
        {
            var response = await InternalCall(ct);
            return response;
        }
    }
}

