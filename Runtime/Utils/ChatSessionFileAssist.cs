using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
namespace Kurisu.UniChat
{
    public class ChatSessionFileAssist
    {
        public class ChatSessionFileInfo
        {
            public ChatSession session;
            public string fileName;
            public string filePath;
        }
        public readonly List<ChatSessionFileInfo> files = new();
        public ChatSessionFileAssist()
        {
            LoadFiles();
        }
        public void LoadFiles()
        {
            files.Clear();
            string[] paths = Directory.GetFiles(PathUtil.SessionPath, "*.json", SearchOption.AllDirectories);
            foreach (string path in paths)
            {
                try
                {
                    ChatSession sessionFile = JsonConvert.DeserializeObject<ChatSession>(File.ReadAllText(path));
                    files.Add(new ChatSessionFileInfo()
                    {
                        session = sessionFile,
                        filePath = path,
                        fileName = Path.GetFileNameWithoutExtension(path)
                    });
                }
                catch
                {
                    continue;
                }
            }
        }
        public void Delate(int index)
        {
            if (File.Exists(files[index].filePath))
                File.Delete(files[index].filePath);
            files.RemoveAt(index);
        }
    }
}
