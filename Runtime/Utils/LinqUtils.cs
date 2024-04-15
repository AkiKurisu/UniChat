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
        public static void AddRange<T>(this IList<T> list, IEnumerable<T> collection)
        {
            if (list is List<T> list1)
            {
                list1.AddRange(collection);
                return;
            }

            foreach (T item in collection)
            {
                list.Add(item);
            }
        }
    }
}