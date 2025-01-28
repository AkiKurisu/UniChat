using System;
using System.Collections.Generic;
namespace UniChat
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
        /// <summary>
        /// Adds key values of additional to target dictionary if key is not yet present in target
        /// </summary>
        /// <returns>target dictionary</returns>
        public static void TryAddKeyValues<TKey, TValue>(
            this Dictionary<TKey, TValue> target,
            IReadOnlyDictionary<TKey, TValue> additional)
            where TKey : notnull
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            additional = additional ?? throw new ArgumentNullException(nameof(additional));

            foreach (var kv in additional)
            {
                if (!target.ContainsKey(kv.Key))
                {
                    target.Add(kv.Key, kv.Value);
                }
            }
        }
    }
}