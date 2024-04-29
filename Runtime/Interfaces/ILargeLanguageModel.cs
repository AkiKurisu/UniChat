using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
namespace Kurisu.UniChat
{
    public interface ILLMResponse
    {
        string Response { get; }
    }
    public readonly struct LLMResponse : ILLMResponse
    {
        public string Response { get; }
        public LLMResponse(string response)
        {
            Response = response;
        }
    }
    public enum MessageRole
    {
        System,
        Bot,
        User
    }
    public interface IMessage
    {
        public MessageRole Role { get; }
        public string Content { get; }
    }
    public interface IChatRequest
    {
        /// <summary>
        /// The system context of request
        /// </summary>
        /// <value></value>
        string Context { get; }
        /// <summary>
        /// The messages of request
        /// </summary>
        /// <value></value>
        IEnumerable<IMessage> Messages { get; }
    }
    public interface ITranslator
    {
        UniTask<string> TranslateAsync(string input, CancellationToken ct);
    }
    public interface ILargeLanguageModel
    {
        /// <summary>
        /// Log request and response content
        /// </summary>
        /// <value></value>
        bool Verbose { get; set; }
        /// <summary>
        /// Generate llm data from string input
        /// </summary>
        /// <param name="input"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        UniTask<ILLMResponse> GenerateAsync(string input, CancellationToken ct);
    }
}