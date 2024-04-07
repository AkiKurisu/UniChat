using System.Text;
using Unity.Sentis;
namespace Kurisu.UniChat
{
    public static class TensorExtensions
    {
        public static void MakeReadable(this TensorFloat[] tensorFloats)
        {
            for (int i = 0; i < tensorFloats.Length; ++i)
            {
                tensorFloats[i].MakeReadable();
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
        /// <param name="InputSequence"></param>
        /// <param name="ComparisonSequences"></param>
        /// <returns></returns>
        public static TensorFloat CosineSimilarity(this Ops ops, TensorFloat InputSequence, TensorFloat ComparisonSequences)
        {
            return ops.MatMul2D(InputSequence, ComparisonSequences, false, true);
        }
    }
}