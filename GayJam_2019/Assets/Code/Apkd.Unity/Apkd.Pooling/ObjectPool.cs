using System.Collections.Generic;

namespace Apkd
{
    namespace Internal
    {
        public abstract class ObjectPool<T> where T : class, new()
        {
            protected readonly Stack<T> pool;

            public ObjectPool()
                => pool = new Stack<T>(capacity: InitialCapacity);

            protected virtual int InitialCapacity => 8;

            protected virtual void RecycleInstance(T instance) { }

            protected virtual T CreateInstance()
                => new T();

            public T GetInstance()
            {
                if (pool.Count > 0)
                    return pool.Pop();

                return CreateInstance();
            }

            public void ReclaimInstance(T instance)
            {
#if !UNITY_EDITOR
                RecycleInstance(instance);
#else
                try
                {
                    RecycleInstance(instance);
                }
                catch (System.Exception exception)
                {
                    UnityEngine.Debug.LogException(exception);
                }
#endif
                pool.Push(instance);
            }

            public virtual void Prewarm(int? count = default)
            {
                int n = count ?? InitialCapacity;
                while(pool.Count < n)
                    ReclaimInstance(CreateInstance());
            }
        }
    }

    public sealed class ObjectPool<T> : Internal.ObjectPool<T> where T : class, new()
    {
        public ObjectPool() : base() { }
    }
}