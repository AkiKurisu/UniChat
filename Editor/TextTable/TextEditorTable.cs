using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using static Kurisu.UniChat.TextEmbeddingTable;
namespace Kurisu.UniChat.Editor.TextTable
{
    public class TextEditorTable : ScriptableObject
    {
        [Serializable]
        public class AudioInfo
        {
            public string infoText;
            public string filePath;
            public string fileName;
        }
        [Serializable]
        public class Entry
        {
            public uint uniqueId;
            [TextArea]
            public string stringValue;
            public bool isEdit;
            public readonly TextEmbeddingEntry internalEntry;
            public AudioInfo[] audioInfos;
            public Entry(TextEmbeddingEntry internalEntry)
            {
                this.internalEntry = internalEntry;
                uniqueId = internalEntry.uniqueId;
                stringValue = internalEntry.stringValue;
            }
            public void Update()
            {
                internalEntry.stringValue = stringValue;
            }
        }
        private readonly List<Entry> lastEntries = new();
        public List<Entry> tableEntries = new();
        private TextEmbeddingTable internalTable;
        private string path;
        private AudioCache audioFileAssist;
        public void Initialize(TextEmbeddingTable internalTable, string path)
        {
            this.path = path;
            this.internalTable = internalTable;
            audioFileAssist = AudioCache.CreateCache(Path.GetDirectoryName(path));
            tableEntries.Clear();
            tableEntries.AddRange(internalTable.tableEntries.Select(x => new Entry(x)
            {
                audioInfos = audioFileAssist.GetPathAndSegments(x.uniqueId)
                                            .Select(x => new AudioInfo()
                                            {
                                                infoText = x.segment,
                                                filePath = x.filePath,
                                                fileName = Path.GetFileNameWithoutExtension(x.filePath)
                                            }).ToArray()
            }));
            lastEntries.Clear();
            lastEntries.AddRange(tableEntries);
        }
        public void Update()
        {
            tableEntries.ForEach(x => x.Update());
            internalTable.tableEntries = tableEntries.Select(x => x.internalEntry).ToList();
            internalTable.Save(path);
            lastEntries.ForEach(x => { if (!tableEntries.Contains(x)) audioFileAssist.Delate(x.uniqueId); });
            lastEntries.Clear();
            lastEntries.AddRange(tableEntries);
        }

        public void Remove(uint uintValue)
        {
            tableEntries.RemoveAll(x => x.uniqueId == uintValue);
        }

        public async UniTask PlayAudio(uint id, int index)
        {
            (AudioClip[] clips, string[] _) = await audioFileAssist.Load(id);
            AudioUtil.PlayClip(clips[index]);
        }
    }
    public static class AudioUtil
    {
        public static readonly string PrefKey = Application.productName + "_NGD_AudioSavePath";
        static readonly Dictionary<string, MethodInfo> methods = new();

        static MethodInfo GetMethod(string methodName, Type[] argTypes)
        {
            if (methods.TryGetValue(methodName, out MethodInfo method)) return method;

            var asm = typeof(AudioImporter).Assembly;
            var audioUtil = asm.GetType("UnityEditor.AudioUtil");
            method = audioUtil.GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.Public,
                null,
                argTypes,
                null);

            if (method != null)
            {
                methods.Add(methodName, method);
            }

            return method;
        }


        public static void PlayClip(AudioClip clip)
        {
            if (!clip) return;
#if UNITY_2020_1_OR_NEWER
            var method = GetMethod("PlayPreviewClip", new Type[] { typeof(AudioClip), typeof(int), typeof(bool) });
            method.Invoke(null, new object[] { clip, 0, false });
#else
            var method = GetMethod("PlayClip", new Type[] { typeof(AudioClip) });
            method.Invoke(null, new object[] { clip });
#endif
        }

        public static void StopClip(AudioClip clip)
        {
#if UNITY_2020_1_OR_NEWER
            var method = GetMethod("StopAllPreviewClips", new Type[] { });
            method.Invoke(null, new object[] { });
#else
            if (!clip) return;
            var method = GetMethod("StopClip", new Type[] { typeof(AudioClip) });
            method.Invoke(null, new object[] { clip });
#endif
        }
    }
}