using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static Kurisu.UniChat.TextEmbeddingTable;
namespace Kurisu.UniChat.Editor
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
            public void Delate()
            {
                foreach (var info in audioInfos)
                {
                    if (File.Exists(info.fileName)) File.Delete(info.filePath);
                }
            }
        }
        private readonly List<Entry> lastEntries = new();
        public List<Entry> tableEntries = new();
        private TextEmbeddingTable internalTable;
        private string path;
        private AudioFileAssist audioFileAssist;
        public void Initialize(TextEmbeddingTable internalTable, string path)
        {
            this.path = path;
            this.internalTable = internalTable;
            audioFileAssist = AudioFileAssist.CreateAssist(Path.GetDirectoryName(path));
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
            lastEntries.ForEach(x => { if (!tableEntries.Contains(x)) x.Delate(); });
            lastEntries.Clear();
            lastEntries.AddRange(tableEntries);
        }

        public void Remove(uint uintValue)
        {
            tableEntries.RemoveAll(x => x.uniqueId == uintValue);
        }
        public void Delate(uint uintValue)
        {
            tableEntries.FirstOrDefault(x => x.uniqueId == uintValue).Delate();
        }
    }
}