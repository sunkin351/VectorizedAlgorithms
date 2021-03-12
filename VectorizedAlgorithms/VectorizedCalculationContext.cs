using System;
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;

namespace VectorizedAlgorithms
{
    public struct VectorizedCalculationContext
    {
        public readonly float[] X, Y, Z;
        public readonly Guid[] Guids;
        public readonly int Vector256Count;
        public int Vector128Count => Vector256Count * 2;

        public VectorizedCalculationContext(int elementCount)
        {
            int revisedElementCount = RoundUp(elementCount, Vector256<float>.Count);

            X = new float[revisedElementCount];
            Y = new float[revisedElementCount];
            Z = new float[revisedElementCount];
            Guids = null;

            Vector256Count = revisedElementCount / Vector256<float>.Count;
        }

        public void SetElements(int i, float x, float y, float z)
        {
            X[i] = x;
            Y[i] = y;
            Z[i] = z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Vector128<float> GetXVector128(int vectorIndex)
        {
            return ref GetVector128(X, vectorIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Vector128<float> GetYVector128(int vectorIndex)
        {
            return ref GetVector128(Y, vectorIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Vector128<float> GetZVector128(int vectorIndex)
        {
            return ref GetVector128(Z, vectorIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Vector256<float> GetXVector256(int vectorIndex)
        {
            return ref GetVector256(X, vectorIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Vector256<float> GetYVector256(int vectorIndex)
        {
            return ref GetVector256(Y, vectorIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Vector256<float> GetZVector256(int vectorIndex)
        {
            return ref GetVector256(Z, vectorIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref Vector128<float> GetVector128(float[] arr, int vectorIndex)
        {
            var elemIndex = vectorIndex * Vector128<float>.Count;

            return ref Unsafe.As<float, Vector128<float>>(ref arr[elemIndex]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref Vector256<float> GetVector256(float[] arr, int vectorIndex)
        {
            var elemIndex = vectorIndex * Vector256<float>.Count;

            return ref Unsafe.As<float, Vector256<float>>(ref arr[elemIndex]);
        }

        private static int RoundUp(int value, int alignment)
        {
            return (value + alignment - 1) / alignment * alignment;
        }
    }
}
