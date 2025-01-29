using System.Threading;
using Cysharp.Threading.Tasks;
namespace UniChat
{
    public interface IChatModel : ILargeLanguageModel
    {
        /// <summary>
        /// Generate llm data from chat request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        UniTask<ILLMResponse> GenerateAsync(IChatRequest request, CancellationToken ct);
    }
}