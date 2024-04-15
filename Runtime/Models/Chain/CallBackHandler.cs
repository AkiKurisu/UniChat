using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
namespace Kurisu.UniChat.Chains
{
    public abstract class CallbackHandler : ICallbackHandler
    {
        public abstract string Name { get; }
        public abstract UniTask HandleChainStartAsync(IChain chain, Dictionary<string, object> inputs,
            string runId, string parentRunId = null,
            List<string> tags = null,
            Dictionary<string, object> metadata = null,
            string runType = null,
            string name = null,
            Dictionary<string, object> extraParams = null);

        public abstract UniTask HandleChainErrorAsync(
            Exception err, string runId,
            Dictionary<string, object> inputs = null,
            string parentRunId = null);

        public abstract UniTask HandleChainEndAsync(
            Dictionary<string, object> inputs,
            Dictionary<string, object> outputs,
            string runId,
            string parentRunId = null);

        public abstract UniTask HandleToolStartAsync(
            Dictionary<string, object> tool,
            string input, string runId,
            string parentRunId = null,
            List<string> tags = null,
            Dictionary<string, object> metadata = null,
            string runType = null,
            string name = null,
            Dictionary<string, object> extraParams = null);

        public abstract UniTask HandleToolErrorAsync(Exception err, string runId, string parentRunId = null);
        public abstract UniTask HandleToolEndAsync(string output, string runId, string parentRunId = null);
        public abstract UniTask HandleTextAsync(string text, string runId, string parentRunId = null);
        public abstract UniTask HandleAgentActionAsync(Dictionary<string, object> action, string runId, string parentRunId = null);
        public abstract UniTask HandleAgentEndAsync(Dictionary<string, object> action, string runId, string parentRunId = null);
    }
    public class ManagerCallbacks : ICallbacks
    {
        public ChainCallback Value { get; set; }
        public ManagerCallbacks(ChainCallback value)
        {
            Value = value;
        }
    }
    public class HandlersCallbacks : ICallbacks
    {
        public List<CallbackHandler> Value { get; set; }
        public HandlersCallbacks(List<CallbackHandler> value)
        {
            Value = value;
        }
    }
}