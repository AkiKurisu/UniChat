using System.Collections.Generic;
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
        /// Output threshold to clip ports above this value
        /// </summary>
        public float outputThreshold;
        /// <summary>
        /// Filter count
        /// </summary>
        public int topK = 5;
        public TopKFilter() { }
        public TopKFilter(float inputThreshold, float outputThreshold, int topK = 5)
        {
            this.inputThreshold = inputThreshold;
            this.outputThreshold = outputThreshold;
            this.topK = topK;
        }
        public bool Filter(Ops ops, IReadOnlyList<TensorFloat> inputTensors, IEmbeddingDataBase db, ref NativeArray<int> ids, ref NativeArray<float> scores)
        {
            int count = db.Count;
            if (count == 0) return false;
            int topKNum = Mathf.Min(count, topK);
            ids.Resize(topKNum);
            scores.Resize(topKNum);
            //Allocate tensor
            TensorFloat[] tensors = db.AllocateTensors();
            //Input similarity
            TensorFloat inputScores = ops.CosineSimilarity(inputTensors[0], tensors[0]);
            //Output similarity
            TensorFloat outputScores = ops.CosineSimilarity(inputTensors[1], tensors[1]);
            //Clipping
            inputScores = ops.Clip(inputScores, outputScores, outputThreshold);
            //TopK indices
            var topKTensors = ops.TopK(inputScores, topK, 1, true, true);
            topKTensors[0].MakeReadable();
            topKTensors[1].MakeReadable();
            for (int i = 0; i < topKNum; ++i)
            {
                scores[i] = (topKTensors[0] as TensorFloat)[i];
                ids[i] = (topKTensors[1] as TensorInt)[i];
            }
            topKTensors[0].Dispose();
            topKTensors[1].Dispose();
            inputScores.Dispose();
            outputScores.Dispose();
            tensors.Dispose();
            return scores[^1] >= inputThreshold;
        }
    }
}
