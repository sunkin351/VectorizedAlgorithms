using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using System.Diagnostics;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Toolkit.HighPerformance.Helpers;

namespace VectorizedAlgorithms
{
    [SimpleJob(RuntimeMoniker.NetCoreApp50), DisassemblyDiagnoser(maxDepth: 4)]
    public class ElonAbernathy_Project
    {
        readonly int seed = 1;
        [Params(500, 1000)]
        public int NumberOfPoints;
        [Params(100, 500)]
        public int NumberOfSegments;
        public Point[] Points { get; private set; }
        public LineSegment[] Segments { get; private set; }
        private VecPoint[] vecPoints;
        private VecSegment[] vecSegments;

        private VectorizedCalculationContext PointData;

        private readonly ParallelOptions _options = new ParallelOptions()
        {
            MaxDegreeOfParallelism = 12
        };

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.Points = GetPoints();
            this.Segments = GetSegments();
        }

        public void BenchmarkSetup()
        {
            NumberOfPoints = 200;
            NumberOfSegments = 100;
            this.Points = GetPoints();
            this.Segments = GetSegments();
        }

        public void Unit_Setup(Point[] pointData, LineSegment[] segmentData)
        {
            NumberOfPoints = pointData.Length;
            NumberOfSegments = segmentData.Length;

            this.Points = pointData;
            this.Segments = segmentData;

            PointData = new VectorizedCalculationContext(pointData.Length);

            for (int i = 0; i < pointData.Length; ++i)
            {
                var point = pointData[i];

                PointData.SetElements(i, point.X, point.Y, point.Z);
            }
        }

        private static Point GetRandomPoint(Random rng)
        {
            return new Point((rng.NextDouble() - 0.5) * 1000, (rng.NextDouble() - 0.5) * 1000, (rng.NextDouble() - 0.5) * 1000);
        }

        public Point[] GetPoints()
        {
            Point[] points = new Point[NumberOfPoints];

            vecPoints = new VecPoint[NumberOfPoints];

            PointData = new VectorizedCalculationContext(NumberOfPoints);

            Random random = new Random(seed);

            for (int i = 0; i < NumberOfPoints; i++)
            {
                Point point = GetRandomPoint(random);

                points[i] = point;

                vecPoints[i] = new Vector3(point.X, point.Y, point.Z);

                PointData.SetElements(i, point.X, point.Y, point.Z);
            }

            return points;
        }

        public LineSegment[] GetSegments()
        {
            LineSegment[] lineSegments = new LineSegment[NumberOfSegments];
            vecSegments = new VecSegment[NumberOfSegments];

            Random random = new Random(seed * 2); //Mersenne, take the wheel!
            for (int i = 0; i < NumberOfSegments; i++)
            {
                Point a = GetRandomPoint(random);
                Point b = GetRandomPoint(random);

                lineSegments[i] = new LineSegment(a, b);

                vecSegments[i] = new VecSegment(
                    new Vector3(a.X, a.Y, a.Z),
                    new Vector3(b.X, b.Y, b.Z)
                );
            }

            return lineSegments;
        }

        [Benchmark(Baseline = true)]
        public (int[] indices, float[] distances) Solution()
        {
            int[] indices = new int[NumberOfPoints];
            float[] distances = new float[NumberOfPoints];

            for (int i = 0; i < NumberOfPoints; ++i)
            {
                Vector3 point = Points[i];

                float distanceSq = float.MaxValue;
                int closestSegment = 0;

                for (int j = 0; j < Segments.Length; ++j)
                {
                    var tmp = DomainMathFunctions.GetClosestPointOnLine_ScalarMath(point, ref Segments[j]);

                    float tdist = Vector3.DistanceSquared(point, tmp);

                    if (distanceSq > tdist)
                    {
                        closestSegment = j;
                        distanceSq = tdist;
                    }
                }

                indices[i] = closestSegment;
                distances[i] = MathF.Sqrt(distanceSq);
            }

            return (indices, distances);
        }

        [Benchmark]
        public (int[] indices, float[] distances) VecSolution()
        {
            int[] indices = new int[NumberOfPoints];
            float[] distances = new float[NumberOfPoints];

            for (int i = 0; i < NumberOfPoints; ++i)
            {
                Vector3 point = Points[i];

                float distanceSq = float.MaxValue;
                int closestSegment = 0;

                for (int j = 0; j < Segments.Length; ++j)
                {
                    var tmp = DomainMathFunctions.GetClosestPointOnLine_VecMath(point, ref Segments[j]);

                    float tdist = Vector3.DistanceSquared(point, tmp);

                    if (distanceSq > tdist)
                    {
                        distanceSq = tdist;
                        closestSegment = j;
                    }
                }

                indices[i] = closestSegment;
                distances[i] = MathF.Sqrt(distanceSq);
            }

            return (indices, distances);
        }

        [Benchmark]
        public (int[] indices, float[] distances) IntrinsicSolution()
        {
            return PointData.SegmentsClosestToPoints(Segments);
        }

        [Benchmark]
        public ReadOnlySpan<float> Sse41_ParallelHelper_Solution()
        {
            if (!Sse41.IsSupported)
            {
                throw new PlatformNotSupportedException();
            }

            int elemCount = PointData.Vector128Count * Vector128<float>.Count;

            int[] indices = new int[elemCount];
            float[] distances = new float[elemCount];

            VectorizedCalculationContext.ParallelAction_Sse41 action = new(PointData, Segments, indices, distances);

            ParallelHelper.For(0, PointData.Vector128Count, in action, 4);

            return distances.AsSpan(0, NumberOfPoints);
        }

        [Benchmark]
        public ReadOnlySpan<float> Avx_ParallelHelper_Solution()
        {
            if (!Avx.IsSupported)
            {
                throw new PlatformNotSupportedException();
            }

            int elemCount = PointData.Vector256Count * Vector256<float>.Count;

            int[] indices = new int[elemCount];
            float[] distances = new float[elemCount];

            VectorizedCalculationContext.ParallelAction_Avx action = new(PointData, Segments, indices, distances);

            ParallelHelper.For(0, PointData.Vector256Count, in action, 2);

            return distances.AsSpan(0, NumberOfPoints);
        }
    }
}
