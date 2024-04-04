using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Kurisu.NGDS;
namespace Kurisu.UniChat
{
    /// <summary>
    /// Use llm to generate contents
    /// </summary>
    public class LLMGenerator : ChatGeneratorBase
    {
        private readonly ILLMDriver driver;
        public async Task<ILLMOutput> Generate(CancellationToken ct)
        {
            var response = await driver.ProcessLLM(this, ct);
            return response;
        }
        public LLMGenerator(ILLMDriver driver) : base()
        {
            this.driver = driver;
        }
        public sealed override async UniTask<bool> Generate(GenerateContext context, CancellationToken ct)
        {
            driver.SetSystemPrompt(Context);
            var llmData = await Generate(ct);
            context.generatedContent = llmData.Response;
            return llmData.Status;
        }
    }
}