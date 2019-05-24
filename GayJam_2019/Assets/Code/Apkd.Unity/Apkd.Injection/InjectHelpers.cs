using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Apkd.Internal
{
    public static class InjectHelpers
    {
        static readonly Dictionary<Type, UnityEngine.Object> singletonCache = new Dictionary<Type, UnityEngine.Object>();
        static readonly Dictionary<Type, Type[]> interfaceImplementationsCache = new Dictionary<Type, Type[]>();

        static Component WrapResult(this Component value)
        {
            // ensure resolved component isn't a UnityEngine placeholder null-like object
#if UNITY_EDITOR
            if (value == null)
                return null;
#endif
            return value;
        }

#if UNITY_EDITOR
        static Func<int, bool> getIsDirtyFunc;

        static Func<int, bool> GetIsDirty
                => getIsDirtyFunc = getIsDirtyFunc ?? (Func<int, bool>)typeof(UnityEditor.EditorUtility)
                    .GetMethod("IsDirty", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    .CreateDelegate(typeof(Func<int, bool>));

        static bool OmitInject(UnityEngine.Object obj)
            => (obj as Component)?.CompareTag("NoInject") ?? false;

        public static bool ShouldUpdateInjectedComponents(UnityEngine.Object obj)
            => !OmitInject(obj) && (GetIsDirty(obj.GetInstanceID()) || !UnityEditor.PrefabUtility.IsPartOfPrefabAsset(obj));
#else
        public static bool ShouldUpdateInjectedComponents(UnityEngine.Object obj)
            => true;
#endif

        public static Component Resolve(MonoBehaviour obj, Type type)
            => obj.GetComponent(type).WrapResult();

        public static T[] ResolveArray<T>(MonoBehaviour obj)
            => obj.GetComponents<T>();

        public static Component ResolveFromChildren(MonoBehaviour obj, Type type)
            => obj.GetComponentInChildren(type).WrapResult();

        public static Component ResolveFromChildrenIncludeInactive(MonoBehaviour obj, Type type)
            => obj.GetComponentInChildren(type, includeInactive: true).WrapResult();

        public static T[] ResolveFromChildrenArray<T>(MonoBehaviour obj)
            => obj.GetComponentsInChildren<T>();

        public static T[] ResolveFromChildrenArrayIncludeInactive<T>(MonoBehaviour obj)
            => obj.GetComponentsInChildren<T>(includeInactive: true);

        public static ReadOnlySet<T> ResolveRuntimeSet<T>(MonoBehaviour obj) where T : class
            => SetContainer<T>.Instances;

        public static Component ResolveFromParents(MonoBehaviour obj, Type type)
            => obj.GetComponentInParent(type).WrapResult();

        public static T[] ResolveFromParentsArray<T>(MonoBehaviour obj)
            => obj.GetComponentsInParent<T>();

        public static T[] ResolveFromParentsArrayIncludeInactive<T>(MonoBehaviour obj)
            => obj.GetComponentsInParent<T>(includeInactive: true);

        public static UnityEngine.Object ResolveScriptableObject(MonoBehaviour _, Type type)
            => TryGetCachedObjectOfType(type) ?? TryFindScriptableObject(type);

        static void AddToPreloadedAssets(UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            var preloadedAssets = UnityEditor.PlayerSettings.GetPreloadedAssets();
            if (preloadedAssets.Contains(obj))
                return;

            preloadedAssets = preloadedAssets.Append(obj).ToArray();
            UnityEditor.PlayerSettings.SetPreloadedAssets(preloadedAssets);
#endif
        }

        static UnityEngine.Object TryFindScriptableObject(Type type)
        {
            UnityEngine.Object obj;

            obj = Resources.FindObjectsOfTypeAll(type).FirstOrDefault();
            if (obj)
            {
                singletonCache[type] = obj;
                AddToPreloadedAssets(obj);
                return obj;
            }
#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets("t:" + type.Name);
            if (guids.Length == 0)
                return null;
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids.FirstOrDefault());
            obj = UnityEditor.AssetDatabase.LoadAssetAtPath(path, type);
            if (obj)
            {
                singletonCache[type] = obj;
                AddToPreloadedAssets(obj);
                return obj;
            }
#endif
            return null;
        }

        static readonly Lazy<System.Reflection.Assembly> mainAssembly = new Lazy<System.Reflection.Assembly>(
            () => AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.FullName.StartsWith("Assembly-CSharp, ")), isThreadSafe: false);

        static IEnumerable<Type> GetTypesImplementingInterface(Type type)
        {
            foreach (var candidate in mainAssembly.Value.DefinedTypes)
                if (candidate.IsClass)
                    if (candidate.ImplementedInterfaces.Contains(type))
                        yield return candidate;
        }

        static UnityEngine.Object TryGetCachedObjectOfType(Type type)
            => singletonCache.TryGetValue(type, out var obj) ? obj : null;

        static UnityEngine.Object TryFindSingletonOfType(Type type)
        {
            UnityEngine.Object obj;

            // slow method for obtaining object from scene
            obj = UnityEngine.Object.FindObjectOfType(type);
            if (obj)
            {
                singletonCache[type] = obj;
                return obj;
            }

            // fallback to an even slower hack for obtaining disabled components
            obj = Resources
                .FindObjectsOfTypeAll(type)
                .FirstOrDefault(x => (x as Component)?.gameObject.scene.name != null);

            if (obj)
            {
                singletonCache[type] = obj;
                return obj;
            }

            return null;
        }

        static UnityEngine.Object ResolveSingletonClass(Type type)
            => TryGetCachedObjectOfType(type) ?? TryFindSingletonOfType(type);

        public static T ResolveSingleton<T>(MonoBehaviour _) where T : class
        {
            var type = typeof(T);

            if (Application.isPlaying)
            {
                var singleton = SingletonContainer<T>.GetInstanceNoWarn();
                if (singleton != null)
                    return singleton;
            }

            if (type.IsInterface)
            {
                Type[] implementations;

                if (!interfaceImplementationsCache.TryGetValue(type, out implementations))
                    implementations = GetTypesImplementingInterface(type).ToArray();

                return implementations
                    .Select(ResolveSingletonClass)
                    .FirstOrDefault(x => x != null) as T;
            }
            if (type.IsClass)
            {
                return ResolveSingletonClass(type) as T;
            }
            throw new System.NotSupportedException($"Cannot inject type: {type.FullName}");
        }

        public static UnityEngine.Object ResolveMainCamera(MonoBehaviour _, Type type)
            => Camera.main;

        public static ParticleSystem.CollisionModule ResolveCollisionModule(MonoBehaviour obj, Type _)
            => obj.GetComponent<ParticleSystem>().collision;

        public static ParticleSystem.ColorBySpeedModule ResolveColorBySpeedModule(MonoBehaviour obj, Type _)
            => obj.GetComponent<ParticleSystem>().colorBySpeed;

        public static ParticleSystem.ColorOverLifetimeModule ResolveColorOverLifetimeModule(MonoBehaviour obj, Type _)
            => obj.GetComponent<ParticleSystem>().colorOverLifetime;

        public static ParticleSystem.CustomDataModule ResolveCustomDataModule(MonoBehaviour obj, Type _)
            => obj.GetComponent<ParticleSystem>().customData;

        public static ParticleSystem.EmissionModule ResolveEmissionModule(MonoBehaviour obj, Type _)
            => obj.GetComponent<ParticleSystem>().emission;

        public static ParticleSystem.ExternalForcesModule ResolveExternalForcesModule(MonoBehaviour obj, Type _)
            => obj.GetComponent<ParticleSystem>().externalForces;

        public static ParticleSystem.ForceOverLifetimeModule ResolveForceOverLifetimeModule(MonoBehaviour obj, Type _)
            => obj.GetComponent<ParticleSystem>().forceOverLifetime;

        public static ParticleSystem.InheritVelocityModule ResolveFromheritVelocityModule(MonoBehaviour obj, Type _)
            => obj.GetComponent<ParticleSystem>().inheritVelocity;

        public static ParticleSystem.LightsModule ResolveLightsModule(MonoBehaviour obj, Type _)
            => obj.GetComponent<ParticleSystem>().lights;

        public static ParticleSystem.LimitVelocityOverLifetimeModule ResolveLimitVelocityOverLifetimeModule(MonoBehaviour obj, Type _)
            => obj.GetComponent<ParticleSystem>().limitVelocityOverLifetime;

        public static ParticleSystem.MainModule ResolveMainModule(MonoBehaviour obj, Type _)
            => obj.GetComponent<ParticleSystem>().main;

        public static ParticleSystem.NoiseModule ResolveNoiseModule(MonoBehaviour obj, Type _)
            => obj.GetComponent<ParticleSystem>().noise;

        public static ParticleSystem.RotationBySpeedModule ResolveRotationBySpeedModule(MonoBehaviour obj, Type _)
            => obj.GetComponent<ParticleSystem>().rotationBySpeed;

        public static ParticleSystem.RotationOverLifetimeModule ResolveRotationOverLifetimeModule(MonoBehaviour obj, Type _)
            => obj.GetComponent<ParticleSystem>().rotationOverLifetime;

        public static ParticleSystem.ShapeModule ResolveShapeModule(MonoBehaviour obj, Type _)
            => obj.GetComponent<ParticleSystem>().shape;

        public static ParticleSystem.SizeBySpeedModule ResolveSizeBySpeedModule(MonoBehaviour obj, Type _)
            => obj.GetComponent<ParticleSystem>().sizeBySpeed;

        public static ParticleSystem.SizeOverLifetimeModule ResolveSizeOverLifetimeModule(MonoBehaviour obj, Type _)
            => obj.GetComponent<ParticleSystem>().sizeOverLifetime;

        public static ParticleSystem.SubEmittersModule ResolveSubEmittersModule(MonoBehaviour obj, Type _)
            => obj.GetComponent<ParticleSystem>().subEmitters;

        public static ParticleSystem.TextureSheetAnimationModule ResolveTextureSheetAnimationModule(MonoBehaviour obj, Type _)
            => obj.GetComponent<ParticleSystem>().textureSheetAnimation;

        public static ParticleSystem.TrailModule ResolveTrailModule(MonoBehaviour obj, Type _)
            => obj.GetComponent<ParticleSystem>().trails;

        public static ParticleSystem.TriggerModule ResolveTriggerModule(MonoBehaviour obj, Type _)
            => obj.GetComponent<ParticleSystem>().trigger;

        public static ParticleSystem.VelocityOverLifetimeModule ResolveVelocityOverLifetimeModule(MonoBehaviour obj, Type _)
            => obj.GetComponent<ParticleSystem>().velocityOverLifetime;
    }
}