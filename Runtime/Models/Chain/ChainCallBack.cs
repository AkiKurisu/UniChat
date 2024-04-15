using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
namespace Kurisu.UniChat.Chains
{
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
        public async UniTask<CallbackManagerForChainRun> HandleChainStart(
            Chain chain,
            IChainValues inputs,
            string runId = null)
        {
            inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
            runId ??= Guid.NewGuid().ToString();

            foreach (var handler in Handlers)
            {
                try
                {
                    await handler.HandleChainStartAsync(chain, inputs.Value, runId, ParentRunId);
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
            ICallbacks inheritableCallbacks = null,
            ICallbacks localCallbacks = null,
            IReadOnlyList<string> localTags = null,
            IReadOnlyList<string> inheritableTags = null,
            IReadOnlyDictionary<string, object> localMetadata = null,
            IReadOnlyDictionary<string, object> inheritableMetadata = null)
        {
            string parentId = null;

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

            return UniTask.FromResult(callBack);
        }
    }
}