using System.Collections.Generic;
namespace UniChat.LLMs
{
    public class OllamaChatRequest
    {
        public string model;
        public double? temperature;
        public double? top_p;
        public bool stream = false;
        public List<SendData> messages;
        public string[] stop;
    }
    public class OllamaChatResponse
    {
        public string created_at;
        public string model;
        public Message message;
    }
}