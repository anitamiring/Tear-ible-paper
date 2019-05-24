using UnityEngine;
using Odin = Sirenix.OdinInspector;

namespace Apkd
{
    public sealed class Poolable : MonoBehaviour
    {
        [Inject.FromChildren]
        [S] Internal.IRecyclable[] recyclables { get; }

        [Odin.ShowInInspector]
        [Odin.ReadOnly]
        [Odin.HideInEditorMode]
        [Odin.HideInPrefabAssets]
        public Poolable Prefab { get; internal set; }

        public void Recycle()
        {
            for (int i = 0; i < recyclables.Length; ++i)
            {
                try
                {
                    recyclables[i].Recycle(Prefab.recyclables[i] as MonoBehaviour);
                }
                catch (System.Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        public void Prewarm(int? count = null)
            => RecyclablePool.ForPrefab(this).Prewarm(count);
    }
}