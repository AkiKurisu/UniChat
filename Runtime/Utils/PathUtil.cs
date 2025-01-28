using System.IO;
using UnityEngine;
namespace UniChat
{
    internal class LazyDirectory
    {
        private readonly string path;
        private bool initialized;
        public LazyDirectory(string path)
        {
            this.path = path;
        }
        public string GetPath()
        {
            if (initialized)
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                initialized = true;
            }
            return path;
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
