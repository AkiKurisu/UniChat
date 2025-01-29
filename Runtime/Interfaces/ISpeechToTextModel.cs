using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Whisper.Utils;
namespace UniChat
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
        public float[] Samples;
        
        public int Frequency;
        
        public int Channels;

        public static implicit operator SpeechToTextRequest(AudioChunk audioChunk)
        {
            return new SpeechToTextRequest()
            {
                Samples = audioChunk.Data,
                Frequency = audioChunk.Frequency,
                Channels = audioChunk.Channels,
            };
        }
        
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
                Samples = samples,
                Frequency = audioClip.frequency,
                Channels = audioClip.channels,
            };
        }
    }
    
    public class SpeechToTextSettings { }
}