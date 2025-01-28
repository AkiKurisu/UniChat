using Newtonsoft.Json;
namespace UniChat.LLMs
{
    public class OllamaCompletionResponse
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("created_at")]
        public string CreatedAt { get; set; } = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("response")]
        public string Response { get; set; } = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("done")]
        public bool Done { get; set; }
    }
}