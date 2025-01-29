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
        
        public abstract void Delete(uint key);
    }
    
    /// <summary>
    /// Cache text use a map in memory
    /// </summary>
    public class TextMemoryCache : TextCache
    {
        private readonly List<uint> _keys = new();
        
        private readonly List<string> _values = new();
        
        private readonly string _infoPath;
        
        public TextMemoryCache(string folderPath)
        {
            _infoPath = Path.Combine(folderPath, "text_memory_cache.txt");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            if (File.Exists(_infoPath))
            {
                LoadFile(_infoPath);
            }
        }
        
        public override void Save(uint hash, IReadOnlyList<string> values)
        {
            for (int i = 0; i < values.Count; ++i)
                _keys.Add(hash);
            _values.AddRange(values);
            SaveFile(_infoPath);
        }
        
        public override void Save(uint hash, string value)
        {
            _keys.Add(hash);
            _values.Add(value);
            SaveFile(_infoPath);
        }
        
        public override UniTask<string[]> Load(uint hash)
        {
            int start = _keys.IndexOf(hash);
            int end = _keys.LastIndexOf(hash);
            if (start == end) return UniTask.FromResult(new string[1] { _values[start] });
            return UniTask.FromResult(_values.Skip(start).Take(end - start).ToArray());
        }
        
        public void SaveFile(string infoPath)
        {
            using var stream = new FileStream(infoPath, FileMode.Create, FileAccess.Write);
            using var sw = new StreamWriter(stream);
            SaveFile(sw);
        }
        
        public void SaveFile(StreamWriter sw)
        {
            for (int i = 0; i < _keys.Count; ++i)
            {
                sw.Write(_keys[i]);
                sw.Write('|');
                sw.WriteLine(_values[i]);
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
                    _keys.Add(uint.Parse(file));
                    _values.Add(segment);
                }
            }
        }
        
        public override bool Contains(uint key)
        {
            return _keys.Contains(key);
        }

        public override void Delete(uint key)
        {
            int index = _keys.IndexOf(key);
            if (index < 0) return;
            _keys.RemoveAt(index);
            _values.RemoveAt(index);
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