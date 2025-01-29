using Newtonsoft.Json;
namespace UniChat.LLMs
{
    public class OllamaOptions
    {

        [JsonProperty("num_keep")]
        public int? NumKeep { get; set; } = 24;


        [JsonProperty("seed")]
        public int? Seed { get; set; }


        [JsonProperty("num_predict")]
        public int? NumPredict { get; set; }


        [JsonProperty("top_k")]
        public int? TopK { get; set; }


        [JsonProperty("top_p")]
        public double? TopP { get; set; }


        [JsonProperty("tfs_z")]
        public double? TfsZ { get; set; }


        [JsonProperty("typical_p")]
        public double? TypicalP { get; set; }


        [JsonProperty("repeat_last_n")]
        public int? RepeatLastN { get; set; }


        [JsonProperty("temperature")]
        public double? Temperature { get; set; }


        [JsonProperty("repeat_penalty")]
        public double? RepeatPenalty { get; set; }


        [JsonProperty("presence_penalty")]
        public double? PresencePenalty { get; set; }


        [JsonProperty("frequency_penalty")]
        public double? FrequencyPenalty { get; set; }


        [JsonProperty("mirostat")]
        public int? Mirostat { get; set; }


        [JsonProperty("mirostat_tau")]
        public double? MirostatTau { get; set; }


        [JsonProperty("mirostat_eta")]
        public double? MirostatEta { get; set; }


        [JsonProperty("penalize_newline")]
        public bool? PenalizeNewline { get; set; }

        // See https://ollama.com/library/llama3:latest/blobs/577073ffcc6c
        [JsonProperty("stop")]
        public string[] Stop { get; set; } = {
            "<|start_header_id|>",
            "<|end_header_id|>",
            "<|eot_id|>"
        };


        [JsonProperty("numa")]
        public bool? Numa { get; set; }


        [JsonProperty("num_ctx")]
        public int? NumCtx { get; set; }


        [JsonProperty("num_batch")]
        public int? NumBatch { get; set; }


        [JsonProperty("num_gqa")]
        public int? NumGqa { get; set; }


        [JsonProperty("num_gpu")]
        public int? NumGpu { get; set; }


        [JsonProperty("main_gpu")]
        public int? MainGpu { get; set; }


        [JsonProperty("low_vram")]
        public bool? LowVram { get; set; }


        [JsonProperty("f16_kv")]
        public bool? F16Kv { get; set; }


        [JsonProperty("vocab_only")]
        public bool? VocabOnly { get; set; }


        [JsonProperty("use_mmap")]
        public bool? UseMmap { get; set; }


        [JsonProperty("use_mlock")]
        public bool? UseMlock { get; set; }


        [JsonProperty("embedding_only")]
        public bool? EmbeddingOnly { get; set; }


        [JsonProperty("rope_frequency_base")]
        public double? RopeFrequencyBase { get; set; }


        [JsonProperty("rope_frequency_scale")]
        public double? RopeFrequencyScale { get; set; }


        [JsonProperty("num_thread")]
        public int? NumThread { get; set; }
    }
}