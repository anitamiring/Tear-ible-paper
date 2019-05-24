using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Apkd.Internal
{
    public static class SetContainer<T> where T : class
    {
        static readonly HashSet<T> instances = new HashSet<T>();

        public static ReadOnlySet<T> Instances
            => new ReadOnlySet<T>(instances);

        public static void RegisterInstance(T instance)
            => instances.Add(instance);
        
        public static void UnregisterInstance(T instance)
            => instances.Remove(instance);
    }
}