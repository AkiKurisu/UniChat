using UnityEngine;
namespace UniChat
{
    public static class TextureUtils
    {
        public static Texture2D ChangeTextureFromByte(
            byte[] data,
            int width = 0,
            int height = 0,
            TextureFormat format = TextureFormat.ARGB32,
            bool mipmap = false
        )
        {
            Texture2D tex = new(width, height, format, mipmap);
            if (null == tex)
                return null;
            tex.LoadImage(data);
            return tex;
        }
    }
}