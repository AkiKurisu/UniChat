using System;
using UnityEngine;
namespace UniChat.LLMs
{
        public interface ILLMSettings
        {
                string OpenAI_API_URL { get; }
                
                string OpenAIKey { get; }
                
                string Model_Type { get; }
                
                string LLM_Address { get; }
                
                string LLM_Port { get; }
                
                string LLM_Language { get; }
        }
        
        public class LLMSettings : ILLMSettings
        {
                public string OpenAI_API_URL { get; set; } = "https://api.openai-proxy.com/v1/chat/completions";
                
                public string OpenAIKey { get; set; }
                
                public string Model_Type { get; set; } = "gpt-3.5-turbo";
                
                public string LLM_Address { get; set; } = "127.0.0.1";
                
                public string LLM_Port { get; set; } = "8000";
                
                
                public string LLM_Language { get; set; } = "en";
        }
        
        [CreateAssetMenu(fileName = "LLMSettingsAsset", menuName = "UniChat/LLM Settings Asset")]
        public class LLMSettingsAsset : ScriptableObject, ILLMSettings
        {
                public enum ModelType
                {
                        Custom,
                        
                        ChatGPT3,
                        
                        ChatGPT4,
                        
                        Llama2,
                        
                        Llama3,
                        
                        Qwen,
                        
                        Llama2_Uncensored
                }
                
                [field: Header("OpenAI Setting")]
                [field: SerializeField]
                public string OpenAI_API_URL { get; set; }
                
                [field: SerializeField]
                public string OpenAIKey { get; set; }
                
                [field: Header("Model Setting")]
                [Tooltip("Set model type from list of use custom model")]
                public ModelType modelType;
                
                public string Model_Type
                {
                        get
                        {
                                if (modelType == ModelType.Custom) return customModel;
                                return modelType switch
                                {
                                        ModelType.ChatGPT3 => OpenAIModels.ChatGPT3,
                                        ModelType.ChatGPT4 => OpenAIModels.ChatGPT4,
                                        ModelType.Llama2 => OllamaModels.Llama2,
                                        ModelType.Llama3 => OllamaModels.Llama3,
                                        ModelType.Qwen => OllamaModels.Qwen,
                                        ModelType.Llama2_Uncensored => OllamaModels.Llama2_Uncensored,
                                        _ => throw new ArgumentOutOfRangeException(nameof(modelType)),
                                };
                        }
                }
                
                public string customModel;

                [field: Header("Local LLM Setting")]
                [field: SerializeField]
                public string LLM_Address { get; set; } = "127.0.0.1";
                
                [field: SerializeField]
                public string LLM_Port { get; set; } = "5001";
                
                [field: SerializeField, Tooltip("LLM output and input language code")]
                public string LLM_Language { get; set; } = "en";
        }
}
