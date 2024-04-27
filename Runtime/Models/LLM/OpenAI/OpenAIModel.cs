using System.Collections.Generic;
namespace Kurisu.UniChat.LLMs
{
    public class PostData
    {
        public string model;
        public float temperature;
        public float top_p;
        public List<SendData> messages;
        public List<string> stop;
    }
    public class SendData
    {
        public string role;
        public string content;
        public SendData(string _role, string _content)
        {
            role = _role;
            content = _content;
        }

    }
    public class MessageBack
    {
        public string id;
        public string created;
        public string model;
        public List<MessageBody> choices;
    }
    public class MessageBody
    {
        public Message message;
        public string finish_reason;
        public string index;
    }
    public class Message
    {
        public string role;
        public string content;
    }
}