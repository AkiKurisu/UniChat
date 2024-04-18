using Piper.Native;
using System.IO;
using UnityEngine;
namespace Piper
{
    /// <summary>
    /// Code from https://github.com/Macoron/piper.unity under GPL-3.0 license
    /// </summary>
    public readonly struct PiperProcessedSentence
    {
        public readonly int[] PhonemesIds;

        public unsafe PiperProcessedSentence(PiperProcessedSentenceNative native)
        {
            var len = (uint)native.length;
            PhonemesIds = new int[len];
            for (var i = 0; i < len; i++)
            {
                PhonemesIds[i] = (int)native.phonemesIds[i];
            }
        }
    };


    public class PiperProcessedText
    {
        public readonly PiperProcessedSentence[] Sentences;

        public unsafe PiperProcessedText(PiperProcessedTextNative native)
        {
            var len = (uint)native.sentencesCount;
            Sentences = new PiperProcessedSentence[len];
            for (var i = 0; i < len; i++)
            {
                var nativeSentence = native.sentences[i];

                Sentences[i] = new PiperProcessedSentence(nativeSentence);
            }
        }
    }

    public static class PiperWrapper
    {
        public static bool InitPiper(string datapath)
        {
            if (!Directory.Exists(datapath))
            {
                Debug.LogError($"Provided espeak data path \"{datapath}\" doesn't exist!");
                return false;
            }

            var code = PiperNative.init_piper(datapath);
            if (code < 0)
            {
                Debug.LogError($"Failed to init Piper with code: {code}");
                return false;
            }

            return true;
        }

        public static PiperProcessedText ProcessText(string text, string voice)
        {
            var code = PiperNative.process_text(text, voice);
            if (code < 0)
            {
                Debug.LogError($"Failed to get phonemes with code: {code}");
                return null;
            }

            var nativePhonemes = PiperNative.get_processed_text();
            return new PiperProcessedText(nativePhonemes);
        }

        public static void FreePiper()
        {
            PiperNative.free_piper();
        }
    }
}