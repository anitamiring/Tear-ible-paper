using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Apkd
{
    public static partial class ExtensionMethods
    {
        [MethodImpl(inline)]
        public static T Exists<T>(this T obj) where T : UnityEngine.Object
#if UNITY_EDITOR
            => obj ? obj : null;
#else
            => obj;
#endif
    }
}