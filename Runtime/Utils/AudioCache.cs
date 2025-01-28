using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
namespace UniChat
{
    public class AudioCache
    {
        private readonly string _folderPath;
        
        private readonly List<string> _files = new();
        
        private readonly List<string> _segments = new();
        
        private readonly string _infoPath;
        
        public AudioCache(string folderPath)
        {
            _folderPath = folderPath;
            _infoPath = Path.Combine(folderPath, "info.txt");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            if (File.Exists(_infoPath))
            {
                LoadFile();
            }
        }
        
        public void Save(uint sourceHash, AudioClip[] audioClips, IReadOnlyList<string> segments)
        {
            for (int i = 0; i < audioClips.Length; ++i)
            {
                string fileName = $"{sourceHash}-{i:D2}";
                _files.Add(fileName);
                WavUtil.Save(Path.Combine(_folderPath, $"{fileName}.wav"), audioClips[i]);
            }
            this._segments.AddRange(segments);
            SaveFile();
        }
        
        public void Save(uint sourceHash, AudioClip audioClip, string segment)
        {
            string fileName = sourceHash.ToString();
            _files.Add(fileName);
            WavUtil.Save(Path.Combine(_folderPath, $"{fileName}.wav"), audioClip);
            _segments.Add(segment);
            SaveFile();
        }
        
        public void SaveFile()
        {
            using var stream = new FileStream(_infoPath, FileMode.Create, FileAccess.Write);
            using var sw = new StreamWriter(stream);
            SaveFile(sw);
        }
        
        public void SaveFile(StreamWriter sw)
        {
            for (int i = 0; i < _files.Count; ++i)
            {
                sw.Write(_files[i]);
                sw.Write('|');
                sw.WriteLine(_segments[i]);
            }
        }
        
        public void LoadFile()
        {
            using var stream = new FileStream(_infoPath, FileMode.Open, FileAccess.Read);
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
                    _files.Add(file);
                    _segments.Add(segment);
                }
            }
        }
        
        public bool Contains(uint sourceHash)
        {
            foreach (var file in _files)
            {
                if (file.Contains(sourceHash.ToString())) return true;
            }
            return false;
        }
        
        public string[] GetSegments(uint sourceHash)
        {
            return _files.Where(x => x.Contains(sourceHash.ToString())).Select(x => _segments[_files.IndexOf(x)]).ToArray();
        }
        
        public IEnumerable<(string filePath, string segment)> GetPathAndSegments(uint sourceHash)
        {
            return _files.Where(x => x.Contains(sourceHash.ToString()))
                        .Select(x => (Path.Combine(_folderPath, $"{x}.wav"), _segments[_files.IndexOf(x)]));
        }
        
        public void CopyFrom(AudioCache audioCache)
        {
            audioCache._files.ForEach(f => File.Copy(Path.Combine(audioCache._folderPath, $"{f}.wav"), Path.Combine(_folderPath, $"{f}.wav")));
            _files.AddRange(audioCache._files);
            _segments.AddRange(audioCache._segments);
        }
        
        public void Delete(uint sourceHash)
        {
            for (int i = _segments.Count - 1; i >= 0; i--)
            {
                if (!_files[i].Contains(sourceHash.ToString())) continue;
                if (File.Exists(_files[i])) File.Delete(_files[i]);
                _segments.RemoveAt(i);
                _files.RemoveAt(i);
            }
        }
        
        public async UniTask<(AudioClip[] clips, string[] segments)> Load(uint sourceHash)
        {
            var pairs = GetPathAndSegments(sourceHash);
            var requests = pairs.Select(x => UnityWebRequestMultimedia.GetAudioClip(
                                            new Uri(x.filePath),
                                            AudioType.WAV
                                        )).ToArray();
            var texts = pairs.Select(x => x.segment).ToArray();
            try
            {
                await UniTask.WhenAll(requests.Select(x => x.SendWebRequest().ToUniTask()));
                var clips = requests.Select(x => DownloadHandlerAudioClip.GetContent(x)).ToArray();
                return (clips, texts);
            }
            catch (UnityWebRequestException e)
            {
                Debug.LogError(e.Error);
                return (null, null);
            }
            finally
            {
                foreach (var request in requests)
                {
                    request.Dispose();
                }
            }
        }
        
        public static AudioCache CreateCache(string modelFolder)
        {
            if (!Directory.Exists(modelFolder))
            {
                Directory.CreateDirectory(modelFolder);
            }
            return new AudioCache(Path.Combine(modelFolder, ".audios"));
        }
    }
}