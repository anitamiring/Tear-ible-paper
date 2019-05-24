using System;
using System.Collections.Generic;
using System.Linq;

namespace Apkd
{
    public static partial class ExtensionMethods
    {
        /// <summary> Determines whether the sequence is empty. </summary>
        public static bool Empty<T>(this IEnumerable<T> enumerable)
            => !enumerable.Any();

        /// <summary> Converts a null value into an empty enumerable. </summary>
        public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T> enumerable)
            => enumerable != null ? enumerable : Enumerable.Empty<T>();

        /// <summary> Produces an <see cref="IEnumerable{T}"/> that excludes elements which satisfy the condition. </summary>
        public static IEnumerable<T> Except<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate)
        {
            foreach (var item in enumerable)
                if (!predicate(item))
                    yield return item;
        }

        /// <summary> Non-allocating version of Enumerable.ToList() </summary>
        public static List<T> ToList<T>(this IEnumerable<T> enumerable, List<T> list, bool append = false)
        {
            if (!append)
                list.Clear();

            foreach (var item in enumerable)
                list.Add(item);

            return list;
        }

        /// <summary> Non-allocating version of Enumerable.ToArray(). Array must match size. </summary>
        public static T[] ToArray<T>(this IEnumerable<T> enumerable, T[] array)
        {
            int i = 0;

            foreach (var item in enumerable)
                array[i++] = item;

            return array;
        }

        public static List<T> Shuffle<T>(this IEnumerable<T> enumerable)
        {
            var shuffled = new List<T>(enumerable);
            int n = shuffled.Count;

            while (n > 1)
            {
                int k = UnityEngine.Random.Range(0, n);
                n -= 1;

                var value = shuffled[k];
                shuffled[k] = shuffled[n];
                shuffled[n] = value;
            }

            return shuffled;
        }
    }
}