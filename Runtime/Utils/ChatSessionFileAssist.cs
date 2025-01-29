using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
namespace UniChat
{
    public class ChatSessionFileAssist
    {
        public class ChatSessionFileInfo
        {
            public ChatSession Session;
            
            public string FileName;
            
            public string FilePath;
        }
        
        public readonly List<ChatSessionFileInfo> Files = new();
        
        public ChatSessionFileAssist()
        {
            LoadFiles();
        }
        
        public void LoadFiles()
        {
            Files.Clear();
            string[] paths = Directory.GetFiles(PathUtil.SessionPath, "*.json", SearchOption.AllDirectories);
            foreach (string path in paths)
            {
                try
                {
                    ChatSession sessionFile = JsonConvert.DeserializeObject<ChatSession>(File.ReadAllText(path));
                    Files.Add(new ChatSessionFileInfo()
                    {
                        Session = sessionFile,
                        FilePath = path,
                        FileName = Path.GetFileNameWithoutExtension(path)
                    });
                }
                catch
                {
                    continue;
                }
            }
        }
        
        public void Delete(int index)
        {
            if (File.Exists(Files[index].FilePath))
                File.Delete(Files[index].FilePath);
            Files.RemoveAt(index);
        }
    }
}
