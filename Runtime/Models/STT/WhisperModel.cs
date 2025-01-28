using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Whisper;
using Whisper.Native;
using Whisper.Utils;
namespace UniChat.STT
{
    public class WhisperModel : ISpeechToTextModel
    {
        [Serializable]
        public class WhisperSettings : SpeechToTextSettings
        {
            [Tooltip("Output text language. Use empty or \"auto\" for auto-detection.")]
            public string language = "en";

            [Tooltip("Force output text to English translation. Improves translation quality.")]
            public bool translateToEnglish;

            [Tooltip("Do not use past transcription (if any) as initial prompt for the decoder.")]
            public bool noContext = true;

            [Tooltip("Force single segment output (useful for streaming).")]
            public bool singleSegment;

            [Tooltip("Output tokens with their confidence in each segment.")]
            public bool enableTokens;

            [Tooltip("Initial prompt as a string variable. " +
                     "It should improve transcription quality or guide it to the right direction.")]
            public string initialPrompt;
            [Tooltip("[EXPERIMENTAL] Output timestamps for each token. Need enabled tokens to work.")]
            public bool tokensTimestamps;

            [Tooltip("[EXPERIMENTAL] Speed-up the audio by 2x using Phase Vocoder. " +
                     "These can significantly reduce the quality of the output.")]
            public bool speedUp;

            [Tooltip("[EXPERIMENTAL] Overwrite the audio context size (0 = use default). " +
                     "These can significantly reduce the quality of the output.")]
            public int audioCtx;
            public LogLevel logLevel = LogLevel.Error;
        }
        private readonly WhisperWrapper whisper;
        public WhisperParams Params { get; }
        public WhisperModel(WhisperWrapper whisper, WhisperSamplingStrategy strategy = WhisperSamplingStrategy.WHISPER_SAMPLING_GREEDY)
        {
            this.whisper = whisper;
            Params = WhisperParams.GetDefaultParams(strategy);
        }
        public static async UniTask<WhisperModel> FromPath(string filePath, WhisperSamplingStrategy strategy = WhisperSamplingStrategy.WHISPER_SAMPLING_GREEDY)
        {
            var whisper = await WhisperWrapper.InitFromFileAsync(filePath);
            return new WhisperModel(whisper, strategy);
        }
        public static async UniTask<WhisperModel> FromBytes(byte[] bytes, WhisperSamplingStrategy strategy = WhisperSamplingStrategy.WHISPER_SAMPLING_GREEDY)
        {
            var whisper = await WhisperWrapper.InitFromBufferAsync(bytes);
            return new WhisperModel(whisper, strategy);
        }
        public async UniTask<string> TranscribeAsync(SpeechToTextRequest request, SpeechToTextSettings settings = null, CancellationToken cancellationToken = default)
        {
            if (settings is WhisperSettings whisperSettings)
            {
                Params.Language = whisperSettings.language;
                Params.Translate = whisperSettings.translateToEnglish;
                Params.NoContext = whisperSettings.noContext;
                Params.SingleSegment = whisperSettings.singleSegment;
                Params.SpeedUp = whisperSettings.speedUp;
                Params.AudioCtx = whisperSettings.audioCtx;
                Params.EnableTokens = whisperSettings.enableTokens;
                Params.TokenTimestamps = whisperSettings.tokensTimestamps;
                Params.InitialPrompt = whisperSettings.initialPrompt;
                LogUtils.Level = whisperSettings.logLevel;
            }
            var result = await whisper.GetTextAsync(request.samples, request.frequency, request.channels, Params);
            return result.Result;
        }
    }
}