using System.Collections.Generic;
using Newtonsoft.Json;
namespace Kurisu.UniChat.LLMs
{
    public class TextGenWebUICompletionResponse
    {
        [JsonProperty("results")]
        public List<Result> Results { get; set; }
        public class Result
        {
            [JsonProperty("text")]
            public string Text { get; set; }
        }
    }
}
