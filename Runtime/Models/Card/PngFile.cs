using System;
using System.IO;
namespace Kurisu.UniChat
{
    public static class PngFile
    {
        public static long GetPngSize(BinaryReader br)
        {
            return GetPngSize(br.BaseStream);
        }

        public static long GetPngSize(Stream st)
        {
            if (st == null)
                return 0;
            long position = st.Position;
            long pngSize;
            try
            {
                byte[] buffer1 = new byte[8];
                byte[] numArray = new byte[8]
                {
                137,
                80,
                78,
                71,
                13,
                10,
                26,
                10
                };
                st.Read(buffer1, 0, 8);
                for (int index = 0; index < 8; ++index)
                {
                    if (buffer1[index] != numArray[index])
                    {
                        st.Seek(position, SeekOrigin.Begin);
                        return 0;
                    }
                }
                bool flag = true;
                while (flag)
                {
                    byte[] buffer2 = new byte[4];
                    st.Read(buffer2, 0, 4);
                    Array.Reverse((Array)buffer2);
                    int int32 = BitConverter.ToInt32(buffer2, 0);
                    byte[] buffer3 = new byte[4];
                    st.Read(buffer3, 0, 4);
                    if (BitConverter.ToInt32(buffer3, 0) == 1145980233)
                        flag = false;
                    if (int32 + 4 > st.Length - st.Position)
                    {
                        st.Seek(position, SeekOrigin.Begin);
                        return 0;
                    }
                    st.Seek(int32 + 4, SeekOrigin.Current);
                }
                pngSize = st.Position - position;
                st.Seek(position, SeekOrigin.Begin);
            }
            catch
            {
                st.Seek(position, SeekOrigin.Begin);
                return 0;
            }
            return pngSize;
        }

        public static long SkipPng(Stream st)
        {
            long pngSize = GetPngSize(st);
            st.Seek(pngSize, SeekOrigin.Current);
            return pngSize;
        }

        public static long SkipPng(BinaryReader br)
        {
            long pngSize = GetPngSize(br);
            br.BaseStream.Seek(pngSize, SeekOrigin.Current);
            return pngSize;
        }

        public static byte[] LoadPngBytes(string path)
        {
            using FileStream st = new(path, FileMode.Open, FileAccess.Read);
            return LoadPngBytes(st);
        }

        public static byte[] LoadPngBytes(Stream st)
        {
            using BinaryReader br = new(st);
            return LoadPngBytes(br);
        }

        public static byte[] LoadPngBytes(BinaryReader br)
        {
            long pngSize = GetPngSize(br);
            return pngSize == 0L ? null : br.ReadBytes((int)pngSize);
        }
    }
}