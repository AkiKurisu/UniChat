using UnityEngine;
namespace Kurisu.UniChat.LLMs
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
                public enum GPT_ModelType
                {
                        [InspectorName("GPT-3.5-Turbo")]
                        GPT3_5,
                        [InspectorName("GPT-4")]
                        GPT4
                }
                [field: Header("LLM Setting")]
                [field: SerializeField]
                public string OpenAI_API_URL { get; set; }
                [Tooltip("Set ChatGPT model type")]
                public GPT_ModelType gptType;
                public string Model_Type
                {
                        get
                        {
                                if (gptType == GPT_ModelType.GPT3_5) return "gpt-3.5-turbo";
                                else return "gpt-4";
                        }
                }
                [field: SerializeField]
                public string OpenAIKey { get; set; }
                [field: SerializeField]
                public string LLM_Address { get; set; } = "127.0.0.1";
                [field: SerializeField]
                public string LLM_Port { get; set; } = "5001";
                [field: SerializeField, Tooltip("LLM output and input language code")]
                public string LLM_Language { get; set; } = "en";
        }
}
