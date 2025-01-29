using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
namespace UniChat
{
    public class ChatModelFileAssist
    {
        public readonly List<ChatModelFile> Files = new();
        
        public ChatModelFileAssist()
        {
            LoadFiles();
        }
        
        public void LoadFiles()
        {
            Files.Clear();
            string[] paths = Directory.GetFiles(PathUtil.UserDataPath, "*.cfg", SearchOption.AllDirectories);
            foreach (string path in paths)
            {
                try
                {
                    ChatModelFile modelFile = JsonConvert.DeserializeObject<ChatModelFile>(File.ReadAllText(path));
                    Files.Add(modelFile);
                }
                catch
                {
                    continue;
                }
            }
        }
        
        public void Delete(int index)
        {
            if (Directory.Exists(Files[index].DirectoryPath))
                Directory.Delete(Files[index].DirectoryPath, true);
            Files.RemoveAt(index);
        }
    }
}
