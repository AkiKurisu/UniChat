using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
namespace Kurisu.UniChat
{
    /// <summary>
    /// Assist to cache&&load audios
    /// </summary>
    public class AudioFileAssist
    {
        private readonly string folderPath;
        private readonly List<string> files = new();
        private readonly List<string> segments = new();
        private readonly string infoPath;
        public AudioFileAssist(string folderPath)
        {
            this.folderPath = folderPath;
            infoPath = Path.Combine(folderPath, "info.txt");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            if (File.Exists(infoPath))
            {
                using var stream = new FileStream(infoPath, FileMode.Open, FileAccess.Read);
                using var sr = new StreamReader(stream);
                Load(sr);
            }
        }
        public void Save(uint sourceHash, AudioClip[] audioClips, string[] segments)
        {
            for (int i = 0; i < audioClips.Length; ++i)
            {
                string fileName = $"{sourceHash}-{i:D2}";
                files.Add(fileName);
                WavUtil.Save(Path.Combine(folderPath, $"{fileName}.wav"), audioClips[i]);
            }
            this.segments.AddRange(segments);
            using var stream = new FileStream(infoPath, FileMode.Create, FileAccess.Write);
            using var sw = new StreamWriter(stream);
            Save(sw);
        }
        private void Save(StreamWriter sw)
        {
            for (int i = 0; i < files.Count; ++i)
            {
                sw.Write(files[i]);
                sw.Write('|');
                sw.WriteLine(segments[i]);
            }
        }
        private void Load(StreamReader sr)
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                string[] parts = line.Split('|');
                if (parts.Length == 2)
                {
                    string file = parts[0];
                    string segment = parts[1];
                    files.Add(file);
                    segments.Add(segment);
                }
            }
        }
        public bool Contains(uint sourceHash)
        {
            foreach (var file in files)
            {
                if (file.Contains(sourceHash.ToString())) return true;
            }
            return false;
        }
        public string[] GetSegments(uint sourceHash)
        {
            return files.Where(x => x.Contains(sourceHash.ToString())).Select(x => segments[files.IndexOf(x)]).ToArray();
        }
        public IEnumerable<(string filePath, string segment)> GetPathAndSegments(uint sourceHash)
        {
            return files.Where(x => x.Contains(sourceHash.ToString()))
                        .Select(x => (Path.Combine(folderPath, $"{x}.wav"), segments[files.IndexOf(x)]));
        }
        public void Delate(uint sourceHash)
        {
            for (int i = segments.Count - 1; i >= 0; i--)
            {
                if (!files[i].Contains(sourceHash.ToString())) continue;
                if (File.Exists(files[i])) File.Delete(files[i]);
                segments.RemoveAt(i);
                files.RemoveAt(i);
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
        private static void GetAllAudioDirectories(string rootFolder, List<string> directories)
        {
            string[] subDirectories = Directory.GetDirectories(rootFolder);
            foreach (var directory in subDirectories)
            {
                if (directory.Contains(".audios"))
                    directories.Add(directory);
                GetAllAudioDirectories(directory, directories);
            }
        }
        public static AudioFileAssist[] GetAllAssists()
        {
            var list = new List<string>();
            GetAllAudioDirectories(PathUtil.UserDataPath, list);
            return list.Select(x => new AudioFileAssist(x)).ToArray();
        }
        public static AudioFileAssist CreateAssist(string modelFolder)
        {
            if (!Directory.Exists(modelFolder))
            {
                Directory.CreateDirectory(modelFolder);
            }
            return new AudioFileAssist(Path.Combine(modelFolder, ".audios"));
        }
    }
}