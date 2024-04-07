using Unity.Collections;
using Unity.Sentis;
using UnityEngine;
namespace Kurisu.UniChat
{
    public class TopKFilter : IFilter
    {
        /// <summary>
        /// Input threshold to return comparison result above this value
        /// </summary>
        public float inputThreshold;
        /// <summary>
        /// Filter count
        /// </summary>
        public int topK = 5;
        public TopKFilter() { }
        public TopKFilter(float inputThreshold, int topK = 5)
        {
            this.inputThreshold = inputThreshold;
            this.topK = topK;
        }
        public bool Filter(Ops ops, TensorFloat scoredTensors, ref NativeArray<int> ids, ref NativeArray<float> scores)
        {
            int count = scoredTensors.shape[1];
            int topKNum = Mathf.Min(count, topK);
            ids.Resize(topKNum);
            scores.Resize(topKNum);
            //TopK indices
            var topKTensors = ops.TopK(scoredTensors, topK, 1, true, true);
            topKTensors[0].MakeReadable();
            topKTensors[1].MakeReadable();
            for (int i = 0; i < topKNum; ++i)
            {
                scores[i] = (topKTensors[0] as TensorFloat)[i];
                ids[i] = (topKTensors[1] as TensorInt)[i];
            }
            topKTensors[0].Dispose();
            topKTensors[1].Dispose();
            return scores[^1] >= inputThreshold;
        }
    }
}
