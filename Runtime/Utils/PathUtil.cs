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
    
    public class PathUtil
    {
#if UNITY_EDITOR||!UNITY_ANDROID
        public static readonly string UserDataPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "UserData");
#else
        public static readonly string UserDataPath = Path.Combine(Application.persistentDataPath, "UserData");
#endif
        private static readonly LazyDirectory sessionPath = new(Path.Combine(UserDataPath, "sessions"));
        
        public static string SessionPath => sessionPath.GetPath();
        

        private static readonly LazyDirectory modelPath = new(Path.Combine(UserDataPath, "models"));
        
        public static string ModelPath => modelPath.GetPath();
        

        private static readonly LazyDirectory characterPath = new(Path.Combine(UserDataPath, "characters"));
        
        public static string CharacterPath => characterPath.GetPath();
        

        [RuntimeInitializeOnLoadMethod]
        public static void Initialize()
        {
            if (!Directory.Exists(UserDataPath))
            {
                Directory.CreateDirectory(UserDataPath);
            }
        }
    }
}
