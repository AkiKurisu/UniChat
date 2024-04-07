using Unity.Collections;
using Unity.Sentis;
namespace Kurisu.UniChat
{
    public interface IFilter
    {
        /// <summary>
        /// Return true or false for implemented filter logic
        /// </summary>
        /// <param name="ops">Ops</param>
        /// <param name="scoreTensor">input scored tensors</param>
        /// <param name="ids">filter index if has</param>
        /// <param name="scores">score if has</param>
        /// <returns>whether has result</returns>
        bool Filter(Ops ops, TensorFloat scoreTensor, ref NativeArray<int> ids, ref NativeArray<float> scores);
    }
}