using System.Threading;
using Cysharp.Threading.Tasks;
using UniChat.Chains;

namespace UniChat.Translators
{
    public class LLMTranslator : ITranslator
    {
        private readonly StackableChain innerChain;
        private string input;
        /// <summary>
        /// Create a translator using llm.
        /// </summary>
        /// <param name="chatModel">Set chat model to use</param>
        /// <param name="sourceLanguage">Set source language</param>
        /// <param name="targetLanguage">Set target language</param>
        /// <param name="usePromptPreset">Set to use translator system prompt preset</param>
        public LLMTranslator(ILargeLanguageModel llm, string sourceLanguage, string targetLanguage, string prompt = null)
        {
            if (string.IsNullOrEmpty(prompt))
            {
                if (!string.IsNullOrEmpty(sourceLanguage))
                    prompt = "{targetLanguage} and {sourceLanguage} are language codes. You should translate {sourceLanguage} to {targetLanguage}. You should only reply the translation.\n{input}";
                else
                    prompt = "{targetLanguage} is language code. You should detect my language and translate them to {targetLanguage}. You should only reply the translation.\n{input}";
            }
            innerChain = Chain.Set(sourceLanguage, "sourceLanguage")
                        | Chain.Set(targetLanguage, "targetLanguage")
                        | Chain.Set(() => input, "input")
                        | Chain.Template(prompt)
                        | Chain.LLM(llm);
        }
        public async UniTask<string> TranslateAsync(string input, CancellationToken ct)
        {
            this.input = input;
            return await innerChain.Run("text");
        }
    }
}