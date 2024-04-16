using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
namespace Kurisu.UniChat.Chains
{
    public class TTSChain : StackableChain
    {
        private readonly ITextToSpeechModel _model;
        private readonly TextToSpeechSettings _settings;
        private readonly string _inputKey;
        private readonly string _outputKey;
        private bool useCache;
        private bool verbose;
        public AudioCache audioFileAssist;
        public TTSChain(
            ITextToSpeechModel model,
            TextToSpeechSettings settings = null,
            string inputKey = "text",
            string outputKey = "audio")
        {
            InputKeys = new[] { inputKey };
            OutputKeys = new[] { outputKey };
            _model = model;
            _settings = settings;
            _inputKey = inputKey;
            _outputKey = outputKey;

        }
        protected override async UniTask<IChainValues> InternalCall(IChainValues values)
        {
            values = values ?? throw new ArgumentNullException(nameof(values));
            var input = values.Value[_inputKey];
            if (input is string stringValue)
            {
                return await GenerateAudios(values, stringValue);
            }
            if (input is IReadOnlyList<string> segments)
            {
                return await GenerateAudiosBatch(values, segments);
            }
            throw new ArgumentException(nameof(input));
        }
        private async UniTask<IChainValues> GenerateAudios(IChainValues values, string segment)
        {
            uint hash = XXHash.CalculateHash(segment);
            AudioClip audioClip;
            if (useCache && audioFileAssist.Contains(hash))
            {
                if (verbose) Debug.Log($"Load audio from cache {hash}");
                (AudioClip[] audioClips, _) = await audioFileAssist.Load(hash);
                audioClip = audioClips[0];
            }
            else
            {
                //Batch
                audioClip = await _model.GenerateSpeechAsync(segment, _settings);
                if (useCache)
                {
                    if (verbose) Debug.Log($"Save audio to cache {hash}");
                    audioFileAssist.Save(hash, audioClip, segment);
                }
            }
            values.Value[_outputKey] = audioClip;
            return values;
        }
        private async UniTask<IChainValues> GenerateAudiosBatch(IChainValues values, IReadOnlyList<string> segments)
        {
            uint hash = XXHash.CalculateHash(segments[0]);
            AudioClip[] audioClips;
            if (useCache && audioFileAssist.Contains(hash))
            {
                if (verbose) Debug.Log($"Load audio from cache {hash}");
                (audioClips, _) = await audioFileAssist.Load(hash);
            }
            else
            {
                //Batch
                audioClips = await UniTask.WhenAll(segments.Select(x => _model.GenerateSpeechAsync(x, _settings)));
                if (useCache)
                {
                    if (verbose) Debug.Log($"Save audio to cache {hash}");
                    audioFileAssist.Save(hash, audioClips, segments);
                }
            }
            values.Value[_outputKey] = audioClips;
            return values;
        }
        public TTSChain UseCache(AudioCache audioFileAssist)
        {
            this.audioFileAssist = audioFileAssist;
            useCache = audioFileAssist != null;
            return this;
        }
        public TTSChain Verbose(bool verbose)
        {
            this.verbose = verbose;
            return this;
        }
    }
}