using System.Collections.Generic;
using Unity.Collections;
using Unity.Sentis;
namespace Kurisu.UniChat
{
    public interface IFilter
    {
        /// <summary>
        /// Return true or false based on input tensors and db for implemented comparison logic
        /// </summary>
        /// <param name="ops">Ops</param>
        /// <param name="inputTensors">input tensors</param>
        /// <param name="db">input dataBase</param>
        /// <param name="ids">filter index if has</param>
        /// <param name="scores">score if has</param>
        /// <returns>whether has result</returns>
        bool Filter(Ops ops, IReadOnlyList<TensorFloat> inputTensors, IEmbeddingDataBase db, ref NativeArray<int> ids, ref NativeArray<float> scores);
    }
}