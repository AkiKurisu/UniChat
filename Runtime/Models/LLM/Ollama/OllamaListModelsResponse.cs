using System;
using Newtonsoft.Json;
namespace UniChat.LLMs
{
    public class OllamaListModelsResponse
    {
        [JsonProperty("models")]
        public Model[] Models { get; set; } = Array.Empty<Model>();
        
        public class Model
        {
            /// <summary>
            /// Model fullname
            /// </summary>
            [JsonProperty("name")]
            public string Name { get; set; } = string.Empty;


            [JsonProperty("modified_at")]
            public DateTime ModifiedAt { get; set; }


            [JsonProperty("size")]
            public long Size { get; set; }


            [JsonProperty("digest")]
            public string Digest { get; set; } = string.Empty;

            public string GetModelName()
            {
                if (Name.Contains(':')) return Name.Split(':')[0];
                return Name;
            }
        }
    }
}