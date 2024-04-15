using System.Collections.Generic;
using Newtonsoft.Json;
namespace Kurisu.UniChat.LLMs
{
    //Modify from https://github.com/pboardman/KoboldSharp
    public class ModelOutput
    {
        [JsonProperty("results")]
        public List<Result> Results { get; set; }
    }

    public class Result
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }
}
