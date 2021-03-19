using System;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace VectorizedAlgorithms
{
    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    public class Benchmarks_CompliantBoolConversion
    {
        [Params(4, 12, 16, 86, 241)]
        public int ArrayLength;

        bool[] bools;
        bool[] boolsDest;

        byte[] UInt8Array;
        ushort[] UInt16Array;
        uint[] UInt32Array;

        [GlobalSetup]
        public void Setup()
        {
            bools = new bool[ArrayLength];
            boolsDest = new bool[ArrayLength];

            var random = new Random();

            foreach (ref var b in bools.AsSpan())
            {
                Unsafe.As<bool, byte>(ref b) = (byte)random.Next(0, 3);
            }

            UInt8Array = new byte[ArrayLength];
            UInt16Array = new ushort[ArrayLength];
            UInt32Array = new uint[ArrayLength];
        }

        [Benchmark]
        public void ToCompliantBools()
        {
            Algorithms.EnsureCompliantBools(bools, boolsDest);
        }

        [Benchmark]
        public int IndexOf_Byte()
        {
            return Algorithms.IndexOf(UInt8Array, 1);
        }

        [Benchmark]
        public int IndexOf_UInt16()
        {
            return Algorithms.IndexOf(UInt16Array, 1);
        }

        [Benchmark]
        public int IndexOf_UInt32()
        {
            return Algorithms.IndexOf(UInt32Array, 1);
        }
    }
}
