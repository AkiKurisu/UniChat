namespace Kurisu.UniChat
{
    public static class ChatMessageExtensions
    {
        public static bool TryGetMessage(this IChatMemory chatMemory, MessageRole messageRole, uint hash, out ChatMessage message)
        {
            foreach (var botMg in chatMemory.GetMessages(messageRole))
            {
                if (botMg.id == hash)
                {
                    message = botMg; return true;
                }
            }
            message = null;
            return false;
        }
    }
}