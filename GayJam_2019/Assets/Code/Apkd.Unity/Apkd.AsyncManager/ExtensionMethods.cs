using System;
using UnityEngine;

namespace Apkd
{
    public static partial class ExtensionMethods
    {
        public static AsyncManager.UnityAwaiter AsyncNextUpdate(this UnityEngine.Object behaviour)
            => AsyncManager.Instance.NextUpdate(behaviour);

        public static AsyncManager.UnityAwaiter AsyncNextLateUpdate(this UnityEngine.Object behaviour)
            => AsyncManager.Instance.NextLateUpdate(behaviour);

        public static AsyncManager.UnityAwaiter AsyncNextFixedUpdate(this UnityEngine.Object behaviour)
            => AsyncManager.Instance.NextFixedUpdate(behaviour);

        public static AsyncManager.UnityAwaiter AsyncUpdates(this UnityEngine.Object behaviour, int framesToWait)
            => AsyncManager.Instance.Updates(behaviour, framesToWait);

        public static AsyncManager.UnityAwaiter AsyncLateUpdates(this UnityEngine.Object behaviour, int framesToWait)
            => AsyncManager.Instance.LateUpdates(behaviour, framesToWait);

        public static AsyncManager.UnityAwaiter AsyncFixedUpdates(this UnityEngine.Object behaviour, int stepsToWait)
            => AsyncManager.Instance.FixedUpdates(behaviour, stepsToWait);

        public static AsyncManager.UnityAwaiter AsyncDelay(this UnityEngine.Object behaviour, float secondsToWait)
            => AsyncManager.Instance.Seconds(behaviour, secondsToWait);

        public static AsyncManager.UnityAwaiter AsyncDelay(this UnityEngine.Object behaviour, TimeSpan duration)
            => AsyncManager.Instance.Seconds(behaviour, (float)duration.TotalSeconds);

        public static AsyncManager.UnityAwaiter AsyncDelayRealtime(this UnityEngine.Object behaviour, float secondsToWait)
            => AsyncManager.Instance.SecondsUnscaled(behaviour, secondsToWait);

        public static AsyncManager.UnityAwaiter AsyncUntil(this UnityEngine.Object behaviour, Func<bool> condition)
            => AsyncManager.Instance.Until(behaviour, condition);

        public static AsyncManager.UnityAwaiter AsyncWhile(this UnityEngine.Object behaviour, Func<bool> condition)
            => AsyncManager.Instance.While(behaviour, condition);

        public static AsyncManager.UnityAwaiter GetAwaiter(this AsyncOperation @this)
            => AsyncManager.Instance.AsyncOp(AsyncManager.Instance, @this);

        public static AsyncManager.UnityAwaiter GetAwaiter(this CustomYieldInstruction @this)
            => AsyncManager.Instance.Custom(AsyncManager.Instance, @this);
    }
}