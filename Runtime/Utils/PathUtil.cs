using System.IO;
using UnityEngine;

namespace UniChat
{
    internal class LazyDirectory
    {
        private readonly string _path;
        
        private bool _initialized;
        
        public LazyDirectory(string path)
        {
            _path = path;
        }
        
        public string GetPath()
        {
            if (_initialized)
            {
                if (!Directory.Exists(_path))
                {
                    Directory.CreateDirectory(_path);
                }
                _initialized = true;
            }
            return _path;
        }
    }
    
    public static class PathUtil
    {
#if UNITY_EDITOR||!UNITY_ANDROID
        public static readonly string SavedPath = Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "Saved");
#else
        public static readonly string SavedPath = Path.Combine(Application.persistentDataPath, "Saved");
#endif
        private static readonly LazyDirectory SessionPathLazy = new(Path.Combine(SavedPath, "sessions"));
        
        public static string SessionPath => SessionPathLazy.GetPath();
        

        private static readonly LazyDirectory ModelsPathLazy = new(Path.Combine(SavedPath, "models"));
        
        public static string ModelsPath => ModelsPathLazy.GetPath();
        

        private static readonly LazyDirectory CharacterPathLazy = new(Path.Combine(SavedPath, "characters"));
        
        public static string CharacterPath => CharacterPathLazy.GetPath();
        

        [RuntimeInitializeOnLoadMethod]
        public static void Initialize()
        {
            if (!Directory.Exists(SavedPath))
            {
                Directory.CreateDirectory(SavedPath);
            }
        }
    }
}
