using System.Collections.Generic;
using Unity.Sentis;
namespace Kurisu.UniChat.NLP
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