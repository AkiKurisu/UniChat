using Newtonsoft.Json;
namespace Kurisu.UniChat
{
    /// <summary>
    /// Chat session for <see cref="ChatHistory"/> ,data structure is similar to OobaboogaSession
    /// </summary>
    public class ChatSession
    {
        /// <summary>
        /// User name
        /// </summary>
        public string name1;
        /// <summary>
        /// Bot name
        /// </summary>
        public string name2;
        public ChatHistoryData history;
        /// <summary>
        /// Constructed system prompt
        /// </summary>
        public string context;
    }
    public class ChatHistoryData
    {
        [JsonProperty("internal")]
        public string[][] contents;
        public uint[][] ids;
    }
}