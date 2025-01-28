using Unity.Collections;
using Unity.Sentis;
namespace UniChat.NLP
{
    /// <summary>
    /// Filter id with top sentence similarity
    /// </summary>
    public class TopSimilarityFilter : IFilter
    {
        /// <summary>
        /// Input threshold to return comparison result above this value
        /// </summary>
        public float inputThreshold;
        public TopSimilarityFilter() { }
        public TopSimilarityFilter(float inputThreshold)
        {
            this.inputThreshold = inputThreshold;
        }
        public bool Filter(Ops ops, TensorFloat scoreTensor, ref NativeArray<int> ids, ref NativeArray<float> scores)
        {
            ids.Resize(1);
            scores.Resize(1);
            TensorInt scoreIndex = ops.ArgMax(scoreTensor, 1, true);
            scoreIndex.MakeReadable();
            ids[0] = scoreIndex[0];
            scoreTensor.MakeReadable();
            scores[0] = scoreTensor[scoreIndex[0]];
            scoreIndex.Dispose();
            return scores[0] >= inputThreshold;
        }
    }
}