using System.Text;
using UnityEngine;
using Unity.Sentis;
using UnityEngine.Pool;
using Cysharp.Threading.Tasks;
namespace UniChat
{
    public static class TensorExtensions
    {
        /// <summary>
        /// Perform Mean Pooling
        /// </summary>
        /// <param name="ops">Ops on tensor</param>
        /// <param name="attentionMaskTensor">Attention Mask Tensor</param>
        /// <param name="outputTensor">Output Tensor</param>
        /// <returns></returns>
        public static TensorFloat MeanPooling(this Ops ops, Tensor attentionMaskTensor, TensorFloat outputTensor)
        {
            if (attentionMaskTensor == null || outputTensor == null)
            {
                Debug.LogError("AttentionMaskTensor or outputTensor is null.");
            }
            // Create an attention mask and 
            // add a new dimension (to make the mask compatible for element wise multiplication with token embeddings)
            var attentionMaskTensorFloat = ops.Cast(attentionMaskTensor, DataType.Float) as TensorFloat;
            var inputMaskExpanded = attentionMaskTensorFloat.ShallowReshape(attentionMaskTensorFloat.shape.Unsqueeze(-1));
            var inputMaskExpandedFloat = ops.Cast(inputMaskExpanded, DataType.Float) as TensorFloat;

            // Expand to 384 => [2, 6, 384]
            inputMaskExpandedFloat = ops.Expand(inputMaskExpandedFloat, outputTensor.shape);

            // torch.sum(token_embeddings * input_mask_expanded, 1) / torch.clamp(input_mask_expanded.sum(1), min=1e-9)
            var mulResult = ops.Mul(outputTensor, inputMaskExpandedFloat);
            var MeanPooledTensor = ops.ReduceMean(mulResult, new[] { 1 }, false);

            return MeanPooledTensor;
        }


        /// <summary>
        /// L2 Normalization
        /// </summary>
        /// <param name="meanPooledTensor"></param>
        /// <param name="ops">Ops on tensor</param>
        /// <returns></returns>
        public static TensorFloat L2Norm(this Ops ops, TensorFloat meanPooledTensor)
        {
            // L2 NORMALIZATION
            // Compute L2 norm along axis 1 (dim=1)
            TensorFloat l2Norms = ops.ReduceL2(meanPooledTensor, new int[] { 1 }, true);

            // Broadcast the L2 norms to the original shape
            TensorFloat l2NormsBroadcasted = ops.Expand(l2Norms, meanPooledTensor.shape);

            // Divide sentence_embeddings by their L2 norms to achieve normalization
            TensorFloat normalizedEmbeddings = ops.Div(meanPooledTensor, l2NormsBroadcasted);

            return normalizedEmbeddings;
        }
        
        public static void MakeReadable(this TensorFloat[] tensorFloats)
        {
            for (int i = 0; i < tensorFloats.Length; ++i)
            {
                tensorFloats[i].MakeReadable();
            }
        }
        
        public static async UniTask MakeReadableAsync(this TensorFloat[] tensorFloats)
        {
            var pool = ListPool<UniTask>.Get();
            try
            {
                for (int i = 0; i < tensorFloats.Length; ++i)
                {
                    pool.Add(tensorFloats[i].MakeReadableAsync().AsUniTask());
                }
                await UniTask.WhenAll(pool);
            }
            finally
            {
                ListPool<UniTask>.Release(pool);
            }
        }
        
        public static void Dispose(this TensorFloat[] tensorFloats)
        {
            for (int i = 0; i < tensorFloats.Length; ++i)
            {
                tensorFloats[i].Dispose();
            }
        }
        
        /// <summary>
        /// Return tensor[d1,0..shape[1]] as string
        /// </summary>
        /// <param name="tensor"></param>
        /// <param name="d1"></param>
        /// <returns></returns>
        public static string ToString(this TensorFloat tensor, int d1)
        {
            tensor.MakeReadable();
            StringBuilder sb = new();
            sb.Append('(');
            for (int i = 0; i < tensor.shape[1]; ++i)
            {
                if (i != 0)
                    sb.Append(", ");
                sb.Append(tensor[d1, i]);
            }
            sb.Append(')');
            return sb.ToString();
        }
        
        public static string ToString(this TensorInt tensor, int d1)
        {
            tensor.MakeReadable();
            StringBuilder sb = new();
            sb.Append('(');
            for (int i = 0; i < tensor.shape[1]; ++i)
            {
                if (i != 0)
                    sb.Append(", ");
                sb.Append(tensor[d1, i]);
            }
            sb.Append(')');
            return sb.ToString();
        }
        
        /// <summary>
        /// Return tensor[d1,0..shape[1]] as array
        /// </summary>
        /// <param name="tensor"></param>
        /// <param name="d1"></param>
        /// <returns></returns>
        public static float[] ToArray(this TensorFloat tensor, int d1)
        {
            tensor.MakeReadable();
            var array = new float[tensor.shape[1]];
            for (int i = 0; i < tensor.shape[1]; ++i)
            {
                array[i] = tensor[d1, i];
            }
            return array;
        }
        
        /// <summary>
        /// Return tensor[d2,d1,0..shape[1]] as string
        /// </summary>
        /// <param name="tensor"></param>
        /// /// <param name="d2"></param>
        /// <param name="d1"></param>
        /// <returns></returns>
        public static string ToString(this TensorFloat tensor, int d2, int d1)
        {
            StringBuilder sb = new();
            sb.Append('(');
            for (int i = 0; i < tensor.shape[3]; ++i)
            {
                if (i != 0)
                    sb.Append(", ");
                sb.Append(tensor[d2, d1, i]);
            }
            sb.Append(')');
            return sb.ToString();
        }
        
        /// <summary>
        /// Calculate cosine similarity
        /// (A dot B) / ||A|| * ||B||
        /// </summary>
        /// <param name="ops"></param>
        /// <param name="inputSequence"></param>
        /// <param name="comparisonSequences"></param>
        /// <returns></returns>
        public static TensorFloat CosineSimilarity(this Ops ops, TensorFloat inputSequence, TensorFloat comparisonSequences)
        {
            return ops.MatMul2D(inputSequence, comparisonSequences, false, true);
        }
        
        public static TensorFloat Encode(this IEncoder encoder, Ops ops, string input)
        {
            var pool = ListPool<string>.Get();
            pool.Add(input);
            try
            {
                return encoder.Encode(ops, pool);
            }
            finally
            {
                ListPool<string>.Release(pool);
            }
        }
    }
}