using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
namespace Kurisu.UniChat
{
    public class TextEmbeddingTable : IEmbeddingTable, ISerializable, IPersistHandlerFactory<string>
    {
        public struct PersistHandler : IPersistEmbeddingValue<string, string>, IPersistEmbeddingValue<string>
        {
            public readonly bool Persist(uint hash, string value, Embedding embedding, out IEmbeddingEntry<string> entry)
            {
                entry = new TextEmbeddingEntry() { uniqueId = hash, stringValue = value, embedding = embedding };
                return true;
            }

            public readonly bool Persist(uint hash, string value, Embedding embedding, out IEmbeddingEntry entry)
            {
                bool result = Persist(hash, value, embedding, out IEmbeddingEntry<string> stringEntry);
                entry = stringEntry;
                return result;
            }
        }
        public int Count => tableEntries.Count;

        public IEmbeddingEntry this[int index] => tableEntries[index];

        public List<TextEmbeddingEntry> tableEntries = new();
        public TextEmbeddingTable() { }
        public TextEmbeddingTable(string filePath)
        {
            Load(filePath);
        }
        public bool TryGetEntry(uint uniqueId, out IEmbeddingEntry embeddingEntry)
        {
            foreach (var entry in tableEntries)
            {
                if (entry.uniqueId == uniqueId)
                {
                    embeddingEntry = entry;
                    return true;
                }
            }
            embeddingEntry = null;
            return false;
        }
        public bool AddEntry(IEmbeddingEntry embeddingEntry)
        {
            if (TryGetEntry(embeddingEntry.Hash, out _))
                return false;
            if (embeddingEntry is not IEmbeddingEntry<string> stringEntry)
                return false;
            if (stringEntry is TextEmbeddingEntry textEmbeddingEntry)
            {
                tableEntries.Add(textEmbeddingEntry);
            }
            else
            {
                tableEntries.Add(new TextEmbeddingEntry()
                {
                    uniqueId = stringEntry.Hash,
                    stringValue = stringEntry.TValue,
                    embedding = stringEntry.Embedding.Clone()
                });
            }
            return true;
        }
        public void Cleanup()
        {
            tableEntries.Clear();
        }

        public void Save(string filePath)
        {
            using var bw = new BinaryWriter(new FileStream(filePath, FileMode.Create));
            Save(bw);
        }
        public void Save(BinaryWriter bw)
        {
            bw.Write(tableEntries.Count);
            foreach (var entry in tableEntries)
            {
                bw.Write(entry.uniqueId);
                bw.Write(entry.stringValue);
                entry.embedding.Save(bw);
            }
        }
        public void Load(string filePath)
        {
            using var br = new BinaryReader(new FileStream(filePath, FileMode.Open));
            Load(br);
        }
        public void Load(BinaryReader br)
        {
            int count = br.ReadInt32();
            for (int i = 0; i < count; ++i)
            {
                var entry = new TextEmbeddingEntry
                {
                    uniqueId = br.ReadUInt32(),
                    stringValue = br.ReadString(),
                    embedding = new()
                };
                entry.embedding.Load(br);
                tableEntries.Add(entry);
            }
        }

        public IPersistEmbeddingValue<string> CreatePersistHandler()
        {
            return new PersistHandler();
        }

        public IEnumerator<IEmbeddingEntry> GetEnumerator()
        {
            return tableEntries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return tableEntries.GetEnumerator();
        }

        [Serializable]
        public class TextEmbeddingEntry : IEmbeddingEntry<string>
        {
            /// <summary>
            /// This field defines entry's uniqueId
            /// </summary>
            public uint uniqueId;
            /// <summary>
            /// This field defines actual string content
            /// </summary>
            public string stringValue;
            public Embedding embedding;
            public uint Hash => uniqueId;
            public string TValue => stringValue;
            public Embedding Embedding => embedding;
            public object Value => stringValue;
        }
    }

}