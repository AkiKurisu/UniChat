using System.Collections.Generic;
using Unity.Sentis;
namespace UniChat
{
    /// <summary>
    /// Classifier returns label from input, and can act as encoder
    /// </summary>
    public interface IClassifier : IEncoder
    {
        (TensorFloat, TensorInt) Classify(Ops ops, IReadOnlyList<string> inputs);
    }
}