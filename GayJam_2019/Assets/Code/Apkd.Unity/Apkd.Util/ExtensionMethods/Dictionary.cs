using System;
using System.Collections.Generic;

namespace Apkd
{
    public static partial class ExtensionMethods
    {
        /// <summary> Gets the item from the dictionary. If necessary, creates the item using the initialization function. </summary>
        /// <remarks> Warning: Null values are treated same as no value. </remarks>
        public static T GetOrInitialize<T, TKey>(this IDictionary<TKey, T> dict, TKey key, Func<TKey, T> initializer)
        {
            T value;
            if (!dict.TryGetValue(key, out value) || value == null)
                dict[key] = value = initializer(key);

            return value;
        }

        /// <summary> Gets the item from the dictionary or create the item using the default value. </summary>
        /// <remarks> Warning: Null values are treated same as no value. </remarks>
        public static T GetOrInitialize<T, TKey>(this IDictionary<TKey, T> dict, TKey key, T defaultValue)
        {
            T value;
            if (!dict.TryGetValue(key, out value) || value == null)
                dict[key] = value = defaultValue;

            return value;
        }

        /// <summary> Gets the item from the dictionary. If necessary, creates the item using the initialization function. </summary>
        /// <remarks> Warning: Null values are treated same as no value. </remarks>
        public static T GetOrInitialize<T, TKey>(this IDictionary<TKey, T> dict, TKey key, Func<T> initializer)
        {
            T value;
            if (!dict.TryGetValue(key, out value) || value == null)
                dict[key] = value = initializer();

            return value;
        }

        public static T GetValueOrDefault<T, TKey>(this IReadOnlyDictionary<TKey, T> dict, TKey key, T defaultValue = default(T))
            => dict.TryGetValue(key, out var value) ? value : defaultValue;
    }
}