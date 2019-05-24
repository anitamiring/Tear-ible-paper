using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Apkd.Internal
{
    /// <summary>
    /// Base class for moving average implementations.
    /// </summary>
    /// <typeparam name="T">Type of the averaged value.</typeparam>
    public abstract class MovingAverage<T>
    {
        protected Queue<T> buffer;
        int length;

        public MovingAverage(int size = 4)
        {
            buffer = new Queue<T>(length);
            length = size;
        }

        /// <summary> Push a new value into the moving average buffer. </summary>
        public void Push(T value)
        {
            if (buffer.Count >= length)
                buffer.Dequeue();

            buffer.Enqueue(value);
        }

        /// <summary> Calculate the moving average from the values in the buffer. </summary>
        public abstract T GetAverage();
    }
}

namespace Apkd
{
    /// <summary> Moving average for a series of <see cref="float"/> values. </summary>
    public sealed class MovingAverage : Internal.MovingAverage<float>
    {
        public MovingAverage(int size = 4) : base(size) { }

        /// <summary> Calculate the moving average from the values in the buffer. </summary>
        public override float GetAverage()
        {
            float avg = 0;
            foreach (var item in buffer)
                avg += item;
            return avg / buffer.Count;
        }
    }

    /// <summary> Moving average for a series of <see cref="Vector3"/> values. </summary>
    public sealed class MovingAverageVector3 : Internal.MovingAverage<Vector3>
    {
        public MovingAverageVector3(int size = 4) : base(size) { }

        /// <summary> Calculate the moving average from the values in the buffer. </summary>
        public override Vector3 GetAverage()
        {
            Vector3 avg = default(Vector3);
            foreach (var item in buffer)
                avg += item;
            return avg / buffer.Count;
        }
    }
}