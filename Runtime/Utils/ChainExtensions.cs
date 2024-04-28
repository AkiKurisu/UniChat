using Kurisu.UniChat.Chains;
namespace Kurisu.UniChat
{
    public static class ChainExtensions
    {
        /// <summary>
        /// Convert run pipeline with input action to chain
        /// </summary>
        /// <param name="chatPipelineCtrl"></param>
        /// <param name="inputKey"></param>
        /// <param name="outputKey"></param>
        /// <returns></returns>
        public static ChatPipelineChain ToChain(this ChatPipelineCtrl chatPipelineCtrl, string inputKey = "input", string outputKey = "context")
        {
            return new ChatPipelineChain(chatPipelineCtrl, inputKey, outputKey);
        }
    }
}