using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace VectorizedAlgorithms
{
    public static unsafe partial class Algorithms
    {
        public static unsafe void EnsureCompliantBools(Span<bool> input, Span<bool> output)
        {
            EnsureCompliantBools(MemoryMarshal.Cast<bool, byte>(input), MemoryMarshal.Cast<bool, byte>(output));
        }

        public static unsafe void EnsureCompliantBools(Span<byte> input, Span<byte> output)
        {
            if (input.IsEmpty)
                return;

            if (input.Length > output.Length)
            {
                throw new ArgumentException("Input buffer larger than output buffer");
            }

            fixed (byte* pInput = input, pOutput = output)
            {
                int i = 0;

                if (Sse2.IsSupported && output.Length >= Vector128<byte>.Count)
                {
                    var zero = Vector128<byte>.Zero;
                    var one = Vector128.Create((byte)1);
                    Vector128<byte> vec;

                    do
                    {
                        vec = Sse2.CompareEqual(zero, Sse2.LoadVector128(pInput + i));
                        vec = Sse2.AndNot(vec, one);
                        Sse2.Store(pOutput + i, vec);

                        i += Vector128<byte>.Count;
                    }
                    while (output.Length - i >= Vector128<byte>.Count);

                    if (i != output.Length)
                    {
                        i = output.Length - Vector128<byte>.Count;

                        vec = Sse2.CompareEqual(zero, Sse2.LoadVector128(pInput + i));
                        vec = Sse2.AndNot(vec, one);
                        Sse2.Store(pOutput + i, vec);
                    }

                    return;
                }

                while (i < output.Length)
                {
                    pOutput[i] = (byte)(pInput[i] != 0 ? 1 : 0);
                    i += 1;
                }
            }
        }
    }
}
