using System;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;
namespace Kurisu.UniChat.Chains
{
    /// <summary>
    /// Speech to text chain
    /// </summary>
    public class STTChain : StackableChain
    {
        private readonly ISpeechToTextModel _model;
        private readonly SpeechToTextSettings _settings;
        private readonly string _inputKey;
        private readonly string _outputKey;
        public STTChain(
            ISpeechToTextModel model,
            SpeechToTextSettings settings = null,
            string inputKey = "audio",
            string outputKey = "text")
        {
            _model = model;
            _settings = settings;
            _inputKey = inputKey;
            _outputKey = outputKey;
            InputKeys = new[] { inputKey };
            OutputKeys = new[] { outputKey };

        }

        protected override async UniTask<IChainValues> InternalCall(IChainValues values)
        {
            values = values ?? throw new ArgumentNullException(nameof(values));

            SpeechToTextRequest request = (SpeechToTextRequest)values.Value[_inputKey];
            Assert.IsNotNull(request);
            string text = await _model.TranscribeAsync((SpeechToTextRequest)values.Value[_inputKey], _settings);
            values.Value[_outputKey] = text;
            return values;
        }
    }
}