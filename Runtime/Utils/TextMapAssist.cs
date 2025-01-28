using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using System.Linq;
namespace UniChat
{
    public abstract class TextCache
    {
        public abstract void Save(uint hash, IReadOnlyList<string> values);
        public abstract void Save(uint hash, string value);
        public abstract UniTask<string[]> Load(uint hash);
        public abstract bool Contains(uint key);
        public abstract void Delate(uint key);
    }
    /// <summary>
    /// Cache text use a map in memory
    /// </summary>
    public class TextMemoryCache : TextCache
    {
        private readonly List<uint> keys = new();
        private readonly List<string> values = new();
        private readonly string infoPath;
        public TextMemoryCache(string folderPath)
        {
            infoPath = Path.Combine(folderPath, "text_memory_cache.txt");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            if (File.Exists(infoPath))
            {
                LoadFile(infoPath);
            }
        }
        public override void Save(uint hash, IReadOnlyList<string> values)
        {
            for (int i = 0; i < values.Count; ++i)
                keys.Add(hash);
            this.values.AddRange(values);
            SaveFile(infoPath);
        }
        public override void Save(uint hash, string value)
        {
            keys.Add(hash);
            values.Add(value);
            SaveFile(infoPath);
        }
        public override UniTask<string[]> Load(uint hash)
        {
            int start = keys.IndexOf(hash);
            int end = keys.LastIndexOf(hash);
            if (start == end) return UniTask.FromResult(new string[1] { values[start] });
            return UniTask.FromResult(values.Skip(start).Take(end - start).ToArray());
        }
        public void SaveFile(string infoPath)
        {
            using var stream = new FileStream(infoPath, FileMode.Create, FileAccess.Write);
            using var sw = new StreamWriter(stream);
            SaveFile(sw);
        }
        public void SaveFile(StreamWriter sw)
        {
            for (int i = 0; i < keys.Count; ++i)
            {
                sw.Write(keys[i]);
                sw.Write('|');
                sw.WriteLine(values[i]);
            }
        }
        public void LoadFile(string infoPath)
        {
            using var stream = new FileStream(infoPath, FileMode.Open, FileAccess.Read);
            using var sr = new StreamReader(stream);
            LoadFile(sr);
        }
        public void LoadFile(StreamReader sr)
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                string[] parts = line.Split('|');
                if (parts.Length == 2)
                {
                    string file = parts[0];
                    string segment = parts[1];
                    keys.Add(uint.Parse(file));
                    values.Add(segment);
                }
            }
        }
        public override bool Contains(uint key)
        {
            return keys.Contains(key);
        }

        public override void Delate(uint key)
        {
            int index = keys.IndexOf(key);
            if (index < 0) return;
            keys.RemoveAt(index);
            values.RemoveAt(index);
        }
        public static TextMemoryCache CreateCache(string modelFolder)
        {
            if (!Directory.Exists(modelFolder))
            {
                Directory.CreateDirectory(modelFolder);
            }
            return new TextMemoryCache(Path.Combine(modelFolder, ".text"));
        }
    }
}