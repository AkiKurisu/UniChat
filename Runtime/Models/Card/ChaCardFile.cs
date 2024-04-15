using System.IO;
using Newtonsoft.Json;
namespace Kurisu.UniChat
{
    public class ChaCardFile
    {
        public byte[] pngData;
        public TavernAICard tavernAIData;
        public ChaCardFile()
        {
            tavernAIData = new();
        }
        public bool LoadThumb(string path)
        {
            using FileStream st = new(path, FileMode.Open, FileAccess.Read);
            using BinaryReader br = new(st);
            long pngSize = PngFile.GetPngSize(br);
            if (pngSize != 0L)
            {
                pngData = br.ReadBytes((int)pngSize);
                return true;
            }
            return false;
        }
        public bool LoadCard(string path, bool noLoadPNG = false)
        {
            if (!File.Exists(path))
            {
                return false;
            }
            if (Path.GetExtension(path) == ".json")
            {
                return LoadCharacterJson(path);
            }
            using FileStream st = new(path, FileMode.Open, FileAccess.Read);
            return LoadCard(st, noLoadPNG);
        }

        public bool LoadCard(Stream st, bool noLoadPNG = false)
        {
            using BinaryReader br = new(st);
            return LoadCard(br, noLoadPNG);
        }
        public bool LoadCharacterJson(string path)
        {
            tavernAIData = JsonConvert.DeserializeObject<TavernAICard>(File.ReadAllText(path));
            return true;
        }
        public virtual bool LoadCard(BinaryReader br, bool noLoadPNG = false)
        {
            long pngSize = PngFile.GetPngSize(br);
            if (pngSize != 0L)
            {
                if (noLoadPNG)
                {
                    br.BaseStream.Seek(pngSize, SeekOrigin.Current);
                }
                else
                {
                    pngData = br.ReadBytes((int)pngSize);
                }
                if (br.BaseStream.Length - br.BaseStream.Position == 0L)
                {
                    return false;
                }
            }
            if (br.ReadString() != "【UniChat_TavernAI】")
            {
                return false;
            }
            var json = br.ReadString();
            tavernAIData = JsonConvert.DeserializeObject<TavernAICard>(json);
            return true;
        }
        public bool SaveCard(string path, bool savePng)
        {
            using FileStream st = new(path, FileMode.Create, FileAccess.Write);
            return SaveCard(st, savePng);
        }
        public bool SaveCard(Stream st, bool savePng)
        {
            using BinaryWriter bw = new(st);
            return SaveCard(bw, savePng);
        }
        public virtual bool SaveCard(BinaryWriter bw, bool savePng)
        {
            if (savePng && pngData != null)
            {
                bw.Write(pngData);
            }
            bw.Write("【UniChat_TavernAI】");
            bw.Write(JsonConvert.SerializeObject(tavernAIData));
            return true;
        }
    }
}