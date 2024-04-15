using System.Threading;
using Cysharp.Threading.Tasks;
using Kurisu.UniChat.LLMs;
namespace Kurisu.UniChat.Translators
{
    public class OpenAITranslator : ITranslator
    {
        public string SourceLanguage { get; set; }
        public string TargetLanguage { get; set; }
        private readonly OpenAIClient client;
        /// <summary>
        /// Create a translator using OpenAI model
        /// </summary>
        /// <param name="client">Set client to use</param>
        /// <param name="sourceLanguage">Set source language</param>
        /// <param name="targetLanguage">Set target language</param>
        /// <param name="usePromptPreset">Set to use translator system prompt preset</param>
        public OpenAITranslator(OpenAIClient client, string sourceLanguage, string targetLanguage, bool usePromptPreset = true)
        {
            this.client = client;
            SourceLanguage = sourceLanguage;
            TargetLanguage = targetLanguage;
            if (!usePromptPreset) return;
            if (sourceLanguage != null)
                client.SystemPrompt = $"{targetLanguage} and {sourceLanguage} are language codes. You should translate {sourceLanguage} to {targetLanguage}. You should only reply the translation.";
            else
                client.SystemPrompt = $"{targetLanguage} is language code. You should detect my language and translate them to {targetLanguage}. You should only reply the translation.";
        }
        public async UniTask<string> TranslateAsync(string input, CancellationToken ct)
        {
            return (await client.GenerateAsync(input, ct)).Response;
        }
    }
}