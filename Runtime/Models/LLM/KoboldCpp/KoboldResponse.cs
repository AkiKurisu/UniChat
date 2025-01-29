using System.Collections.Generic;
using Newtonsoft.Json;
namespace UniChat.LLMs
{
    public class KoboldResponse
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
