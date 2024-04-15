using System.Collections.Generic;
using Unity.Sentis;
namespace Kurisu.UniChat
{
    public interface ITensorConverter
    {
        TensorFloat[] Convert(Ops ops, IReadOnlyList<string> inputs);
    }
}