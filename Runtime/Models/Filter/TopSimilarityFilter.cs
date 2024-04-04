using System.Collections.Generic;
using Unity.Collections;
using Unity.Sentis;
namespace Kurisu.UniChat
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
        /// <summary>
        /// Output threshold to clip ports above this value
        /// </summary>
        public float outputThreshold;
        public TopSimilarityFilter() { }
        public TopSimilarityFilter(float inputThreshold, float outputThreshold)
        {
            this.inputThreshold = inputThreshold;
            this.outputThreshold = outputThreshold;
        }
        public bool Filter(Ops ops, IReadOnlyList<TensorFloat> inputTensors, IEmbeddingDataBase db, ref NativeArray<int> ids, ref NativeArray<float> scores)
        {
            ids.Resize(1);
            scores.Resize(1);
            int count = db.Count;
            if (count == 0) return false;
            //Allocate tensor
            TensorFloat[] tensors = db.AllocateTensors();
            //Input similarity
            TensorFloat inputScores = ops.CosineSimilarity(inputTensors[0], tensors[0]);
            //Output similarity
            TensorFloat outputScores = ops.CosineSimilarity(inputTensors[1], tensors[1]);
            //Clipping
            inputScores = ops.Clip(inputScores, outputScores, outputThreshold);
            //Final indices
            TensorInt scoreIndex = ops.ArgMax(inputScores, 1, true);
            scoreIndex.MakeReadable();
            ids[0] = scoreIndex[0];
            inputScores.MakeReadable();
            scores[0] = inputScores[scoreIndex[0]];
            scoreIndex.Dispose();
            inputScores.Dispose();
            outputScores.Dispose();
            tensors.Dispose();
            return scores[0] >= inputThreshold;
        }
    }
}