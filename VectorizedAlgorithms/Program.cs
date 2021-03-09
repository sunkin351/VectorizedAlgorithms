using System;
using BenchmarkDotNet.Running;

namespace VectorizedAlgorithms
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<ElonAbernathy_Project>();
        }
    }
}
