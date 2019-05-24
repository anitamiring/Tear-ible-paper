using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Apkd.Internal
{
    public static class SingletonContainer<T> where T : class
    {
        static Object instance;

        public static void RegisterInstance(T thisInstance)
        {
            var unityObject = thisInstance as Object;
            if (instance == null)
            {
                // Debug.Log($"Registering singleton instance: <i>{unityObject.name}</i> as <i>{typeof(T).Name}</i>", unityObject);
                instance = unityObject;
            }
            else
            {
                Debug.LogError($"Attempted to register singleton instance: <i>{unityObject.name}</i> as <i>{typeof(T).Name}</i>, but an instance already exists: <i>{instance.name}</i>", unityObject);
            }
        }

        public static void UnregisterInstance(T thisInstance)
        {
            var unityObject = thisInstance as Object;
            if (System.Object.ReferenceEquals(instance, thisInstance))
            {
                // Debug.Log($"Unregistering singleton instance: <i>{unityObject.name}</i> as <i>{typeof(T).Name}</i>", unityObject);
                instance = null;
            }
            else
            {
                Debug.LogError($"Attempted to unregister a dangling singleton instance: <i>{unityObject.name}</i> as <i>{typeof(T).Name}</i>", unityObject);
            }
        }

#if UNITY_EDITOR
        static bool warnedNoInstance = false;
        public static T GetInstance()
        {
            if (!Application.isPlaying && !instance)
            {
                Debug.Log($"Resolving singleton in edit mode: <i>{typeof(T).Name}</i>");
                instance = InjectHelpers.ResolveSingleton<T>(null) as Object;
            }
            if (instance == null && !warnedNoInstance)
            {
                Debug.LogError($"No registered singleton instance: <i>{typeof(T).Name}</i>");
                warnedNoInstance = true;
            }
            return instance as T;
        }
#else
        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static T GetInstance() => instance as T;
#endif
        public static T GetInstanceNoWarn() => instance as T;
    }
}