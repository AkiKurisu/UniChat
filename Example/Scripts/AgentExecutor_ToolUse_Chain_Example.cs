using Cysharp.Threading.Tasks;
using Kurisu.UniChat.Chains;
using Kurisu.UniChat.LLMs;
using Kurisu.UniChat.Tools;
using UnityEngine;
namespace Kurisu.UniChat.Example
{
    public class AgentExecutor_ToolUse_Chain_Example : MonoBehaviour
    {
        public LLMSettingsAsset settingsAsset;
        public async void Start()
        {
            var userCommand = @"I want to watch a dance video.";
            var llm = LLMFactory.Create(LLMType.ChatGPT, settingsAsset) as OpenAIClient;
            llm.StopWords = new() { "\nObservation:", "\n\tObservation:" };

            //Create agent with multi-tools
            var chain =
                Chain.Set(userCommand)
                | Chain.ReActAgentExecutor(llm)
                    .UseTool(new AgentLambdaTool(
                        "Play random dance video",
                        @"A wrapper to select dance video and play it. Input should be 'None'.",
                        (e) =>
                        {
                            //PlayRandomVideo();
                            return UniTask.FromResult("Dance video 'Queencard' is playing now.");
                        }))
                    .UseTool(new AgentLambdaTool(
                        "Sleep",
                        @"A wrapper to sleep.",
                        (e) =>
                        {
                            //SetSleep(true);
                            return UniTask.FromResult("You are now sleeping.");
                        }))
                    .Verbose(true);

            //Run chain
            Debug.Log(await chain.Run("text"));
        }
    }
}
