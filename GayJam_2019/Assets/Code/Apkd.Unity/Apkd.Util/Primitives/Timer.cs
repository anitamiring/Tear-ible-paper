using System;
using static UnityEngine.Time;

namespace Apkd
{
    public struct Timer
    {
        float time;

        public Timer(float offsetSeconds)
            => time = unscaledTime + offsetSeconds;

        public Timer(TimeSpan offset)
            => time = unscaledTime + (float)offset.TotalSeconds;

        /// <summary> Check whether the timer has been running for more than given duration and optionally reset the timer. </summary>
        public bool Test(float seconds, bool reset = true)
        {
            bool result = Seconds > seconds;

            if (result && reset)
                Reset();

            return result;
        }

        public void Reset()
            => time = unscaledTime;

        /// <summary> Seconds since the timer was created or reset. </summary>
        public float Seconds
            => unscaledTime - time;

        /// <summary> TimeSpan measuring the time since the timer was created or reset. </summary>
        public TimeSpan TimeSpan
            => new TimeSpan(0, 0, 0, 0, (int)(Seconds * 1000));

        /// <summary> Create a new timer initialized with current time. </summary>
        public static Timer Create()
            => new Timer(offsetSeconds: 0);

        public static implicit operator float(Timer timer)
            => timer.Seconds;
    }
}