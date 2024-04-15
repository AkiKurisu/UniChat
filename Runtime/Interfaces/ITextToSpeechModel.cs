using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
namespace Kurisu.UniChat
{
    public interface ITextToSpeechModel
    {
        UniTask<AudioClip> GenerateSpeechAsync(
            string prompt,
            TextToSpeechSettings settings = null,
            CancellationToken cancellationToken = default);
    }
    public class TextToSpeechSettings { }
}