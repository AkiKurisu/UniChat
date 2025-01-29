using System;

namespace UniChat.LLMs
{
    /// <summary>
    /// Large language model backend type
    /// </summary>
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
        protected readonly ILLMSettings Settings;
        
        public static readonly ILLMSettings DefaultSettings = new LLMSettings();
        
        public LLMFactory(ILLMSettings settings = null)
        {
            settings ??= DefaultSettings;
            Settings = settings;
        }
        
        public ILargeLanguageModel CreateLLM(LLMType llmType)
        {
            ILargeLanguageModel llm = llmType switch
            {
                LLMType.OpenAI => new OpenAIClient(Settings.OpenAI_API_URL, Settings.Model_Type, Settings.OpenAIKey),
                LLMType.ChatGLM => new ChatGLMClient(Settings.LLM_Address, Settings.LLM_Port),
                LLMType.TextGenWebUI => new TextGenWebUIClient(Settings.LLM_Address, Settings.LLM_Port),
                LLMType.Ollama_Chat => new OllamaChat(Settings.LLM_Address, Settings.LLM_Port)
                {
                    Model = Settings.Model_Type
                },
                LLMType.Ollama_Completion => new OllamaCompletion(Settings.LLM_Address, Settings.LLM_Port)
                {
                    Model = Settings.Model_Type
                },
                LLMType.KoboldCpp => new KoboldCppClient(Settings.LLM_Address, Settings.LLM_Port),
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
                LLMType.OpenAI => new OpenAIClient(Settings.OpenAI_API_URL, Settings.Model_Type, Settings.OpenAIKey),
                LLMType.ChatGLM => new ChatGLMClient(Settings.LLM_Address, Settings.LLM_Port),
                LLMType.TextGenWebUI => new TextGenWebUIClient(Settings.LLM_Address, Settings.LLM_Port),
                LLMType.Ollama_Chat => new OllamaChat(Settings.LLM_Address, Settings.LLM_Port)
                {
                    Model = Settings.Model_Type
                },
                LLMType.Ollama_Completion => new OllamaCompletion(Settings.LLM_Address, Settings.LLM_Port)
                {
                    Model = Settings.Model_Type
                },
                LLMType.KoboldCpp => new KoboldCppClient(Settings.LLM_Address, Settings.LLM_Port),
                _ => throw new ArgumentOutOfRangeException(nameof(llmType), "Input type is not a valid chat model")
            };
            return llm;
        }
    }
}
