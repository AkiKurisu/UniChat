using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UniChat.Editor.ChatModel
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
            
            public readonly TextEmbeddingTable.TextEmbeddingEntry InternalEntry;
            
            public AudioInfo[] audioInfos;
            
            public Entry(TextEmbeddingTable.TextEmbeddingEntry internalEntry)
            {
                InternalEntry = internalEntry;
                uniqueId = internalEntry.uniqueId;
                stringValue = internalEntry.stringValue;
            }
            
            public void Update()
            {
                InternalEntry.stringValue = stringValue;
            }
        }
        
        private readonly List<Entry> _lastEntries = new();
        
        public List<Entry> tableEntries = new();
        
        private TextEmbeddingTable _internalTable;
        
        private string _path;
        
        private AudioCache _audioFileAssist;
        
        public void Initialize(TextEmbeddingTable internalTable, string path)
        {
            _path = path;
            _internalTable = internalTable;
            _audioFileAssist = AudioCache.CreateCache(Path.GetDirectoryName(path));
            tableEntries.Clear();
            tableEntries.AddRange(internalTable.tableEntries.Select(x => new Entry(x)
            {
                audioInfos = _audioFileAssist.GetPathAndSegments(x.uniqueId)
                                            .Select(x => new AudioInfo
                                            {
                                                infoText = x.segment,
                                                filePath = x.filePath,
                                                fileName = Path.GetFileNameWithoutExtension(x.filePath)
                                            }).ToArray()
            }));
            _lastEntries.Clear();
            _lastEntries.AddRange(tableEntries);
        }
        
        public void Update()
        {
            tableEntries.ForEach(x => x.Update());
            _internalTable.tableEntries = tableEntries.Select(x => x.InternalEntry).ToList();
            File.Move(_path, Path.Combine(Path.GetDirectoryName(_path)!, $"backup_{Path.GetFileName(_path)}"));
            _internalTable.Save(_path);
            _lastEntries.ForEach(x => { if (!tableEntries.Contains(x)) _audioFileAssist.Delete(x.uniqueId); });
            _lastEntries.Clear();
            _lastEntries.AddRange(tableEntries);
        }

        public void Remove(uint uintValue)
        {
            tableEntries.RemoveAll(x => x.uniqueId == uintValue);
        }

        public async UniTask PlayAudio(uint id, int index)
        {
            (AudioClip[] clips, string[] _) = await _audioFileAssist.Load(id);
            AudioUtil.PlayClip(clips[index]);
        }
    }
    
    public static class AudioUtil
    {
        private static readonly Dictionary<string, MethodInfo> Methods = new();

        static MethodInfo GetMethod(string methodName, Type[] argTypes)
        {
            if (Methods.TryGetValue(methodName, out MethodInfo method)) return method;

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
                Methods.Add(methodName, method);
            }

            return method;
        }


        public static void PlayClip(AudioClip clip)
        {
            if (!clip) return;
#if UNITY_2020_1_OR_NEWER
            var method = GetMethod("PlayPreviewClip", new[] { typeof(AudioClip), typeof(int), typeof(bool) });
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