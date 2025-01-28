using System;
using System.IO;
using System.Linq;
namespace UniChat
{
    [Serializable]
    public class Embedding
    {
        public float[] values;
        public void Save(BinaryWriter bw)
        {
            bw.Write(values.Length);
            for (int i = 0; i < values.Length; ++i)
            {
                bw.Write(values[i]);
            }
        }
        public void Load(BinaryReader br)
        {
            int textEmbeddingL = br.ReadInt32();
            values = new float[textEmbeddingL];
            for (int i = 0; i < textEmbeddingL; ++i)
            {
                values[i] = br.ReadSingle();
            }
        }

        public Embedding Clone()
        {
            //Deep clone
            return new Embedding()
            {
                values = values.ToArray()
            };
        }
    }
}