using System.IO;
using UnityEngine;
namespace Kurisu.UniChat
{
    public class PathUtil
    {
#if UNITY_EDITOR||!UNITY_ANDROID
        public static readonly string UserDataPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "UserData");
#else
        public static readonly string UserDataPath = Path.Combine(Application.persistentDataPath, "UserData");
#endif
        public static readonly string SessionPath = Path.Combine(UserDataPath, "sessions");
        public static readonly string ModelPath = Path.Combine(UserDataPath, "models");
        public static readonly string CharacterPath = Path.Combine(UserDataPath, "characters");
        [RuntimeInitializeOnLoadMethod]
        public static void Initialize()
        {
            if (!Directory.Exists(UserDataPath))
            {
                Directory.CreateDirectory(UserDataPath);
            }
            if (!Directory.Exists(SessionPath))
            {
                Directory.CreateDirectory(SessionPath);
            }
            if (!Directory.Exists(ModelPath))
            {
                Directory.CreateDirectory(ModelPath);
            }
            if (!Directory.Exists(CharacterPath))
            {
                Directory.CreateDirectory(CharacterPath);
            }
        }
    }
}
