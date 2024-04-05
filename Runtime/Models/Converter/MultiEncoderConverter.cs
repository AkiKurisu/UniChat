using System.Collections.Generic;
using Kurisu.NGDS.NLP;
using Unity.Sentis;
using UnityEngine.Pool;
namespace Kurisu.UniChat
{
    public class MultiEncoderConverter : ITensorConverter
    {
        private readonly IEncoder[] encoders;
        private readonly TensorFloat[] inputTensors;
        public MultiEncoderConverter(params IEncoder[] encoders)
        {
            this.encoders = encoders;
            inputTensors = new TensorFloat[encoders.Length];
        }
        public TensorFloat[] Convert(Ops ops, IReadOnlyList<string> input)
        {
            for (int i = 0; i < encoders.Length; ++i)
            {
                inputTensors[i] = encoders[i].Encode(ops, input);
            }
            return inputTensors;
        }
        public TensorFloat[] Convert(Ops ops, string input)
        {
            var pool = ListPool<string>.Get();
            pool.Add(input);
            try
            {
                return Convert(ops, pool);
            }
            finally
            {
                ListPool<string>.Release(pool);
            }
        }
    }
}