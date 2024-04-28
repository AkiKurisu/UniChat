namespace Kurisu.UniChat
{
    public interface IChatModel : ILargeLanguageModel
    {
        string SystemPrompt { get; set; }
    }
}