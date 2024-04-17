using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
namespace Kurisu.UniChat
{
    public interface ICallbackHandler
    {
        string Name { get; }
        public abstract UniTask HandleLlmStartAsync(ILargeLanguageModel llm, string[] prompts, string runId, string parentRunId = null,
            IReadOnlyList<string> tags = null, IReadOnlyDictionary<string, object> metadata = null,
            string name = null, IReadOnlyDictionary<string, object> extraParams = null);

        public UniTask HandleLlmNewTokenAsync(
            string token,
            string runId,
            string parentRunId = null);

        public UniTask HandleLlmErrorAsync(
            Exception err,
            string runId,
            string parentRunId = null);

        public UniTask HandleLlmEndAsync(
            ILLMResponse output,
            string runId,
            string parentRunId = null);

        public UniTask HandleChatModelStartAsync(ILargeLanguageModel llm,
            IReadOnlyList<List<IMessage>> messages,
            string runId,
            string parentRunId = null,
            IReadOnlyDictionary<string, object> extraParams = null);

        public UniTask HandleChainStartAsync(IChain chain,
            Dictionary<string, object> inputs,
            string runId,
            string parentRunId = null,
            List<string> tags = null,
            Dictionary<string, object> metadata = null,
            string runType = null,
            string name = null,
            Dictionary<string, object> extraParams = null);

        public UniTask HandleChainErrorAsync(
            Exception err,
            string runId,
            Dictionary<string, object> inputs,
            string parentRunId = null);

        public UniTask HandleChainEndAsync(
            Dictionary<string, object> inputs,
            Dictionary<string, object> outputs,
            string runId,
            string parentRunId = null);

        public UniTask HandleToolStartAsync(
            Dictionary<string, object> tool,
            string input,
            string runId,
            string parentRunId = null,
            List<string> tags = null,
            Dictionary<string, object> metadata = null,
            string runType = null,
            string name = null,
            Dictionary<string, object> extraParams = null);
        public UniTask HandleToolErrorAsync(
            Exception err,
            string runId,
            string parentRunId = null);

        public UniTask HandleToolEndAsync(
            string output,
            string runId,
            string parentRunId = null);

        public UniTask HandleTextAsync(
            string text,
            string runId,
            string parentRunId = null);

        public UniTask HandleAgentActionAsync(
            Dictionary<string, object> action,
            string runId,
            string parentRunId = null);

        public UniTask HandleAgentEndAsync(
            Dictionary<string, object> action,
            string runId,
            string parentRunId = null);

        //Notice: Not implement in Unity

        // public UniTask HandleRetrieverStartAsync(
        //     BaseRetriever retriever,
        //     string query,
        //     string runId,
        //     string parentRunId,
        //     List<string> tags = null,
        //     Dictionary<string, object> metadata = null,
        //     string runType = null,
        //     string name = null,
        //     Dictionary<string, object> extraParams = null);

        // public UniTask HandleRetrieverEndAsync(
        //     string query,
        //     List<Document> documents,
        //     string runId,
        //     string parentRunId);

        // public UniTask HandleRetrieverErrorAsync(
        //     Exception exception,
        //     string query,
        //     string runId,
        //     string parentRunId);
    }
}