using System;
using Newtonsoft.Json;
namespace UniChat.LLMs
{
    public class OllamaListModelsResponse
    {
        [JsonProperty("models")]
        public Model[] Models { get; set; } = new Model[0];
        public class Model
        {
            [JsonProperty("name")]
            public string Name { get; set; } = string.Empty;


            [JsonProperty("modified_at")]
            public DateTime ModifiedAt { get; set; }


            [JsonProperty("size")]
            public long Size { get; set; }


            [JsonProperty("digest")]
            public string Digest { get; set; } = string.Empty;
        }
    }
}