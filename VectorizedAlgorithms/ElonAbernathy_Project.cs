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

namespace VectorizedAlgorithms
{
    [SimpleJob(RuntimeMoniker.NetCoreApp50), MemoryDiagnoser, DisassemblyDiagnoser(maxDepth: 2)]
    public class ElonAbernathy_Project
    {
        readonly int seed = 1;
        [Params(200, 500)]
        public int NumberOfPoints;
        [Params(100, 1000)]
        public int NumberOfSegments;
        public Point[] Points { get; private set; }
        public LineSegment[] Segments { get; private set; }
        private VecPoint[] vecPoints;
        private VecSegment[] vecSegments;

        private VectorizedCalculationContext PointData;

        private ParallelOptions _options = new ParallelOptions()
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
        public float[] Solution()
        {
            float[] result = new float[this.NumberOfPoints];

            for (int i = 0; i < NumberOfPoints; ++i)
            {
                Vector3 point = Points[i];

                Vector3 shortest = default;
                float distanceSq = float.MaxValue;

                for (int j = 0; j < Segments.Length; ++j)
                {
                    var tmp = DomainMathFunctions.GetClosestPointOnLine_ScalarMath(point, ref Segments[j]);

                    float tdist = Vector3.DistanceSquared(point, tmp);

                    if (distanceSq > tdist)
                    {
                        shortest = tmp;
                        distanceSq = tdist;
                    }
                }

                result[i] = Vector3.Distance(point, shortest);
            }

            return result;
        }

        [Benchmark]
        public float[] VecSolution()
        {
            float[] result = new float[this.NumberOfPoints];

            for (int i = 0; i < NumberOfPoints; ++i)
            {
                Vector3 point = Points[i];

                Vector3 shortest = default;
                float distanceSq = float.MaxValue;

                for (int j = 0; j < Segments.Length; ++j)
                {
                    var tmp = DomainMathFunctions.GetClosestPointOnLine_VecMath(point, ref Segments[j]);

                    float tdist = Vector3.DistanceSquared(point, tmp);

                    if (distanceSq > tdist)
                    {
                        shortest = tmp;
                        distanceSq = tdist;
                    }
                }

                result[i] = Vector3.Distance(point, shortest);
            }

            return result;
        }

        [Benchmark]
        public unsafe ReadOnlySpan<float> Sse41_Solution()
        {
            if (!Sse41.IsSupported)
            {
                throw new PlatformNotSupportedException();
            }

            float[] results = new float[PointData.Vector128Count * Vector128<float>.Count];

            for (int i = 0; i < PointData.Vector128Count; ++i)
            {
                Solution_Sse41_Impl(ref PointData, i, this.Segments, out Unsafe.As<float, Vector128<float>>(ref results[i * Vector128<float>.Count]));
            }

            return results.AsSpan(0, NumberOfPoints);
        }

        [Benchmark]
        public unsafe ReadOnlySpan<float> Avx2_Solution()
        {
            if (!Avx.IsSupported)
            {
                throw new PlatformNotSupportedException();
            }

            float[] results = new float[PointData.Vector256Count * Vector256<float>.Count];

            for (int i = 0; i < PointData.Vector256Count; ++i)
            {
                Solution_Avx_Impl(ref PointData, i, Segments, out Unsafe.As<float, Vector256<float>>(ref results[i * Vector256<float>.Count]));
            }

            return results.AsSpan(0, NumberOfPoints);
        }

        [Benchmark]
        public ReadOnlySpan<float> Sse41_Parallel_Solution()
        {
            if (!Sse41.IsSupported)
            {
                throw new PlatformNotSupportedException();
            }

            float[] results = new float[PointData.Vector128Count * Vector128<float>.Count];

            Parallel.For(0, PointData.Vector128Count, _options, index =>
            {
                Solution_Sse41_Impl(ref PointData, index, Segments, out Unsafe.As<float, Vector128<float>>(ref results[index * Vector128<float>.Count]));
            });

            return results.AsSpan(0, NumberOfPoints);
        }

        [Benchmark]
        public ReadOnlySpan<float> Avx2_Parallel_Solution()
        {
            if (!Avx.IsSupported)
            {
                throw new PlatformNotSupportedException();
            }

            float[] results = new float[PointData.Vector256Count * Vector256<float>.Count];

            Parallel.For(0, PointData.Vector256Count, _options, index =>
            {
                Solution_Avx_Impl(ref PointData, index, Segments, out Unsafe.As<float, Vector256<float>>(ref results[index * Vector256<float>.Count]));
            });

            return results.AsSpan(0, NumberOfPoints);
        }

        private static unsafe void Solution_Sse41_Impl(ref VectorizedCalculationContext context, int i, LineSegment[] segments, out Vector128<float> result)
        {
            Debug.Assert(i < context.Vector128Count);

            //Manual Fully controlled stack spill
            Vector128<float>* shortest = stackalloc Vector128<float>[4]
            {
                default,
                default,
                default,
                Vector128.Create(float.MaxValue)
            };

            // float x = point.X;
            // float y = point.Y;
            // float z = point.Z;
            var point_X = context.GetXVector128(i);
            var point_Y = context.GetYVector128(i);
            var point_Z = context.GetZVector128(i);

            for (int j = 0; j < segments.Length; ++j)
            {
                ref var segment = ref segments[j];

                Vector128<float> v0, v1, v2, v3, v4, v5;

                // float lox = lineSegment.A.X;
                // float loy = lineSegment.A.Y;
                // float loz = lineSegment.A.Z;
                v0 = Vector128.Create(segment.A.X);
                v1 = Vector128.Create(segment.A.Y);
                v2 = Vector128.Create(segment.A.Z);

                // float lx = lineSegment.Direction.X;
                // float ly = lineSegment.Direction.Y;
                // float lz = lineSegment.Direction.Z;
                v3 = Vector128.Create(segment.Direction.X);
                v4 = Vector128.Create(segment.Direction.Y);
                v5 = Vector128.Create(segment.Direction.Z);

                Vector128<float> v6, v7, v8, v9, v10, v11, v12;

                // float firstx = x - lox;
                // float firsty = y - loy;
                // float firstz = z - loz;
                v6 = Sse.Subtract(point_X, v0);
                v7 = Sse.Subtract(point_Y, v1);
                v8 = Sse.Subtract(point_Z, v2);

                // float t = (lx * firstx + ly * firsty + lz * firstz) / segment.DirectionDot;
                v6 = Sse.Multiply(v3, v6);
                v6 = Helper_MultiplyAdd(v4, v7, v6);
                v6 = Helper_MultiplyAdd(v5, v8, v6);

                v7 = Vector128.Create(segment.DirectionDot);

                v6 = Sse.Divide(v6, v7);

                // float xx = lox + t * lx;
                // float yy = loy + t * ly;
                // float zz = loz + t * lz;
                // Point intersectionPoint = new Point(xx, yy, zz);
                v3 = Helper_MultiplyAdd(v6, v3, v0);
                v4 = Helper_MultiplyAdd(v6, v4, v1);
                v5 = Helper_MultiplyAdd(v6, v5, v2);

                // bool isOnLineSegment = Math.Max(Vector3.Distance(lineSegment.A, intersectionPoint), Vector3.Distance(intersectionPoint, lineSegment.B)) < lineSegment.Length;
                // Vector3.Distance(lineSegment.A, lineSegment.B)
                v6 = Vector128.Create(segment.Length);

                // distance from A to Intersect
                v7 = Sse.Subtract(v0, v3);
                v7 = Sse.Multiply(v7, v7);
                v8 = Sse.Subtract(v1, v4);
                v7 = Helper_MultiplyAdd(v8, v8, v7);
                v8 = Sse.Subtract(v2, v5);
                v7 = Helper_MultiplyAdd(v8, v8, v7);
                v7 = Sse.Sqrt(v7);

                // distance from intersect to B
                v0 = Vector128.Create(segment.B.X);
                v1 = Vector128.Create(segment.B.Y);
                v2 = Vector128.Create(segment.B.Z);

                v8 = Sse.Subtract(v3, v0);
                v8 = Sse.Multiply(v8, v8);
                v9 = Sse.Subtract(v4, v1);
                v8 = Helper_MultiplyAdd(v9, v9, v8);
                v9 = Sse.Subtract(v5, v2);
                v8 = Helper_MultiplyAdd(v9, v9, v8);
                v8 = Sse.Sqrt(v8);

                //Max of distances
                v7 = Sse.Max(v7, v8);

                //Compare max distances to segment length
                v6 = Sse.CompareLessThan(v7, v6);

                /*
                if (isOnLineSegment)
                {
                    return new Point(xx, yy, zz);
                }
                else
                {
                    if (Point.Distance(point, lineSegment.A) < Point.Distance(point, lineSegment.B))
                    {
                        return lineSegment.A;
                    }
                    else
                    {
                        return lineSegment.B;
                    }
                }
                */
                v7 = Vector128.Create(segment.A.X);
                v8 = Vector128.Create(segment.A.Y);
                v9 = Vector128.Create(segment.A.Z);

                v10 = Sse.Subtract(point_X, v7);
                v10 = Sse.Multiply(v10, v10);
                v11 = Sse.Subtract(point_Y, v8);
                v10 = Helper_MultiplyAdd(v11, v11, v10);
                v11 = Sse.Subtract(point_Z, v9);
                v10 = Helper_MultiplyAdd(v11, v11, v10);

                v11 = Sse.Subtract(point_X, v0);
                v11 = Sse.Multiply(v11, v11);
                v12 = Sse.Subtract(point_Y, v1);
                v11 = Helper_MultiplyAdd(v12, v12, v11);
                v12 = Sse.Subtract(point_Z, v2);
                v11 = Helper_MultiplyAdd(v12, v12, v11);

                v10 = Sse.CompareLessThan(v10, v11);
                v0 = Sse41.BlendVariable(v0, v7, v10); // Only after reaching 16 variables can I compress 9 of them into 3
                v1 = Sse41.BlendVariable(v1, v8, v10);
                v2 = Sse41.BlendVariable(v2, v9, v10);
                v0 = Sse41.BlendVariable(v0, v3, v6);
                v1 = Sse41.BlendVariable(v1, v4, v6);
                v2 = Sse41.BlendVariable(v2, v5, v6);

                // float tdist = Vector3.DistanceSquared(point.Vec3, tmp);
                v4 = Sse.Subtract(point_X, v0);
                v4 = Sse.Multiply(v4, v4);
                v5 = Sse.Subtract(point_Y, v1);
                v4 = Helper_MultiplyAdd(v5, v5, v4);
                v5 = Sse.Subtract(point_Z, v2);
                v4 = Helper_MultiplyAdd(v5, v5, v4);

                // if (distanceSq > tdist)
                // {
                //     shortest = tmp;
                //     distanceSq = tdist;
                // }
                v5 = shortest[3];
                v6 = Sse.CompareGreaterThan(v5, v4);
                shortest[3] = Sse41.BlendVariable(v5, v4, v6);

                shortest[0] = Sse41.BlendVariable(shortest[0], v0, v6);
                shortest[1] = Sse41.BlendVariable(shortest[1], v1, v6);
                shortest[2] = Sse41.BlendVariable(shortest[2], v2, v6);
            }

            result = Sse.Sqrt(shortest[3]);
        }

        private static unsafe void Solution_Avx_Impl(ref VectorizedCalculationContext context, int i, LineSegment[] segments, out Vector256<float> result)
        {
            Debug.Assert(i < context.Vector256Count);

            //Manual Fully controlled stack spill
            Vector256<float>* shortest = stackalloc Vector256<float>[4]
            {
                default,
                default,
                default,
                Vector256.Create(float.MaxValue)
            };

            // float x = point.X;
            // float y = point.Y;
            // float z = point.Z;
            var point_X = context.GetXVector256(i);
            var point_Y = context.GetYVector256(i);
            var point_Z = context.GetZVector256(i);

            for (int j = 0; j < segments.Length; ++j)
            {
                ref var segment = ref segments[j];

                Vector256<float> v0, v1, v2, v3, v4, v5;

                // float lox = lineSegment.A.X;
                // float loy = lineSegment.A.Y;
                // float loz = lineSegment.A.Z;
                v0 = Vector256.Create(segment.A.X);
                v1 = Vector256.Create(segment.A.Y);
                v2 = Vector256.Create(segment.A.Z);

                // float lx = lineSegment.Direction.X;
                // float ly = lineSegment.Direction.Y;
                // float lz = lineSegment.Direction.Z;
                v3 = Vector256.Create(segment.Direction.X);
                v4 = Vector256.Create(segment.Direction.Y);
                v5 = Vector256.Create(segment.Direction.Z);

                Vector256<float> v6, v7, v8, v9, v10, v11, v12;

                // float firstx = x - lox;
                // float firsty = y - loy;
                // float firstz = z - loz;
                v6 = Avx.Subtract(point_X, v0);
                v7 = Avx.Subtract(point_Y, v1);
                v8 = Avx.Subtract(point_Z, v2);

                // float t = (lx * firstx + ly * firsty + lz * firstz) / segment.DirectionDot;
                v6 = Avx.Multiply(v3, v6);
                v6 = Helper_MultiplyAdd(v4, v7, v6);
                v6 = Helper_MultiplyAdd(v5, v8, v6);

                v7 = Vector256.Create(segment.DirectionDot);

                v6 = Avx.Divide(v6, v7);

                // float xx = lox + t * lx;
                // float yy = loy + t * ly;
                // float zz = loz + t * lz;
                // Point intersectionPoint = new Point(xx, yy, zz);
                v3 = Helper_MultiplyAdd(v6, v3, v0);
                v4 = Helper_MultiplyAdd(v6, v4, v1);
                v5 = Helper_MultiplyAdd(v6, v5, v2);

                // bool isOnLineSegment = Math.Max(Vector3.Distance(lineSegment.A, intersectionPoint), Vector3.Distance(intersectionPoint, lineSegment.B)) < lineSegment.Length;
                // Vector3.Distance(lineSegment.A, lineSegment.B)
                v6 = Vector256.Create(segment.Length);

                // distance from A to Intersect
                v7 = Avx.Subtract(v0, v3);
                v7 = Avx.Multiply(v7, v7);
                v8 = Avx.Subtract(v1, v4);
                v7 = Helper_MultiplyAdd(v8, v8, v7);
                v8 = Avx.Subtract(v2, v5);
                v7 = Helper_MultiplyAdd(v8, v8, v7);
                v7 = Avx.Sqrt(v7);

                // distance from intersect to B
                v0 = Vector256.Create(segment.B.X);
                v1 = Vector256.Create(segment.B.Y);
                v2 = Vector256.Create(segment.B.Z);

                v8 = Avx.Subtract(v3, v0);
                v8 = Avx.Multiply(v8, v8);
                v9 = Avx.Subtract(v4, v1);
                v8 = Helper_MultiplyAdd(v9, v9, v8);
                v9 = Avx.Subtract(v5, v2);
                v8 = Helper_MultiplyAdd(v9, v9, v8);
                v8 = Avx.Sqrt(v8);

                //Max of distances
                v7 = Avx.Max(v7, v8);

                //Compare max distances to segment length
                v6 = Avx.CompareLessThan(v7, v6);

                /*
                if (isOnLineSegment)
                {
                    return new Point(xx, yy, zz);
                }
                else
                {
                    if (Point.Distance(point, lineSegment.A) < Point.Distance(point, lineSegment.B))
                    {
                        return lineSegment.A;
                    }
                    else
                    {
                        return lineSegment.B;
                    }
                }
                */
                v7 = Vector256.Create(segment.A.X);
                v8 = Vector256.Create(segment.A.Y);
                v9 = Vector256.Create(segment.A.Z);

                v10 = Avx.Subtract(point_X, v7);
                v10 = Avx.Multiply(v10, v10);
                v11 = Avx.Subtract(point_Y, v8);
                v10 = Helper_MultiplyAdd(v11, v11, v10);
                v11 = Avx.Subtract(point_Z, v9);
                v10 = Helper_MultiplyAdd(v11, v11, v10);

                v11 = Avx.Subtract(point_X, v0);
                v11 = Avx.Multiply(v11, v11);
                v12 = Avx.Subtract(point_Y, v1);
                v11 = Helper_MultiplyAdd(v12, v12, v11);
                v12 = Avx.Subtract(point_Z, v2);
                v11 = Helper_MultiplyAdd(v12, v12, v11);

                v10 = Avx.CompareLessThan(v10, v11);
                v0 = Avx.BlendVariable(v0, v7, v10); // Only after reaching 16 variables can I compress 9 of them into 3
                v1 = Avx.BlendVariable(v1, v8, v10);
                v2 = Avx.BlendVariable(v2, v9, v10);
                v0 = Avx.BlendVariable(v0, v3, v6);
                v1 = Avx.BlendVariable(v1, v4, v6);
                v2 = Avx.BlendVariable(v2, v5, v6);

                // float tdist = Vector3.DistanceSquared(point.Vec3, tmp);
                v4 = Avx.Subtract(point_X, v0);
                v4 = Avx.Multiply(v4, v4);
                v5 = Avx.Subtract(point_Y, v1);
                v4 = Helper_MultiplyAdd(v5, v5, v4);
                v5 = Avx.Subtract(point_Z, v2);
                v4 = Helper_MultiplyAdd(v5, v5, v4);

                // if (distanceSq > tdist)
                // {
                //     shortest = tmp;
                //     distanceSq = tdist;
                // }
                v5 = shortest[3];
                v6 = Avx.CompareGreaterThan(v5, v4);
                shortest[3] = Avx.BlendVariable(v5, v4, v6);

                shortest[0] = Avx.BlendVariable(shortest[0], v0, v6);
                shortest[1] = Avx.BlendVariable(shortest[1], v1, v6);
                shortest[2] = Avx.BlendVariable(shortest[2], v2, v6);
            }

            result = Avx.Sqrt(shortest[3]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<float> Helper_MultiplyAdd(Vector128<float> a, Vector128<float> b, Vector128<float> c)
        {
            if (Fma.IsSupported)
            {
                return Fma.MultiplyAdd(a, b, c);
            }
            else
            {
                return Sse.Add(Sse.Multiply(a, b), c);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> Helper_MultiplyAdd(Vector256<float> a, Vector256<float> b, Vector256<float> c)
        {
            if (Fma.IsSupported)
            {
                return Fma.MultiplyAdd(a, b, c);
            }
            else
            {
                return Avx.Add(Avx.Multiply(a, b), c);
            }
        }

        private static float Sum(ReadOnlySpan<float> span)
        {
            float sum = 0;

            for (int i = 0; i < span.Length; ++i)
            {
                sum += span[i];
            }

            return sum;
        }
    }
}
