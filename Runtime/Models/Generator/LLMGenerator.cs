using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Kurisu.UniChat.Memory;
namespace Kurisu.UniChat
{
    /// <summary>
    /// Use llm to generate contents
    /// </summary>
    public class LLMGenerator : IGenerator
    {
        private readonly ILargeLanguageModel llm;
        private readonly ChatMemory memory;
        public async Task<ILLMResponse> InternalCall(CancellationToken ct)
        {
            var response = await llm.GenerateAsync(memory, ct);
            return response;
        }
        public LLMGenerator(ILargeLanguageModel llm, ChatMemory memory) : base()
        {
            this.memory = memory;
            this.llm = llm;
        }
        public async UniTask<bool> Generate(GenerateContext context, CancellationToken ct)
        {
            var llmData = await InternalCall(ct);
            context.generatedContent = llmData.Response;
            return llmData.Status;
        }
    }
}