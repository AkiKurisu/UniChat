using System.Collections.Generic;
using Unity.Sentis;
namespace Kurisu.UniChat
{
    /// <summary>
    /// Encoder returns embedding vector from inputs
    /// </summary>
    public interface IEncoder
    {
        TensorFloat Encode(Ops ops, IReadOnlyList<string> input);
    }
}