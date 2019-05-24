namespace Apkd
{
    public interface IRecyclable<T> where T : UnityEngine.MonoBehaviour
    {
        void Recycle(T prefab);
    }

    namespace Internal
    {
        public interface IRecyclable : IRecyclable<UnityEngine.MonoBehaviour> { }
    }
}