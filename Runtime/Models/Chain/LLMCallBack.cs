using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Kurisu.UniChat.Chains
{
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