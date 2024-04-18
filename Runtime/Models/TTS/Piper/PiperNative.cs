using System;
using System.Runtime.InteropServices;
namespace Piper.Native
{
    /// <summary>
    /// Code from https://github.com/Macoron/piper.unity under GPL-3.0 license
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct PiperProcessedSentenceNative
    {
        public long* phonemesIds;
        public UIntPtr length;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct PiperProcessedTextNative
    {
        public PiperProcessedSentenceNative* sentences;
        public UIntPtr sentencesCount;
    }

    public static unsafe class PiperNative
    {
        private const string LibraryName = "piper_phonemize";

        [DllImport(LibraryName)]
        public static extern int init_piper(string dataPath);

        [DllImport(LibraryName)]
        public static extern int process_text(string text, string voice);

        [DllImport(LibraryName)]
        public static extern PiperProcessedTextNative get_processed_text();

        [DllImport(LibraryName)]
        public static extern void free_piper();

    }
}

