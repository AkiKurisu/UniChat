using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Piper;
using Unity.Sentis;
using UnityEngine;
namespace UniChat.TTS
{
    /// <summary>
    /// Modified from https://github.com/Macoron/piper.unity under GPL-3.0 license
    /// </summary>
    public class PiperModel : ITextToSpeechModel, IDisposable
    {
        public class PiperSettings : TextToSpeechSettings
        {
            public string Voice { get; set; } = "en_us";
            public int SampleRate { get; set; } = 22050;
        }
        
        private static readonly PiperSettings DefaultSettings = new();

        private readonly IWorker _worker;
        
        public PiperModel(Model model, string espeakPath, BackendType backendType)
        {
            PiperWrapper.InitPiper(espeakPath);
            _worker = WorkerFactory.CreateWorker(backendType, model);
        }

        public async UniTask<AudioClip> GenerateSpeechAsync(string prompt, TextToSpeechSettings settings = null, CancellationToken cancellationToken = default)
        {
            var setting = settings as PiperSettings;
            setting ??= DefaultSettings;
            var phonemes = PiperWrapper.ProcessText(prompt, setting.Voice);
            var inputLengthsShape = new TensorShape(1);
            var scalesShape = new TensorShape(3);
            using var scalesTensor = new TensorFloat(scalesShape, new float[] { 0.667f, 1f, 0.8f });
            var audioBuffer = new List<float>();
            for (int i = 0; i < phonemes.Sentences.Length; i++)
            {
                var sentence = phonemes.Sentences[i];

                var inputPhonemes = sentence.PhonemesIds;
                var inputShape = new TensorShape(1, inputPhonemes.Length);
                using var inputTensor = new TensorInt(inputShape, inputPhonemes);
                using var inputLengthsTensor = new TensorInt(inputLengthsShape, new int[] { inputPhonemes.Length });

                var input = new Dictionary<string, Tensor>
                {
                    { "input", inputTensor },
                    { "input_lengths", inputLengthsTensor },
                    { "scales", scalesTensor }
                };

                _worker.Execute(input);

                using var outputTensor = _worker.PeekOutput() as TensorFloat;
                await outputTensor.MakeReadableAsync();

                var output = outputTensor.ToReadOnlyArray();
                audioBuffer.AddRange(output);
            }
            var audioClip = AudioClip.Create("piper_tts", audioBuffer.Count, 1, setting.SampleRate, false);
            audioClip.SetData(audioBuffer.ToArray(), 0);
            return audioClip;
        }

        public void Dispose()
        {
            PiperWrapper.FreePiper();
            _worker.Dispose();
        }
    }
}
