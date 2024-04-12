using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
namespace Kurisu.UniChat
{
    public class ChatModelFileAssist
    {
        public readonly List<ChatModelFile> files = new();
        public ChatModelFileAssist()
        {
            LoadFiles();
        }
        public void LoadFiles()
        {
            files.Clear();
            string[] paths = Directory.GetFiles(PathUtil.UserDataPath, "*.cfg", SearchOption.AllDirectories);
            foreach (string path in paths)
            {
                try
                {
                    ChatModelFile modelFile = JsonConvert.DeserializeObject<ChatModelFile>(File.ReadAllText(path));
                    files.Add(modelFile);
                }
                catch
                {
                    continue;
                }
            }
        }
        public void Delate(int index)
        {
            if (Directory.Exists(files[index].DirectoryPath))
                Directory.Delete(files[index].DirectoryPath, true);
            files.RemoveAt(index);
        }
    }
}
