using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;
namespace UniChat.Chains
{
    /// <summary>
    /// A replacement of run_tree_context (https://github.com/langchain-ai/langsmith-sdk/blob/main/python/langsmith/run_helpers.py).
    /// Since AsyncLocal not worked in UniTask (https://github.com/Cysharp/UniTask?tab=readme-ov-file#net-core).
    /// Instead, we use a stacked based context to trace run id.
    /// </summary>
    public class RunContext
    {
        public IChainValues Values { get; private set; }
        /// <summary>
        /// Whether trace the whole run session
        /// </summary>
        /// <value></value>
        public bool StackTrace { get; set; }
        /// <summary>
        /// The id of current run
        /// </summary> <summary>
        public string RunId
        {
            get
            {
                if (runStack.TryPeek(out var runId)) return runId;
                return null;
            }
        }
        private static readonly Dictionary<IChainValues, RunContext> contextMap = new();
        public readonly Stack<string> runStack = new();
        private static readonly ObjectPool<RunContext> pool = new(() => new RunContext(), null, delegate (RunContext context)
        {
            context.runStack.Clear();
            context.Values = null;
            context.StackTrace = false;
        });
        private static RunContext Get(IChainValues chainValues)
        {
            var context = pool.Get();
            context.Values = chainValues;
            return context;
        }
        private static void Release(RunContext toRelease)
        {
            pool.Release(toRelease);
        }
        /// <summary>
        /// Start a run step
        /// </summary>
        /// <param name="runId"></param>
        public void Start(string runId)
        {
            runStack.Push(runId);
        }
        /// <summary>
        /// End a run step
        /// </summary>
        /// <param name="runId"></param>
        public void End(string runId)
        {
            if (RunId == runId)
            {
                runStack.Pop();
                //Release after stack empty
                if (!runStack.TryPeek(out _))
                {
                    ReleaseContext(Values);
                }
            }
            else
            {
                throw new TracerException($"Run id on top of the stack is not {runId}.");
            }
        }
        public static void ReleaseContext(IChainValues values)
        {
            if (contextMap.TryGetValue(values, out var context))
            {
                contextMap.Remove(values);
                Release(context);
            }
        }
        /// <summary>
        /// Get or create a run context
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static RunContext GetContext(IChainValues values)
        {
            if (!contextMap.TryGetValue(values, out var context))
                context = contextMap[values] = Get(values);
            return context;
        }
    }
    public class ChainCallback
    {
        public List<CallbackHandler> Handlers { get; private set; }
        public List<CallbackHandler> InheritableHandlers { get; private set; }
        public string ParentRunId { get; }
        protected List<string> Tags { get; }
        protected List<string> InheritableTags { get; }
        protected Dictionary<string, object> Metadata { get; }
        protected Dictionary<string, object> InheritableMetadata { get; }
        public ChainCallback(
            List<CallbackHandler> handlers = null,
            List<CallbackHandler> inheritableHandlers = null,
            List<string> tags = null,
            List<string> inheritableTags = null,
            Dictionary<string, object> metadata = null,
            Dictionary<string, object> inheritableMetadata = null,
            string parentRunId = null)
        {
            Handlers = handlers ?? new List<CallbackHandler>();
            InheritableHandlers = inheritableHandlers ?? new List<CallbackHandler>();
            Tags = tags ?? new();
            InheritableTags = inheritableTags ?? new();
            ParentRunId = parentRunId;
            Metadata = metadata ?? new();
            InheritableMetadata = inheritableMetadata ?? new();
        }
        public void AddTags(IReadOnlyList<string> tags, bool inherit = true)
        {
            Tags.RemoveAll(tag => tags.Contains(tag));
            Tags.AddRange(tags);

            if (inherit)
            {
                InheritableTags.AddRange(tags);
            }
        }

        public void RemoveTags(IReadOnlyList<string> tags)
        {
            tags = tags ?? throw new ArgumentNullException(nameof(tags));

            foreach (var tag in tags)
            {
                Tags.Remove(tag);
                InheritableTags.Remove(tag);
            }
        }
        public void AddMetadata(IReadOnlyDictionary<string, object> metadata, bool inherit = true)
        {
            metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));

            foreach (var kv in metadata)
            {
                Metadata[kv.Key] = kv.Value;
                if (inherit)
                {
                    InheritableMetadata[kv.Key] = kv.Value;
                }
            }
        }
        public void RemoveMetadata(IReadOnlyList<string> keys)
        {
            keys = keys ?? throw new ArgumentNullException(nameof(keys));

            foreach (var key in keys)
            {
                Metadata.Remove(key);
                InheritableMetadata.Remove(key);
            }
        }

        public async UniTask<CallbackManagerForLlmRun> HandleLlmStart(
            ILargeLanguageModel llm,
            IReadOnlyList<string> prompts,
            string runId = null,
            string parentRunId = null,
            IReadOnlyDictionary<string, object> extraParams = null)
        {
            runId ??= Guid.NewGuid().ToString();

            foreach (var handler in Handlers)
            {
                try
                {
                    await handler.HandleLlmStartAsync(
                        llm,
                        prompts.ToArray(),
                        runId,
                        ParentRunId,
                        extraParams: extraParams);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in handler {handler.GetType().Name}, HandleLLMStart: {ex}");
                }
            }

            return new CallbackManagerForLlmRun(runId, Handlers, InheritableHandlers, ParentRunId);
        }

        public async UniTask<CallbackManagerForLlmRun> HandleChatModelStart(
            ILargeLanguageModel llm,
            IReadOnlyList<List<IMessage>> messages,
            string runId = null,
            string parentRunId = null,
            IReadOnlyDictionary<string, object> extraParams = null)
        {
            runId ??= Guid.NewGuid().ToString();

            foreach (var handler in Handlers)
            {
                try
                {
                    await handler.HandleChatModelStartAsync(llm, messages, runId, ParentRunId, extraParams);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in handler {handler.GetType().Name}, HandleLLMStart: {ex}");
                }
            }

            return new CallbackManagerForLlmRun(runId, Handlers, InheritableHandlers, ParentRunId);
        }
        public async UniTask<CallbackManagerForChainRun> HandleChainStart(
            IChain chain,
            IChainValues inputs,
            string runId = null)
        {
            inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
            runId ??= Guid.NewGuid().ToString();
            RunContext.GetContext(inputs).Start(runId);
            foreach (var handler in Handlers)
            {
                try
                {
                    await handler.HandleChainStartAsync(chain, inputs.Value, runId, ParentRunId, name: chain.GetType().Name);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in handler {handler.GetType().Name}, HandleChainStart: {ex}");
                }
            }
            return new CallbackManagerForChainRun(runId, Handlers, InheritableHandlers, ParentRunId);
        }
        public void AddHandler(CallbackHandler handler, bool inherit = true)
        {
            Handlers.Add(handler);
            if (inherit)
            {
                InheritableHandlers.Add(handler);
            }
        }

        public void RemoveHandler(CallbackHandler handler)
        {
            Handlers.Remove(handler);
            InheritableHandlers.Remove(handler);
        }

        public void SetHandlers(IEnumerable<CallbackHandler> handlers)
        {
            Handlers = handlers.ToList();
        }
        public void SetHandlers(List<CallbackHandler> handlers, bool inherit = true)
        {
            handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));

            Handlers.Clear();
            InheritableHandlers.Clear();
            foreach (var handler in handlers)
            {
                AddHandler(handler, inherit);
            }
        }
        public ChainCallback Copy(List<CallbackHandler> additionalHandlers = null, bool inherit = true)
        {
            var callBack = new ChainCallback(parentRunId: ParentRunId);
            foreach (var handler in Handlers)
            {
                var inheritable = InheritableHandlers.Contains(handler);
                callBack.AddHandler(handler, inheritable);
            }

            if (additionalHandlers != null)
            {
                foreach (var handler in additionalHandlers)
                {
                    callBack.AddHandler(handler, inherit);
                }
            }
            return callBack;
        }

        public static ChainCallback FromHandlers(List<CallbackHandler> handlers)
        {
            handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));

            var callBack = new ChainCallback();

            foreach (var handler in handlers)
            {
                callBack.AddHandler(handler);
            }

            return callBack;
        }

        public static UniTask<ChainCallback> Configure(
            string parentId,
            ICallbacks localCallbacks = null,
            ICallbacks inheritableCallbacks = null,
            IReadOnlyList<string> localTags = null,
            IReadOnlyList<string> inheritableTags = null,
            IReadOnlyDictionary<string, object> localMetadata = null,
            IReadOnlyDictionary<string, object> inheritableMetadata = null,
            bool stackTrace = false)
        {

            ChainCallback callBack;

            if (inheritableCallbacks != null || localCallbacks != null)
            {
                switch (inheritableCallbacks)
                {
                    case HandlersCallbacks inheritableHandlers:
                        callBack = new ChainCallback(parentRunId: parentId);
                        callBack.SetHandlers(inheritableHandlers.Value, true);
                        break;

                    case ManagerCallbacks managerCallbacks:
                        callBack = new ChainCallback(
                            managerCallbacks.Value.Handlers.ToList(),
                            managerCallbacks.Value.InheritableHandlers.ToList(),
                            managerCallbacks.Value.Tags.ToList(),
                            managerCallbacks.Value.InheritableTags.ToList(),
                            managerCallbacks.Value.Metadata.ToDictionary(kv => kv.Key, kv => kv.Value),
                            managerCallbacks.Value.InheritableMetadata.ToDictionary(kv => kv.Key, kv => kv.Value),
                            parentRunId: managerCallbacks.Value.ParentRunId);
                        break;

                    default:
                        callBack = new ChainCallback(parentRunId: parentId);
                        break;
                }

                var localHandlers = localCallbacks switch
                {
                    HandlersCallbacks localHandlersCallbacks => localHandlersCallbacks.Value,
                    ManagerCallbacks managerCallbacks => managerCallbacks.Value.Handlers,
                    _ => new List<CallbackHandler>()
                };

                callBack = callBack.Copy(localHandlers, false);
            }
            else
            {
                callBack = new ChainCallback(parentRunId: parentId);

            }
            if (inheritableTags != null) callBack.AddTags(inheritableTags);
            if (localTags != null) callBack.AddTags(localTags, inherit: false);
            if (inheritableMetadata != null) callBack.AddMetadata(inheritableMetadata);
            if (localMetadata != null) callBack.AddMetadata(localMetadata, inherit: false);

#if !UNICHAT_ALWAYS_TRACE_CHAIN
            if (stackTrace)
#endif
            {
                if (callBack.Handlers.All(h => h.Name != "console_callback_handler"))
                {
                    var consoleHandler = new ConsoleCallbackHandler();
                    callBack.AddHandler(consoleHandler, inherit: true);
                }
            }

            return UniTask.FromResult(callBack);
        }
    }
}