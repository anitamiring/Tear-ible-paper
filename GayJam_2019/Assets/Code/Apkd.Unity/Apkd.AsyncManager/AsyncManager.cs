using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Apkd
{
    public sealed class AsyncManager : MonoBehaviour
    {
        abstract class ContinuationQueue
        {
            public abstract void Update();
            public abstract bool IsEmpty { get; }
        }

        sealed class ContinuationQueue<T> : ContinuationQueue where T : IContinuation
        {
            static void RemoveWhileIterating(List<T> list, ref int index)
            {
                int count = list.Count;

                if (count > 1)
                {
                    list[index] = list[count - 1];
                    list.RemoveAt(count - 1);
                    index -= 1; // decrement index so swapped item is next
                }
                else
                {
                    list.RemoveAt(index);
                }
            }

            readonly List<T> buffer;

            public ContinuationQueue(int capacity)
                => buffer = new List<T>(capacity);

            public override void Update()
            {
                for (int i = 0; i < buffer.Count; ++i)
                {
                    var cont = buffer[i];

                    if (cont.IsCompleted())
                        lock (buffer)
                            RemoveWhileIterating(buffer, ref i); // remove without rearranging list
                    else
                        buffer[i] = cont; // reassign as we are dealing with value type and IsCompleted has side effects
                }
            }

            public override bool IsEmpty
                => buffer.Count == 0;

            public void Add(T c)
            {
                lock (buffer)
                    buffer.Add(c);
            }
        }

        public abstract class UnityAwaiter : INotifyCompletion
        {
            public abstract bool IsCompleted { get; }
            public abstract void OnCompleted(Action continuation);
            public void GetResult() { }
            public UnityAwaiter GetAwaiter() => this;
        }

        sealed class UnityAwaiter<T> : UnityAwaiter where T : IContinuation
        {
            readonly ContinuationQueue<T> buffer;
            readonly Queue<T> continuations = new Queue<T>(capacity: 32);

            public UnityAwaiter(ContinuationQueue buffer)
                => this.buffer = buffer as ContinuationQueue<T>;

            public override bool IsCompleted => false;

            public override void OnCompleted(Action continuation)
            {
                var c = continuations.Dequeue();
                c.Set(continuation);
                buffer.Add(c);
            }

            public void AddContinuation(T continuation)
                => continuations.Enqueue(continuation);
        }

        interface IContinuation
        {
            void Set(Action continuation);
            bool IsCompleted();
        }

        struct FrameContinuation : IContinuation
        {
            readonly int waitForFrame;
            readonly UnityEngine.Object parent;
            Action continuation;

            public FrameContinuation(UnityEngine.Object parent, int waitForFrame)
            {
                this.parent = parent;
                this.waitForFrame = waitForFrame;
                continuation = null;
            }

            public void Set(Action cont) => continuation = cont;

            public bool IsCompleted()
            {
                if (!parent)
                    return true;

                if (frameCount >= waitForFrame)
                {
                    continuation();
                    return true;
                }

                return false;
            }
        }

        struct FixedContinuation : IContinuation
        {
            readonly uint waitForStep;
            readonly UnityEngine.Object parent;
            Action continuation;

            public FixedContinuation(UnityEngine.Object parent, uint waitForStep)
            {
                this.parent = parent;
                this.waitForStep = waitForStep;
                continuation = null;
            }

            public void Set(Action cont) => continuation = cont;

            public bool IsCompleted()
            {
                if (!parent)
                    return true;

                if (fixedStepCount >= waitForStep)
                {
                    continuation();
                    return true;
                }

                return false;
            }
        }

        struct TimeContinuation : IContinuation
        {
            readonly float waitForTime;
            readonly UnityEngine.Object parent;
            Action continuation;

            public TimeContinuation(UnityEngine.Object parent, float waitForTime)
            {
                this.parent = parent;
                this.waitForTime = waitForTime;
                continuation = null;
            }

            public void Set(Action cont) => continuation = cont;

            public bool IsCompleted()
            {
                if (!parent)
                    return true;

                if (time >= waitForTime)
                {
                    continuation();
                    return true;
                }

                return false;
            }
        }

        struct CustomContinuation : IContinuation
        {
            readonly CustomYieldInstruction operation;
            readonly UnityEngine.Object parent;
            Action continuation;

            public CustomContinuation(UnityEngine.Object parent, CustomYieldInstruction operation)
            {
                this.parent = parent;
                this.operation = operation;
                continuation = null;
            }

            public void Set(Action cont) => continuation = cont;

            public bool IsCompleted()
            {
                if (!parent)
                    return true;

                if (!operation.MoveNext())
                {
                    continuation();
                    return true;
                }

                return false;
            }
        }

        struct AsyncOpContinuation : IContinuation
        {
            readonly AsyncOperation operation;
            readonly UnityEngine.Object parent;
            Action continuation;

            public AsyncOpContinuation(UnityEngine.Object parent, AsyncOperation operation)
            {
                this.parent = parent;
                this.operation = operation;
                continuation = null;
            }

            public void Set(Action cont) => continuation = cont;

            public bool IsCompleted()
            {
                if (!parent)
                    return true;

                if (operation.isDone)
                {
                    continuation();
                    return true;
                }

                return false;
            }
        }

        struct UnscaledTimeContinuation : IContinuation
        {
            readonly float waitForTime;
            readonly UnityEngine.Object parent;
            Action continuation;

            public UnscaledTimeContinuation(UnityEngine.Object parent, float waitForTime)
            {
                this.parent = parent;
                this.waitForTime = waitForTime;
                continuation = null;
            }

            public void Set(Action cont) => continuation = cont;

            public bool IsCompleted()
            {
                if (!parent)
                    return true;

                if (unscaledTime >= waitForTime)
                {
                    continuation();
                    return true;
                }

                return false;
            }
        }

        struct ConditionContinuation : IContinuation
        {
            readonly Func<bool> condition;
            readonly UnityEngine.Object parent;
            Action continuation;

            public ConditionContinuation(UnityEngine.Object parent, Func<bool> condition)
            {
                this.parent = parent;
                this.condition = condition;
                continuation = null;
            }

            public void Set(Action cont) => continuation = cont;

            public bool IsCompleted()
            {
                if (!parent)
                    return true;

                if (condition())
                {
                    continuation();
                    return true;
                }

                return false;
            }
        }

        struct InvertedConditionContinuation : IContinuation
        {
            readonly Func<bool> condition;
            readonly UnityEngine.Object parent;
            Action continuation;

            public InvertedConditionContinuation(UnityEngine.Object parent, Func<bool> condition)
            {
                this.parent = parent;
                this.condition = condition;
                continuation = null;
            }

            public void Set(Action cont) => continuation = cont;

            public bool IsCompleted()
            {
                if (!parent)
                    return true;

                if (!condition())
                {
                    continuation();
                    return true;
                }

                return false;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            Instance = new GameObject("Singleton:AsyncManager").AddComponent<AsyncManager>();
            DontDestroyOnLoad(Instance.gameObject);
            Instance.gameObject.hideFlags = HideFlags.HideAndDontSave;
            frameCount = 1;
            fixedStepCount = 1;
            time = Time.time;
            unscaledTime = Time.unscaledTime;
        }

        public static AsyncManager Instance { get; private set; }

        static int frameCount;
        static uint fixedStepCount;
        static float time;
        static float unscaledTime;

        public AsyncManager()
        {
            lateUpdateQueue = new ContinuationQueue<FrameContinuation>(16);
            fixedUpdateQueue = new ContinuationQueue<FixedContinuation>(16);
            updateQueues = new List<ContinuationQueue>
            {
                new ContinuationQueue<FrameContinuation>(64),
                new ContinuationQueue<TimeContinuation>(64),
                new ContinuationQueue<UnscaledTimeContinuation>(8),
                new ContinuationQueue<ConditionContinuation>(8),
                new ContinuationQueue<InvertedConditionContinuation>(8),
                new ContinuationQueue<CustomContinuation>(4),
                new ContinuationQueue<AsyncOpContinuation>(4)
            };

            updateAwaiter = new UnityAwaiter<FrameContinuation>(updateQueues[0]);
            lateUpdateAwaiter = new UnityAwaiter<FrameContinuation>(lateUpdateQueue);
            fixedUpdateAwaiter = new UnityAwaiter<FixedContinuation>(fixedUpdateQueue);
            timeAwaiter = new UnityAwaiter<TimeContinuation>(updateQueues[1]);
            unscaledTimeAwaiter = new UnityAwaiter<UnscaledTimeContinuation>(updateQueues[2]);
            conditionAwaiter = new UnityAwaiter<ConditionContinuation>(updateQueues[3]);
            invertedConditionAwaiter = new UnityAwaiter<InvertedConditionContinuation>(updateQueues[4]);
            customAwaiter = new UnityAwaiter<CustomContinuation>(updateQueues[5]);
            asyncOpAwaiter = new UnityAwaiter<AsyncOpContinuation>(updateQueues[6]);
        }

        readonly List<ContinuationQueue> updateQueues;
        readonly ContinuationQueue lateUpdateQueue;
        readonly ContinuationQueue fixedUpdateQueue;

        readonly UnityAwaiter<FrameContinuation> updateAwaiter;
        readonly UnityAwaiter<FixedContinuation> fixedUpdateAwaiter;
        readonly UnityAwaiter<FrameContinuation> lateUpdateAwaiter;
        readonly UnityAwaiter<ConditionContinuation> conditionAwaiter;
        readonly UnityAwaiter<InvertedConditionContinuation> invertedConditionAwaiter;
        readonly UnityAwaiter<UnscaledTimeContinuation> unscaledTimeAwaiter;
        readonly UnityAwaiter<TimeContinuation> timeAwaiter;
        readonly UnityAwaiter<CustomContinuation> customAwaiter;
        readonly UnityAwaiter<AsyncOpContinuation> asyncOpAwaiter;


        void Update()
        {
            time = Time.time;
            unscaledTime = Time.unscaledTime;

            foreach (var q in updateQueues)
                if (!q.IsEmpty)
                    q.Update();
            ++frameCount;
        }

        void LateUpdate()
        {
            if (!lateUpdateQueue.IsEmpty)
                lateUpdateQueue.Update();
        }

        void FixedUpdate()
        {
            if (!fixedUpdateQueue.IsEmpty)
                fixedUpdateQueue.Update();
            ++fixedStepCount;
        }

        public UnityAwaiter NextUpdate(UnityEngine.Object parent)
        {
            updateAwaiter.AddContinuation(new FrameContinuation(parent, frameCount + 1));
            return updateAwaiter;
        }

        public UnityAwaiter NextLateUpdate(UnityEngine.Object parent)
        {
            lateUpdateAwaiter.AddContinuation(new FrameContinuation(parent, frameCount + 1));
            return lateUpdateAwaiter;
        }

        public UnityAwaiter NextFixedUpdate(UnityEngine.Object parent)
        {
            fixedUpdateAwaiter.AddContinuation(new FixedContinuation(parent, fixedStepCount + 1));
            return fixedUpdateAwaiter;
        }

        public UnityAwaiter Updates(UnityEngine.Object parent, int framesToWait)
        {
            updateAwaiter.AddContinuation(new FrameContinuation(parent, frameCount + framesToWait));
            return updateAwaiter;
        }

        public UnityAwaiter LateUpdates(UnityEngine.Object parent, int framesToWait)
        {
            lateUpdateAwaiter.AddContinuation(new FrameContinuation(parent, frameCount + framesToWait));
            return lateUpdateAwaiter;
        }

        public UnityAwaiter FixedUpdates(UnityEngine.Object parent, int stepsToWait)
        {
            fixedUpdateAwaiter.AddContinuation(new FixedContinuation(parent, fixedStepCount + (uint)stepsToWait));
            return fixedUpdateAwaiter;
        }

        public UnityAwaiter Seconds(UnityEngine.Object parent, float secondsToWait)
        {
            timeAwaiter.AddContinuation(new TimeContinuation(parent, time + secondsToWait));
            return timeAwaiter;
        }

        public UnityAwaiter SecondsUnscaled(UnityEngine.Object parent, float secondsToWait)
        {
            unscaledTimeAwaiter.AddContinuation(new UnscaledTimeContinuation(parent, unscaledTime + secondsToWait));
            return unscaledTimeAwaiter;
        }

        public UnityAwaiter Until(UnityEngine.Object parent, Func<bool> condition)
        {
            conditionAwaiter.AddContinuation(new ConditionContinuation(parent, condition));
            return conditionAwaiter;
        }

        public UnityAwaiter While(UnityEngine.Object parent, Func<bool> condition)
        {
            invertedConditionAwaiter.AddContinuation(new InvertedConditionContinuation(parent, condition));
            return invertedConditionAwaiter;
        }

        public UnityAwaiter Custom(UnityEngine.Object parent, CustomYieldInstruction instruction)
        {
            customAwaiter.AddContinuation(new CustomContinuation(parent, instruction));
            return customAwaiter;
        }

        public UnityAwaiter AsyncOp(UnityEngine.Object parent, AsyncOperation op)
        {
            asyncOpAwaiter.AddContinuation(new AsyncOpContinuation(parent, op));
            return asyncOpAwaiter;
        }
    }
}