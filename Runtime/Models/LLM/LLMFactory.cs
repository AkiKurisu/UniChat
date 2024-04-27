using System;
namespace Kurisu.UniChat.LLMs
{
    public enum LLMType
    {
        ChatGPT,
        Oobabooga,
        ChatGLM,
        Ollama_Chat,
        Ollama_Completion
    }
    public class LLMFactory
    {
        public static ILLMSettings defaultSettings = new LLMSettings();
        public static ILargeLanguageModel Create(LLMType llmType, ILLMSettings settings = null)
        {
            settings ??= defaultSettings;
            ILargeLanguageModel llm = llmType switch
            {
                //GPT models support multilingual input
                LLMType.ChatGPT => new OpenAIClient(settings.OpenAI_API_URL, settings.Model_Type, settings.OpenAIKey),
                LLMType.ChatGLM => new ChatGLMClient(settings.LLM_Address, settings.LLM_Port),
                //Tips: Oobabooga can set google translation in server
                LLMType.Oobabooga => new OobaboogaClient(settings.LLM_Address, settings.LLM_Port),
                LLMType.Ollama_Chat => new OllamaChat(settings.LLM_Address, settings.LLM_Port)
                {
                    Model = settings.Model_Type
                },
                LLMType.Ollama_Completion => new OllamaCompletion(settings.LLM_Address, settings.LLM_Port)
                {
                    Model = settings.Model_Type
                },
                _ => throw new ArgumentOutOfRangeException(nameof(llmType)),
            };
            return llm;
        }
    }
}
