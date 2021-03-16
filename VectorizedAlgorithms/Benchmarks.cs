using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace VectorizedAlgorithms
{
    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    public class Benchmarks_CompliantBoolConversion
    {
        [Params(4, 12, 16, 86, 241)]
        public int ArrayLength;

        byte[] dataArray;

        [GlobalSetup]
        public void Setup()
        {
            var arr = dataArray = new byte[ArrayLength];

            var random = new Random();

            for (int i = 0; i < arr.Length; ++i)
            {
                arr[i] = (byte)random.Next(0, 3);
            }
        }

        [Benchmark]
        public void ToCompliantBools()
        {
            Algorithms.EnsureCompliantBools(dataArray, dataArray);
        }
    }
}
