using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
namespace Kurisu.UniChat.Chains
{
    public class CallbackManagerForChainRun : ParentRunManager, IChainRunner<CallbackManagerForChainRun>
    {
        public CallbackManagerForChainRun()
        {

        }

        public CallbackManagerForChainRun(
            string runId,
            List<CallbackHandler> handlers,
            List<CallbackHandler> inheritableHandlers,
            string parentRunId = null)
            : base(runId, handlers, inheritableHandlers, parentRunId)
        {
        }

        public async UniTask HandleChainEndAsync(IChainValues input, IChainValues output)
        {
            input = input ?? throw new ArgumentNullException(nameof(input));
            output = output ?? throw new ArgumentNullException(nameof(output));
            RunContext.GetContext(input).End(RunId);
            foreach (var handler in Handlers)
            {
                try
                {
                    await handler.HandleChainEndAsync(
                        input.Value,
                        output.Value,
                        RunId,
                        ParentRunId);
                }
                catch (Exception ex)
                {
                    await Console.Error.WriteLineAsync($"Error in handler {handler.GetType().Name}, HandleChainEnd: {ex}");
                }
            }
        }

        public async UniTask HandleChainErrorAsync(Exception error, IChainValues input)
        {
            input = input ?? throw new ArgumentNullException(nameof(input));
            RunContext.GetContext(input).End(RunId);
            foreach (var handler in Handlers)
            {
                try
                {
                    await handler.HandleChainErrorAsync(error, RunId, input.Value, ParentRunId);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in handler {handler.GetType().Name}, HandleChainError: {ex}");
                }
            }
        }

        public async UniTask HandleTextAsync(string text)
        {
            foreach (var handler in Handlers)
            {
                try
                {
                    await handler.HandleTextAsync(text, RunId, ParentRunId);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in handler {handler.GetType().Name}, HandleText: {ex}");
                }
            }
        }
    }
    public class CallbackManagerForLlmRun : RunManager
    {
        public CallbackManagerForLlmRun(string runId, List<CallbackHandler> handlers, List<CallbackHandler> inheritableHandlers, string parentRunId = null)
            : base(runId, handlers, inheritableHandlers, parentRunId)
        {
        }

        public async UniTask HandleLlmNewTokenAsync(string token, string runId, string parentRunId)
        {
            foreach (var handler in Handlers)
            {
                try
                {
                    await handler.HandleLlmNewTokenAsync(token, RunId, ParentRunId);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in handler {handler.GetType().Name}, HandleLLMNewToken: {ex}");
                }
            }
        }

        public async UniTask HandleLlmErrorAsync(Exception error, string runId, string parentRunId)
        {
            foreach (var handler in Handlers)
            {
                try
                {
                    await handler.HandleLlmErrorAsync(error, RunId, ParentRunId);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in handler {handler.GetType().Name}, HandleLLMError: {ex}");
                }
            }
        }

        public async UniTask HandleLlmEndAsync(ILLMResponse output, string runId, string parentRunId)
        {
            foreach (var handler in Handlers)
            {
                try
                {
                    await handler.HandleLlmEndAsync(output, RunId, ParentRunId);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in handler {handler.GetType().Name}, HandleLLMEnd: {ex}");
                }
            }
        }
    }
}