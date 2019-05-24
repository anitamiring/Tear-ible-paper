using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Apkd
{
    public sealed class RecyclablePool : Internal.ObjectPool<Poolable>
    {
        readonly Poolable prefab;
        static readonly Dictionary<Poolable, RecyclablePool> prefabPoolMap = new Dictionary<Poolable, RecyclablePool>();
        static readonly Dictionary<Poolable, RecyclablePool> instancePoolMap = new Dictionary<Poolable, RecyclablePool>();
        static Lazy<Scene> poolScene = new Lazy<Scene>(() => SceneManager.CreateScene("PooledObjects"));

        public RecyclablePool(Poolable prefab) : base()
        {
#if UNITY_EDITOR
            if (!UnityEditor.PrefabUtility.IsPartOfPrefabAsset(prefab))
                throw new System.ArgumentException($"Object {prefab} is not a prefab");
            
            if (!Application.isPlaying)
                throw new System.ArgumentException($"{nameof(RecyclablePool)} used in edit mode.");

            var poolable = prefab.GetComponent<Poolable>();

            if (!poolable)
                throw new System.ArgumentException($"Prefab {prefab} needs to have a {nameof(Poolable)} component.");

            this.prefab = poolable;
#else
            this.prefab = prefab.GetComponent<Poolable>();
#endif
        }

        public static RecyclablePool ForPrefab(Poolable prefab)
            => prefabPoolMap.GetOrInitialize(key: prefab, initializer: x => new RecyclablePool(x));

        static RecyclablePool ForInstance(Poolable instance)
            => instancePoolMap.TryGetValue(instance, out var pool) ? pool : throw new System.ArgumentException($"Unable to get pool for {instance}");

        public static void Reclaim(Poolable instance)
            => RecyclablePool.ForInstance(instance).ReclaimInstance(instance);

        protected override Poolable CreateInstance()
        {
            var instance = Object.Instantiate(prefab.gameObject);
#if UNITY_EDITOR
            SceneManager.MoveGameObjectToScene(instance, poolScene);
#endif
            var poolable = instance.GetComponent<Poolable>();
            poolable.Prefab = prefab;
            instancePoolMap[poolable] = this;
            return poolable;
        }

        protected override void RecycleInstance(Poolable instance)
        {
            instance.gameObject.SetActive(false);
            instance.Recycle();
        }

        public override void Prewarm(int? count = default)
        {
            int n = count ?? InitialCapacity;
            bool wasActive = prefab.gameObject.activeSelf;
            prefab.gameObject.SetActive(false);
            while (pool.Count < n)
            {
                var instance = CreateInstance();
                pool.Push(instance);
            }
            prefab.gameObject.SetActive(wasActive);
        }

        public Poolable GetInstance(Vector3 position, Quaternion rotation)
        {
            var instance = GetInstance();
            instance.gameObject.SetActive(true);
            instance.transform.SetPositionAndRotation(position, rotation);
            return instance;
        }
    }
}