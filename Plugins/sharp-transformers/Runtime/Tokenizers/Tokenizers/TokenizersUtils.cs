using System.Collections.Generic;
namespace TokenizersUtils
{
    public static class Utils
    {
        public static List<int> Fuse(List<int> arr, int value)
        {
            List<int> fused = new();
            int i = 0;
            while (i < arr.Count)
            {
                fused.Add(arr[i]);
                if (arr[i] != value)
                {
                    i++;
                    continue;
                }

                while (i < arr.Count && arr[i] == value)
                {
                    i++;
                }
            }

            return fused;
        }
    }
}