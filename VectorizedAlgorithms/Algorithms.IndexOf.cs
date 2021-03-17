using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace VectorizedAlgorithms
{
    public static unsafe partial class Algorithms
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf(ReadOnlySpan<uint> span, uint value)
        {
            if (Avx2.IsSupported)
            {
                return AVX2(span, value);
            }

            if (Sse2.IsSupported)
            {
                return SSE2(span, value);
            }

            return span.IndexOf(value);

            static int AVX2(ReadOnlySpan<uint> span, uint value)
            {
                if (span.IsEmpty)
                    return -1;

                if (span[0] == value)
                    return 0;

                int i = 0, x;

                fixed (uint* data = span)
                {
                    if (span.Length >= Vector256<uint>.Count)
                    {
                        Vector256<uint> valueVec = Vector256.Create(value);

                        do
                        {
                            Vector256<uint> comp = Avx2.CompareEqual(valueVec, Avx.LoadVector256(data + i));
                            x = Avx2.MoveMask(comp.AsByte());

                            if (x != 0)
                            {
                                goto SimdCalculateIndex;
                            }

                            i += Vector256<uint>.Count;
                        }
                        while (span.Length - i >= Vector256<uint>.Count);

                        if (span.Length != i)
                        {
                            i = span.Length - Vector256<uint>.Count;

                            Vector256<uint> comp = Avx2.CompareEqual(valueVec, Avx.LoadVector256(data + i));
                            x = Avx2.MoveMask(comp.AsByte());

                            if (x != 0)
                            {
                                goto SimdCalculateIndex;
                            }
                        }

                        return -1;
                    }

                    while (i < span.Length)
                    {
                        if (data[i] == value)
                            return i;

                        i += 1;
                    }

                    return -1;

                SimdCalculateIndex:
                    return CalculateIndex(i, x);
                }
            }

            static int SSE2(ReadOnlySpan<uint> span, uint value)
            {
                if (span.IsEmpty)
                    return -1;

                if (span[0] == value)
                    return 0;

                int i = 0, x;

                fixed (uint* data = span)
                {
                    if (span.Length >= Vector128<uint>.Count)
                    {
                        Vector128<uint> valueVec = Vector128.Create(value);

                        do
                        {
                            Vector128<uint> comp = Sse2.CompareEqual(valueVec, Sse2.LoadVector128(data + i));
                            x = Sse2.MoveMask(comp.AsByte());

                            if (x != 0)
                            {
                                goto SimdCalculateIndex;
                            }

                            i += Vector128<uint>.Count;
                        }
                        while (span.Length - i >= Vector128<uint>.Count);

                        if (span.Length != i)
                        {
                            i = span.Length - Vector128<uint>.Count;

                            Vector128<uint> comp = Sse2.CompareEqual(valueVec, Sse2.LoadVector128(data + i));
                            x = Sse2.MoveMask(comp.AsByte());

                            if (x != 0)
                            {
                                goto SimdCalculateIndex;
                            }
                        }

                        return -1;
                    }

                    while (i < span.Length)
                    {
                        if (data[i] == value)
                            return i;

                        i += 1;
                    }

                    return -1;

                SimdCalculateIndex:
                    return CalculateIndex(i, x);
                }
            }

            static int CalculateIndex(int idx, int mask)
            {
                return idx + (BitOperations.TrailingZeroCount(mask) / sizeof(uint));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf(ReadOnlySpan<ushort> span, ushort value)
        {
            if (Avx2.IsSupported)
            {
                return AVX2(span, value);
            }

            if (Sse2.IsSupported)
            {
                return SSE2(span, value);
            }

            return span.IndexOf(value);

            static int AVX2(ReadOnlySpan<ushort> span, ushort value)
            {
                if (span.IsEmpty)
                    return -1;

                if (span[0] == value)
                    return 0;

                int i = 0, x;

                fixed (ushort* data = span)
                {
                    if (span.Length >= Vector256<ushort>.Count)
                    {
                        Vector256<ushort> valueVec = Vector256.Create(value);

                        do
                        {
                            Vector256<ushort> comp = Avx2.CompareEqual(valueVec, Avx.LoadVector256(data + i));
                            x = Avx2.MoveMask(comp.AsByte());

                            if (x != 0)
                            {
                                goto SimdCalculateIndex;
                            }

                            i += Vector256<ushort>.Count;
                        }
                        while (span.Length - i >= Vector256<ushort>.Count);

                        if (span.Length != i)
                        {
                            i = span.Length - Vector256<ushort>.Count;

                            Vector256<ushort> comp = Avx2.CompareEqual(valueVec, Avx.LoadVector256(data + i));
                            x = Avx2.MoveMask(comp.AsByte());

                            if (x != 0)
                            {
                                goto SimdCalculateIndex;
                            }
                        }

                        return -1;
                    }

                    while (i < span.Length)
                    {
                        if (data[i] == value)
                            return i;

                        i += 1;
                    }

                    return -1;

                SimdCalculateIndex:
                    return CalculateIndex(i, x);
                }
            }

            static int SSE2(ReadOnlySpan<ushort> span, ushort value)
            {
                if (span.IsEmpty)
                    return -1;

                if (span[0] == value)
                    return 0;

                int i = 0, x;

                fixed (ushort* data = span)
                {
                    if (span.Length >= Vector128<ushort>.Count)
                    {
                        Vector128<ushort> valueVec = Vector128.Create(value);

                        do
                        {
                            Vector128<ushort> comp = Sse2.CompareEqual(valueVec, Sse2.LoadVector128(data + i));
                            x = Sse2.MoveMask(comp.AsByte());

                            if (x != 0)
                            {
                                goto SimdCalculateIndex;
                            }

                            i += Vector128<ushort>.Count;
                        }
                        while (span.Length - i >= Vector128<ushort>.Count);

                        if (span.Length != i)
                        {
                            i = span.Length - Vector128<ushort>.Count;

                            Vector128<ushort> comp = Sse2.CompareEqual(valueVec, Sse2.LoadVector128(data + i));
                            x = Sse2.MoveMask(comp.AsByte());

                            if (x != 0)
                            {
                                goto SimdCalculateIndex;
                            }
                        }

                        return -1;
                    }

                    while (i < span.Length)
                    {
                        if (data[i] == value)
                            return i;

                        i += 1;
                    }

                    return -1;

                SimdCalculateIndex:
                    return CalculateIndex(i, x);
                }
            }

            static int CalculateIndex(int idx, int mask)
            {
                return idx + (BitOperations.TrailingZeroCount(mask) / sizeof(ushort));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf(ReadOnlySpan<byte> span, byte value)
        {
            if (Avx2.IsSupported)
            {
                return AVX2(span, value);
            }

            if (Sse2.IsSupported)
            {
                return SSE2(span, value);
            }

            return span.IndexOf(value);

            static int AVX2(ReadOnlySpan<byte> span, byte value)
            {
                if (span.IsEmpty)
                    return -1;

                if (span[0] == value)
                    return 0;

                int i = 0, x;

                fixed (byte* data = span)
                {
                    if (span.Length >= Vector256<byte>.Count)
                    {
                        Vector256<byte> valueVec = Vector256.Create(value);

                        do
                        {
                            Vector256<byte> comp = Avx2.CompareEqual(valueVec, Avx.LoadVector256(data + i));
                            x = Avx2.MoveMask(comp.AsByte());

                            if (x != 0)
                            {
                                goto SimdCalculateIndex;
                            }

                            i += Vector256<byte>.Count;
                        }
                        while (span.Length - i >= Vector256<byte>.Count);

                        if (span.Length != i)
                        {
                            i = span.Length - Vector256<byte>.Count;

                            Vector256<byte> comp = Avx2.CompareEqual(valueVec, Avx.LoadVector256(data + i));
                            x = Avx2.MoveMask(comp.AsByte());

                            if (x != 0)
                            {
                                goto SimdCalculateIndex;
                            }
                        }

                        return -1;
                    }

                    while (i < span.Length)
                    {
                        if (data[i] == value)
                            return i;

                        i += 1;
                    }

                    return -1;

                SimdCalculateIndex:
                    return CalculateIndex(i, x);
                }
            }

            static int SSE2(ReadOnlySpan<byte> span, byte value)
            {
                if (span.IsEmpty)
                    return -1;

                if (span[0] == value)
                    return 0;

                int i = 0, x;

                fixed (byte* data = span)
                {
                    if (span.Length >= Vector128<byte>.Count)
                    {
                        Vector128<byte> valueVec = Vector128.Create(value);

                        do
                        {
                            Vector128<byte> comp = Sse2.CompareEqual(valueVec, Sse2.LoadVector128(data + i));
                            x = Sse2.MoveMask(comp.AsByte());

                            if (x != 0)
                            {
                                goto SimdCalculateIndex;
                            }

                            i += Vector128<byte>.Count;
                        }
                        while (span.Length - i >= Vector128<byte>.Count);

                        if (span.Length != i)
                        {
                            i = span.Length - Vector128<byte>.Count;

                            Vector128<byte> comp = Sse2.CompareEqual(valueVec, Sse2.LoadVector128(data + i));
                            x = Sse2.MoveMask(comp.AsByte());

                            if (x != 0)
                            {
                                goto SimdCalculateIndex;
                            }
                        }

                        return -1;
                    }

                    while (i < span.Length)
                    {
                        if (data[i] == value)
                            return i;

                        i += 1;
                    }

                    return -1;

                SimdCalculateIndex:
                    return CalculateIndex(i, x);
                }
            }

            static int CalculateIndex(int idx, int mask)
            {
                return idx + (BitOperations.TrailingZeroCount(mask) / sizeof(byte));
            }
        }
    }
}
