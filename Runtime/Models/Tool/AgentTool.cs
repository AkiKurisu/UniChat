using System;
using System.Threading;
using Cysharp.Threading.Tasks;
namespace Kurisu.UniChat.Tools
{
    public abstract class AgentTool
    {
        protected AgentTool(
            string name,
            string description = null)
        {
            Name = name;
            Description = description ?? string.Empty;
        }
        public string Name { get; set; }
        public string Description { get; set; }
        public abstract UniTask<string> ExecuteTool(string input, CancellationToken token = default);
    }
    public class DynamicConstructedTool : AgentTool
    {
        private readonly Func<string, UniTask<string>> _func;
        public DynamicConstructedTool(string name, string description, Func<string, UniTask<string>> func) : base(name, description)
        {
            _func = func;
        }
        public override UniTask<string> ExecuteTool(string input, CancellationToken cancellationToken = default)
        {
            return _func(input);
        }
    }
}
