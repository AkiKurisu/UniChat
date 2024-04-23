using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if WHISPER_INSTALL
using Whisper.Utils;
#endif
namespace Kurisu.UniChat
{
    public interface ISpeechToTextModel
    {
        /// <summary>
        /// Transcribes audio to text.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="settings"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public UniTask<string> TranscribeAsync(
            SpeechToTextRequest request,
            SpeechToTextSettings settings = default,
            CancellationToken cancellationToken = default);
    }
    public class SpeechToTextRequest
    {
        public float[] samples;
        public int frequency;
        public int channels;
#if WHISPER_INSTALL

        public static implicit operator SpeechToTextRequest(AudioChunk audioChunk)
        {
            return new SpeechToTextRequest()
            {
                samples = audioChunk.Data,
                frequency = audioChunk.Frequency,
                channels = audioChunk.Channels,
            };
        }
#endif
        public static implicit operator SpeechToTextRequest(AudioClip audioClip)
        {
            var samples = new float[audioClip.samples * audioClip.channels];
            if (!audioClip.GetData(samples, 0))
            {
                Debug.LogError($"Failed to get audio data from clip {audioClip.name}!");
                return null;
            }
            return new SpeechToTextRequest()
            {
                samples = samples,
                frequency = audioClip.frequency,
                channels = audioClip.channels,
            };
        }
    }
    public class SpeechToTextSettings { }
}