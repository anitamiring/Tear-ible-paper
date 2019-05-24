using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace Apkd
{
    [Serializable]
#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.InlineProperty]
#endif
    public class SmoothVector3
    {
        public float SmoothTime;
        public float MaxSpeed;
        Vector3 velocity;

        public Vector3 Value { get; set; }
        public Vector3 Velocity => velocity;

        public void ResetVelocity() => velocity = default;

        public SmoothVector3(float smoothTime, float maxSpeed = float.PositiveInfinity, Vector3 initialValue = default(Vector3))
        {
            this.Value = initialValue;
            this.velocity = default(Vector3);
            this.SmoothTime = smoothTime;
            this.MaxSpeed = maxSpeed;
        }

        public Vector3 Update(Vector3 target)
            => Value = Vector3.SmoothDamp(Value, target, ref velocity, SmoothTime, MaxSpeed);

        public Vector3 Update(Vector3 target, float smoothTime)
            => Value = Vector3.SmoothDamp(Value, target, ref velocity, smoothTime, MaxSpeed);

        public Vector3 Update(Vector3 target, float smoothTime = float.NaN, float maxSpeed = float.NaN)
            => Value = Vector3.SmoothDamp(
                current: Value,
                target: target,
                currentVelocity: ref velocity,
                smoothTime: float.IsNaN(smoothTime) ? SmoothTime : smoothTime,
                maxSpeed: float.IsNaN(maxSpeed) ? MaxSpeed : maxSpeed);

        public static implicit operator Vector3(SmoothVector3 v) => v.Value;

        public static implicit operator SmoothVector3(float smoothTime) => new SmoothVector3(smoothTime);

        public static Vector3 operator *(SmoothVector3 v, float f) => v.Value * f;

        public static Vector3 operator *(float f, SmoothVector3 v) => v.Value * f;
    }

    [Serializable]
#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.InlineProperty]
#endif
    public class SmoothFloat
    {
        public float SmoothTime;
        public float MaxSpeed;
        float velocity;

        public float Value { get; set; }
        public float Velocity => velocity;

        public void ResetVelocity() => velocity = default;

        public SmoothFloat(float smoothTime, float maxSpeed = float.PositiveInfinity, float initialValue = default(float))
        {
            this.Value = initialValue;
            this.velocity = default(float);
            this.SmoothTime = smoothTime;
            this.MaxSpeed = maxSpeed;
        }

        public float Update(float target)
            => Value = Mathf.SmoothDamp(Value, target, ref velocity, SmoothTime, MaxSpeed);

        public float Update(float target, float smoothTime)
            => Value = Mathf.SmoothDamp(Value, target, ref velocity, smoothTime, MaxSpeed);

        public float Update(float target, float smoothTime = float.NaN, float maxSpeed = float.NaN)
            => Value = Mathf.SmoothDamp(
                current: Value,
                target: target,
                currentVelocity: ref velocity,
                smoothTime: float.IsNaN(smoothTime) ? SmoothTime : smoothTime,
                maxSpeed: float.IsNaN(maxSpeed) ? MaxSpeed : maxSpeed);

        public static implicit operator float(SmoothFloat f) => f.Value;

        public static implicit operator SmoothFloat(float smoothTime) => new SmoothFloat(smoothTime);
    }

    [Serializable]
#if ODIN_INSPECTOR
    [Sirenix.OdinInspector.InlineProperty]
#endif
    public class SmoothQuaternion
    {
        public float SmoothTime;
        Quaternion velocity;

        public Quaternion Value { get; set; }
        public Quaternion Velocity => velocity;

        public void ResetVelocity() => velocity = default;

        public SmoothQuaternion(float smoothTime, Quaternion initialValue = default(Quaternion))
        {
            this.Value = initialValue;
            this.velocity = default(Quaternion);
            this.SmoothTime = smoothTime;
        }

        public Quaternion Update(Quaternion target)
            => Value = Value.SmoothDamp(target, ref velocity, SmoothTime);

        public Quaternion Update(Quaternion target, float smoothTime)
            => Value = Value.SmoothDamp(target, ref velocity, smoothTime);

        public static implicit operator Quaternion(SmoothQuaternion q) => q.Value;

        public static implicit operator SmoothQuaternion(float smoothTime) => new SmoothQuaternion(smoothTime);
    }
}