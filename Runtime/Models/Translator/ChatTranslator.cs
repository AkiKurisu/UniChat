using System.Threading;
using Cysharp.Threading.Tasks;
namespace Kurisu.UniChat.Translators
{
    public class ChatTranslator : ITranslator
    {
        public string SourceLanguage { get; set; }
        public string TargetLanguage { get; set; }
        private readonly IChatModel chatModel;
        /// <summary>
        /// Create a translator using llm with Chat API.
        /// </summary>
        /// <param name="chatModel">Set chat model to use</param>
        /// <param name="sourceLanguage">Set source language</param>
        /// <param name="targetLanguage">Set target language</param>
        /// <param name="usePromptPreset">Set to use translator system prompt preset</param>
        public ChatTranslator(IChatModel chatModel, string sourceLanguage, string targetLanguage, bool usePromptPreset = true)
        {
            this.chatModel = chatModel;
            SourceLanguage = sourceLanguage;
            TargetLanguage = targetLanguage;
            if (!usePromptPreset) return;
            if (sourceLanguage != null)
                chatModel.SystemPrompt = $"{targetLanguage} and {sourceLanguage} are language codes. You should translate {sourceLanguage} to {targetLanguage}. You should only reply the translation.";
            else
                chatModel.SystemPrompt = $"{targetLanguage} is language code. You should detect my language and translate them to {targetLanguage}. You should only reply the translation.";
        }
        public async UniTask<string> TranslateAsync(string input, CancellationToken ct)
        {
            return (await chatModel.GenerateAsync(input, ct)).Response;
        }
    }
}