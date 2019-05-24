using UnityEngine;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace Apkd
{
    public static partial class ExtensionMethods
    {
        const MethodImplOptions inline = MethodImplOptions.AggressiveInlining;

        [MethodImpl(inline)] public static Vector3 WithX(this Vector3 v, float x) => new Vector3(x, v.y, v.z);
        [MethodImpl(inline)] public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);
        [MethodImpl(inline)] public static Vector3 WithZ(this Vector3 v, float z) => new Vector3(v.x, v.y, z);
        [MethodImpl(inline)] public static Vector3 WithXY(this Vector3 v, float x, float y) => new Vector3(x, y, v.z);
        [MethodImpl(inline)] public static Vector3 WithXZ(this Vector3 v, float x, float z) => new Vector3(x, v.y, z);
        [MethodImpl(inline)] public static Vector3 WithYZ(this Vector3 v, float y, float z) => new Vector3(v.x, y, z);

        [MethodImpl(inline)] public static Vector3 WithX(this Vector3 v, Vector3 other) => new Vector3(other.x, v.y, v.z);
        [MethodImpl(inline)] public static Vector3 WithY(this Vector3 v, Vector3 other) => new Vector3(v.x, other.y, v.z);
        [MethodImpl(inline)] public static Vector3 WithZ(this Vector3 v, Vector3 other) => new Vector3(v.x, v.y, other.z);
        [MethodImpl(inline)] public static Vector3 WithXY(this Vector3 v, Vector3 other) => new Vector3(other.x, other.y, v.z);
        [MethodImpl(inline)] public static Vector3 WithXZ(this Vector3 v, Vector3 other) => new Vector3(other.x, v.y, other.z);
        [MethodImpl(inline)] public static Vector3 WithYZ(this Vector3 v, Vector3 other) => new Vector3(v.x, other.y, other.z);

        [MethodImpl(inline)]
        public static float InverseLerp(Vector3 a, Vector3 b, Vector3 value)
        {
            Vector3 ab = b - a;
            Vector3 av = value - a;
            return Vector3.Dot(av, ab) / Vector3.Dot(ab, ab);
        }

        public static Vector3 Average<T>(this IEnumerable<T> enumerable, System.Func<T, Vector3> selector)
        {
            int n = 0;
            Vector3 sum = default(Vector3);
            foreach (T item in enumerable)
            {
                sum += selector(item);
                ++n;
            }
            return sum / n;
        }

        public static Vector3 Average<T>(this T[] array, System.Func<T, Vector3> selector)
        {
            Vector3 sum = default(Vector3);

            for (int i = 0; i < array.Length; ++i)
                sum += selector(array[i]);

            return sum / array.Length;
        }

        const float ε = 1E-05f;
    }
}