using System.Collections.Generic;
using Newtonsoft.Json;
namespace UniChat.LLMs
{
    public class KoboldGenParams
    {
        [JsonProperty("n")]
        public int N { get; set; } = 1;
        
        [JsonProperty("max_context_length")]
        public int MaxContextLength { get; set; } = 2048;
        
        [JsonProperty("max_length")]
        public int MaxLength { get; set; } = 80;
        
        [JsonProperty("rep_pen")]
        public float RepPen { get; set; } = 1.1f;
        
        [JsonProperty("temperature")]
        public float Temperature { get; set; } = 0.7f;
        
        [JsonProperty("top_p")]
        public float TopP { get; set; } = 0.92f;
        
        [JsonProperty("top_k")]
        public int TopK { get; set; } = 0;
        
        [JsonProperty("top_a")]
        public int TopA { get; set; } = 0;
        
        [JsonProperty("typical")]
        public int Typical { get; set; } = 1;
        
        [JsonProperty("tfs")]
        public int Tfs { get; set; } = 1;
        
        [JsonProperty("rep_pen_range")]
        public int RepPenRange { get; set; } = 300;
        
        [JsonProperty("rep_pen_slope")]
        public float RepPenSlope { get; set; } = 0.7f;
        
        [JsonProperty("sampler_order")]
        public int[] SamplerOrder { get; set; } = { 6, 0, 1, 3, 4, 2, 5 };
        
        [JsonProperty("prompt")]
        public string Prompt { get; set; }
        
        [JsonProperty("quiet")]
        public bool Quiet { get; set; } = true;
        
        [JsonProperty("disable_input_formatting")]
        public bool Disable_input_formatting { get; set; } = true;
        
        [JsonProperty("disable_output_formatting")]
        public bool Disable_output_formatting { get; set; } = true;
        
        [JsonProperty("frmtadsnsp")]
        public bool Frmtadsnsp { get; set; }
        
        [JsonProperty("frmtrmblln")]
        public bool Frmtrmblln { get; set; }
        
        [JsonProperty("frmtrmspch")]
        public bool Frmtrmspch { get; set; }
        
        [JsonProperty("frmttriminc")]
        public bool Frmttriminc { get; set; }
        
        [JsonProperty("stop_sequence")]
        public List<string> StopSequence { get; set; } = new() { "You:", "\nYou " };
        
        public static readonly string[] AlwaysReplaceKey = { "<|endoftext|>" };
        
        [JsonIgnore]
        public List<string> ReplaceKey { get; set; } = new() { "Bot:" };
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
