using System.Collections.Generic;
namespace Kurisu.UniChat.LLMs
{
    public class OllamaChatRequest
    {
        public string model;
        public float temperature;
        public float top_p;
        public bool stream = false;
        public List<SendData> messages;
        public List<string> stop;
    }
    public class OllamaChatResponse
    {
        public string created_at;
        public string model;
        public Message message;
    }
}