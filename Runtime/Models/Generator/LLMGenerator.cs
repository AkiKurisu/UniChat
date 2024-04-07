using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Kurisu.NGDS;
namespace Kurisu.UniChat
{
    /// <summary>
    /// Use llm to generate contents
    /// </summary>
    public class LLMGenerator : IGenerator
    {
        private readonly ILLMDriver driver;
        private readonly ChatHistoryContext chatHistoryContext;
        public async Task<ILLMOutput> Generate(CancellationToken ct)
        {
            var response = await driver.ProcessLLM(chatHistoryContext, ct);
            return response;
        }
        public LLMGenerator(ILLMDriver driver, ChatHistoryContext chatHistoryContext) : base()
        {
            this.chatHistoryContext = chatHistoryContext;
            this.driver = driver;
        }
        public async UniTask<bool> Generate(GenerateContext context, CancellationToken ct)
        {
            driver.SetSystemPrompt(chatHistoryContext.Context);
            var llmData = await Generate(ct);
            context.generatedContent = llmData.Response;
            return llmData.Status;
        }
    }
}