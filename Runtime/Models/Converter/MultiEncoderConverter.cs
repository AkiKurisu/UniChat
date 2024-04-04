using System.Collections.Generic;
using Kurisu.NGDS.NLP;
using Unity.Sentis;
namespace Kurisu.UniChat
{
    public class MultiEncoderConverter : ITensorConverter
    {
        private readonly List<string> inputs = new();
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
            inputs.Clear();
            inputs.Add(input);
            return Convert(ops, inputs);
        }
    }
}