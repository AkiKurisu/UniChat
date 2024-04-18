using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
namespace Kurisu.UniChat.Chains
{
    public class RunManager
    {
        public string RunId { get; }
        protected List<CallbackHandler> Handlers { get; }
        protected List<CallbackHandler> InheritableHandlers { get; }
        protected string ParentRunId { get; }
        protected Dictionary<string, object> Metadata { get; }
        protected Dictionary<string, object> InheritableMetadata { get; }
        protected RunManager(
            string runId,
            List<CallbackHandler> handlers,
            List<CallbackHandler> inheritableHandlers,
            string parentRunId = null,
            Dictionary<string, object> metadata = null,
            Dictionary<string, object> inheritableMetadata = null)
        {
            RunId = runId;
            Handlers = handlers;
            InheritableHandlers = inheritableHandlers;
            Metadata = metadata ?? new();
            InheritableMetadata = inheritableMetadata ?? new();
            ParentRunId = parentRunId;
        }
        protected RunManager()
            : this(
                runId: Guid.NewGuid().ToString("N"),
                handlers: new(),
                inheritableHandlers: new())
        {
        }

        /// <summary>
        /// Run when text is received.
        /// </summary>
        /// <param name="text">The received text.</param>
        public async UniTask HandleText(string text)
        {
            foreach (var handler in Handlers)
            {
                try
                {
                    await handler.HandleTextAsync(text, RunId, ParentRunId);
                }
                catch (Exception ex)
                {
                    Debug.Log($"Error in handler {handler.GetType().Name}, HandleText: {ex}");
                }
            }
        }

        /// <summary>
        /// Return a manager that doesn't perform any operations.
        /// </summary>
        public static T GetNoopManager<T>() where T : IChainRunner<T>, new()
        {
            return new T();
        }
    }

    public interface IChainRunner<TThis> where TThis : IChainRunner<TThis>, new() { }
    /// <summary>
    /// Sync Parent Run Manager.
    /// </summary>
    public class ParentRunManager : RunManager
    {
        public ParentRunManager()
        {

        }

        public ParentRunManager(
            string runId,
            List<CallbackHandler> handlers,
            List<CallbackHandler> inheritableHandlers,
            string parentRunId = null,
            Dictionary<string, object> metadata = null,
            Dictionary<string, object> inheritableMetadata = null)
            : base(runId, handlers, inheritableHandlers, parentRunId, metadata, inheritableMetadata)
        {
        }

        /// <summary>
        /// Get a child callback manager.
        /// </summary>
        /// <returns>The child callback manager.</returns>
        public ChainCallback GetChild()
        {
            var manager = new ChainCallback(new(), parentRunId: RunId);
            manager.SetHandlers(InheritableHandlers);
            manager.AddMetadata(InheritableMetadata);
            return manager;
        }
    }
}