using System.Collections.Generic;
using System.Threading.Tasks;
using Odin = Sirenix.OdinInspector;
using UnityEngine;
using System;

namespace Apkd
{
    public static partial class ExtensionMethods
    {
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer = null)
        {
            if (source == null)
                throw new ArgumentException(nameof(source));
            if (keySelector == null)
                throw new ArgumentException(nameof(keySelector));

            return DistinctByImpl(source, keySelector, comparer);
        }

        static IEnumerable<TSource> DistinctByImpl<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            var knownKeys = new HashSet<TKey>(comparer);

            foreach (TSource element in source)
                if (knownKeys.Add(keySelector(element)))
                    yield return element;
        }
    }
}