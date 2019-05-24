using System.Collections.Generic;
using System.Linq;

namespace Apkd
{
    public static partial class ExtensionMethods
    {
        /// <summary> Select a random element from the array. </summary>
        public static T FirstRandom<T>(this T[] array)
            => array[FirstRandomIndex(array)];

        /// <summary> Select a random element from the collection. </summary>
        public static T FirstRandom<T>(this IReadOnlyCollection<T> collection)
            => collection.ElementAtOrDefault(FirstRandomIndex(collection));

        /// <summary> Select a random element from the set. </summary>
        public static T FirstRandom<T>(this ReadOnlySet<T> set)
            => set.ElementAtOrDefault(FirstRandomIndex(set));

        /// <summary> Select a random element index from the collection. </summary>
        public static int FirstRandomIndex<T>(this T[] array)
            => UnityEngine.Random.Range(0, array.Length);

        /// <summary> Select a random element index from the collection. </summary>
        public static int FirstRandomIndex<T>(this IReadOnlyCollection<T> collection)
            => UnityEngine.Random.Range(0, collection.Count);

        /// <summary> Select a random element index from the set. </summary>
        public static int FirstRandomIndex<T>(this ReadOnlySet<T> set)
            => UnityEngine.Random.Range(0, set.Count);

        /// <summary> Select a number of random elements from the collection. </summary>
        public static IEnumerable<T> FirstRandom<T>(this IReadOnlyCollection<T> collection, int n)
        {
            for (int i = 0; i < n; ++i)
                yield return collection.FirstRandom();
        }
    }
}