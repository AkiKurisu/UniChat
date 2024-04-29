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
        protected readonly ILLMSettings settings;
        public static ILLMSettings defaultSettings = new LLMSettings();
        public LLMFactory(ILLMSettings settings = null)
        {
            settings ??= defaultSettings;
            this.settings = settings;
        }
        public ILargeLanguageModel CreateLLM(LLMType llmType)
        {
            ILargeLanguageModel llm = llmType switch
            {
                LLMType.ChatGPT => new OpenAIClient(settings.OpenAI_API_URL, settings.Model_Type, settings.OpenAIKey),
                LLMType.ChatGLM => new ChatGLMClient(settings.LLM_Address, settings.LLM_Port),
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
    public class ChatModelFactory : LLMFactory
    {
        public ChatModelFactory(ILLMSettings settings = null) : base(settings) { }
        public IChatModel CreateChatModel(LLMType llmType)
        {
            IChatModel llm = llmType switch
            {
                LLMType.ChatGPT => new OpenAIClient(settings.OpenAI_API_URL, settings.Model_Type, settings.OpenAIKey),
                LLMType.ChatGLM => new ChatGLMClient(settings.LLM_Address, settings.LLM_Port),
                LLMType.Oobabooga => new OobaboogaClient(settings.LLM_Address, settings.LLM_Port),
                LLMType.Ollama_Chat => new OllamaChat(settings.LLM_Address, settings.LLM_Port)
                {
                    Model = settings.Model_Type
                },
                LLMType.Ollama_Completion => new OllamaCompletion(settings.LLM_Address, settings.LLM_Port)
                {
                    Model = settings.Model_Type
                },
                _ => throw new ArgumentOutOfRangeException(nameof(llmType), "Input type is not a valid chat model")
            };
            return llm;
        }
    }
}
