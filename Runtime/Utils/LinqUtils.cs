using System;
using System.Collections.Generic;
namespace Kurisu.UniChat
{
    public static class LinqUtils
    {
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T item in source)
            {
                action(item);
            }
            return source;
        }
    }
}