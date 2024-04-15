using System.Collections.Generic;
using Cysharp.Threading.Tasks;
namespace Kurisu.UniChat
{
    public interface IChain
    {
        IReadOnlyList<string> InputKeys { get; }
        IReadOnlyList<string> OutputKeys { get; }
        UniTask<string> Run(string input);
        UniTask<string> Run(Dictionary<string, object> input, ICallbacks callbacks = null);
        UniTask<IChainValues> CallAsync(IChainValues values, ICallbacks callbacks, IReadOnlyList<string> tags = null, IReadOnlyDictionary<string, object> metadata = null);
    }
    public interface IChainValues
    {
        public Dictionary<string, object> Value { get; }
    }
    public interface ICallbacks { }
    public interface IChainInputs
    {
        /// <summary>
        /// [Optional]
        /// </summary>
        /// <value></value>
        public ICallbacks Callbacks { get; set; }
        /// <summary>
        /// [Optional]
        /// </summary>
        /// <value></value>
        public List<string> Tags { get; set; }
        /// <summary>
        /// [Optional]
        /// </summary>
        /// <value></value>
        public Dictionary<string, object> Metadata { get; set; }
    }
}