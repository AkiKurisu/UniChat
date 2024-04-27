using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
namespace Kurisu.UniChat
{
    public interface ILLMResponse
    {
        string Response { get; }
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
    public interface ILLMRequest
    {
        /// <summary>
        /// The character of request
        /// </summary>
        /// <value></value>
        string BotName { get; }
        /// <summary>
        /// The characters of request
        /// </summary>
        /// <value></value>
        string UserName { get; }
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
        /// Generate llm data from llm input
        /// </summary>
        /// <param name="input"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        UniTask<ILLMResponse> GenerateAsync(ILLMRequest input, CancellationToken ct);
        /// <summary>
        /// Generate llm data from input
        /// </summary>
        /// <param name="input"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        UniTask<ILLMResponse> GenerateAsync(string input, CancellationToken ct);
    }
}