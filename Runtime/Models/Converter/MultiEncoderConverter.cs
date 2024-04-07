using System.Collections.Generic;
using Kurisu.NGDS.NLP;
using Unity.Sentis;
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
    }
}