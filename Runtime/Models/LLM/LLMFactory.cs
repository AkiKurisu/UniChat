using System;
namespace UniChat.LLMs
{
    public enum LLMType
    {
        OpenAI,
        TextGenWebUI,
        ChatGLM,
        /// <summary>
        /// Recommend api type for chat
        /// </summary>
        Ollama_Chat,
        /// <summary>
        /// Recommend api type for agent workflow
        /// </summary>
        Ollama_Completion,
        KoboldCpp
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
                LLMType.OpenAI => new OpenAIClient(settings.OpenAI_API_URL, settings.Model_Type, settings.OpenAIKey),
                LLMType.ChatGLM => new ChatGLMClient(settings.LLM_Address, settings.LLM_Port),
                LLMType.TextGenWebUI => new TextGenWebUIClient(settings.LLM_Address, settings.LLM_Port),
                LLMType.Ollama_Chat => new OllamaChat(settings.LLM_Address, settings.LLM_Port)
                {
                    Model = settings.Model_Type
                },
                LLMType.Ollama_Completion => new OllamaCompletion(settings.LLM_Address, settings.LLM_Port)
                {
                    Model = settings.Model_Type
                },
                LLMType.KoboldCpp => new KoboldCppClient(settings.LLM_Address, settings.LLM_Port),
                _ => throw new ArgumentOutOfRangeException(nameof(llmType))
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
                LLMType.OpenAI => new OpenAIClient(settings.OpenAI_API_URL, settings.Model_Type, settings.OpenAIKey),
                LLMType.ChatGLM => new ChatGLMClient(settings.LLM_Address, settings.LLM_Port),
                LLMType.TextGenWebUI => new TextGenWebUIClient(settings.LLM_Address, settings.LLM_Port),
                LLMType.Ollama_Chat => new OllamaChat(settings.LLM_Address, settings.LLM_Port)
                {
                    Model = settings.Model_Type
                },
                LLMType.Ollama_Completion => new OllamaCompletion(settings.LLM_Address, settings.LLM_Port)
                {
                    Model = settings.Model_Type
                },
                LLMType.KoboldCpp => new KoboldCppClient(settings.LLM_Address, settings.LLM_Port),
                _ => throw new ArgumentOutOfRangeException(nameof(llmType), "Input type is not a valid chat model")
            };
            return llm;
        }
    }
}
