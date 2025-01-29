using System;
using System.Collections.Generic;
namespace UniChat.LLMs
{
    [Serializable]
    public class PostData
    {
        public string model;
        
        public float temperature;
        
        public float top_p;
        
        public List<SendData> messages;
        
        public List<string> stop;
    }
    
    [Serializable]
    public class SendData
    {
        public string role;
        
        public string content;
        
        public SendData(string inRole, string inContent)
        {
            role = inRole;
            content = inContent;
        }
    }
    
    [Serializable]
    public class MessageBack
    {
        public string id;
        
        public string created;
        
        public string model;
        
        public List<MessageBody> choices;
    }
    
    [Serializable]
    public class MessageBody
    {
        public Message message;
        
        public string finish_reason;
        
        public string index;
    }
    
    [Serializable]
    public class Message
    {
        public string role;
        
        public string content;
    }
}