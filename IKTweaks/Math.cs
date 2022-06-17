using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace IKTweaks
{
    // il2cpp calls that return value types are expensive - therefore don't use them
    [StructLayout(LayoutKind.Sequential)]
    internal struct Float3
    {
        public float X;
        public float Y;
        public float Z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Float3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector3(Float3 a)
        {
            unsafe
            {
                return *(Vector3*)&a;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Float3(Vector3 a)
        {
            unsafe
            {
                return *(Float3*)&a;
            }
        }

        public float sqrMagnitude
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Dot(this, this);
        }
        
        public float magnitude
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Mathf2.Sqrt(sqrMagnitude);
        }

        public Float3 normalized
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var length = magnitude;
                if (length <= Mathf2.Epsilon) return default;
                return this / length;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float3 operator +(Float3 a, Float3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float3 operator +(Vector3 a, Float3 b) => new(a.x + b.X, a.y + b.Y, a.z + b.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float3 operator +(Float3 a, Vector3 b) => new(a.X + b.z, a.Y + b.y, a.Z + b.z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float3 operator -(Float3 a, Float3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float3 operator -(Float3 a, Vector3 b) => new(a.X - b.x, a.Y - b.y, a.Z - b.z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float3 operator -(Vector3 a, Float3 b) => new(a.x - b.X, a.y - b.Y, a.z - b.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float3 operator *(Float3 a, float b) => new(a.X * b, a.Y * b, a.Z * b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float3 operator *(float b, Float3 a) => new(a.X * b, a.Y * b, a.Z * b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float3 operator /(Float3 a, float b) => new(a.X / b, a.Y / b, a.Z / b);

        public static readonly Float3 right = new(1, 0, 0);
        public static readonly Float3 forward = new(0, 0, 1);
        public static readonly Float3 down = new(0, -1, 0);
        public static readonly Float3 zero = new(0, 0, 0);
        public static readonly Float3 one = new(1, 1, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(Float3 a, Float3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float3 Cross(Float3 a, Float3 b) => new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float3 ProjectOnPlane(Float3 a, Float3 planeNormal)
        {
            planeNormal = planeNormal.normalized;
            return a - planeNormal * Dot(planeNormal, a);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float3 Lerp(Float3 a, Float3 b, float t) => a * (1 - t) + b * t;

        public override string ToString()
        {
            return $"({X:0.####}, {Y:0.####}, {Z:0.####})";
        }
    }
    
    [StructLayout(LayoutKind.Sequential)]
    internal struct Quat
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quat(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quat(Float3 v, float w)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
            W = w;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Quaternion(Quat a)
        {
            unsafe
            {
                return *(Quaternion*)&a;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Quat(Quaternion a)
        {
            unsafe
            {
                return *(Quat*)&a;
            }
        }

        
        private Float3 Vec
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(X, Y, Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quat Inverse(Quat a) => new(-a.X, -a.Y, -a.Z, a.W);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quat operator *(Quat a, Quat b) => new(Float3.Cross(a.Vec, b.Vec) + a.W * b.Vec + b.W * a.Vec, a.W * b.W - Float3.Dot(a.Vec, b.Vec));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Float3 operator *(Quat a, Float3 b) => (a * new Quat(b, 0) * Inverse(a)).Vec;

        public static readonly Quat identity = new(0, 0, 0, 1);
    }

    struct FakeVirtualBone
    {
        public Quat solverRotation;
        public Quat readRotation;
        public Float3 solverPosition;
        public Float3 readPosition;

        public FakeVirtualBone(Float3 solverPosition, Quat solverRotation)
        {
            this.solverPosition = readPosition = solverPosition;
            this.solverRotation = readRotation = solverRotation;
        }
    }
    
    public static class Mathf2
    {
        public const float PI = 3.141593f;
        public const float Deg2Rad = 0.01745329f;
        public const float Rad2Deg = 57.29578f;
        public const float Epsilon = 1.175494E-38f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Lerp(float a, float b, float t) => a * (1 - t) + b * t;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp01(float a)
        {
            if (a < 0) return 0;
            if (a > 1) return 1;
            return a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sign(float a)
        {
            if (a < 0) return -1;
            if (a > 0) return 1;
            return 0;
        }

        // Using double variants is probably still faster than il2cpp call
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float Acos(float a) => (float)Math.Acos(a);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float Asin(float a) => (float)Math.Asin(a);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float Sqrt(float a) => (float)Math.Sqrt(a);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float Pow(float a, float p) => (float)Math.Pow(a, p);
    }
}