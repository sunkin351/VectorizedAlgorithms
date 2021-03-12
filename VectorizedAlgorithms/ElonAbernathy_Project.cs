using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace VectorizedAlgorithms
{
    /*

     */

    [SimpleJob(RuntimeMoniker.NetCoreApp50), MemoryDiagnoser, DisassemblyDiagnoser(maxDepth: 2)]
    public class ElonAbernathy_Project
    {
        readonly int seed = 1;
        [Params(200, 500)]
        public int NumberOfPoints;
        [Params(100, 1000)]
        public int NumberOfSegments;
        private Point[] points;
        private LineSegment[] segments;
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
            this.points = GetPoints();
            this.segments = GetSegments();
        }

        public void BenchmarkSetup()
        {
            NumberOfPoints = 200;
            NumberOfSegments = 100;
            this.points = GetPoints();
            this.segments = GetSegments();
        }

        public void Unit_Setup(Point[] pointData, LineSegment[] segmentData)
        {
            NumberOfPoints = pointData.Length;
            NumberOfSegments = segmentData.Length;

            this.points = pointData;
            this.segments = segmentData;

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
                Point point = points[i];

                Point shortest = default;
                float distanceSq = float.MaxValue;

                foreach (var segment in this.segments)
                {
                    var tmp = DomainMathFunctions.GetClosestPointOnLine(point, segment);

                    float tdist = Point.DistanceSquared(point, tmp);

                    if (distanceSq > tdist)
                    {
                        shortest = tmp;
                        distanceSq = tdist;
                    }
                }

                result[i] = Point.Distance(point, shortest);
            }

            return result;
        }

        [Benchmark]
        public float VecSolution()
        {
            float result = 0;
            foreach (var point in this.vecPoints)
            {
                Vector3 shortest = default;
                float distanceSq = float.MaxValue;

                foreach (var segment in this.vecSegments)
                {
                    var tmp = DomainMathFunctions.GetClosestPointOnLine(point, segment);

                    float tdist = Vector3.DistanceSquared(point.Vec3, tmp);

                    if (distanceSq > tdist)
                    {
                        shortest = tmp;
                        distanceSq = tdist;
                    }
                }

                result += Vector3.Distance(point.Vec3, shortest);
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

            Vector128<float>* shortest = stackalloc Vector128<float>[4] //Manual Fully controlled stack spill
            {
                Vector128<float>.Zero, // X
                Vector128<float>.Zero, // Y
                Vector128<float>.Zero, // Z
                Vector128.Create(float.MaxValue) // distance
            };

            for (int i = 0; i < PointData.Vector128Count; ++i)
            {
                // float x = point.X;
                // float y = point.Y;
                // float z = point.Z;
                var point_X = PointData.GetXVector128(i);
                var point_Y = PointData.GetYVector128(i);
                var point_Z = PointData.GetZVector128(i);

                for (int j = 0; j < this.segments.Length; ++j)
                {
                    ref var segment = ref this.segments[j];

                    Vector128<float> v0, v1, v2, v3, v4, v5;

                    // float lox = lineSegment.A.X;
                    // float loy = lineSegment.A.Y;
                    // float loz = lineSegment.A.Z;
                    v0 = Vector128.Create(segment.A.X);
                    v1 = Vector128.Create(segment.A.Y);
                    v2 = Vector128.Create(segment.A.Z);

                    // float lx = lineSegment.B.X - lox;
                    // float ly = lineSegment.B.Y - loy;
                    // float lz = lineSegment.B.Z - loz;
                    v3 = Vector128.Create(segment.B.X - v0.ToScalar());
                    v4 = Vector128.Create(segment.B.Y - v1.ToScalar());
                    v5 = Vector128.Create(segment.B.Z - v2.ToScalar());

                    Vector128<float> v6, v7, v8, v9, v10, v11, v12;

                    // float firstx = x - lox;
                    // float firsty = y - loy;
                    // float firstz = z - loz;
                    v6 = Sse.Subtract(point_X, v0);
                    v7 = Sse.Subtract(point_Y, v1);
                    v8 = Sse.Subtract(point_Z, v2);

                    // float numerator = lx * firstx + ly * firsty + lz * firstz;
                    v6 = Sse.Multiply(v3, v6);
                    v6 = Helper_MultiplyAdd(v4, v7, v6);
                    v6 = Helper_MultiplyAdd(v5, v8, v6);

                    // float denominator = lx * lx + ly * ly + lz * lz;
                    v7 = Sse.Multiply(v3, v3);
                    v7 = Helper_MultiplyAdd(v4, v4, v7);
                    v7 = Helper_MultiplyAdd(v5, v5, v7);

                    // float t = numerator / denominator;
                    v6 = Sse.Divide(v6, v7);

                    // float xx = lox + t * lx;
                    // float yy = loy + t * ly;
                    // float zz = loz + t * lz;
                    // Point maybeMiddle = new Point(xx, yy, zz);
                    v3 = Helper_MultiplyAdd(v6, v3, v0);
                    v4 = Helper_MultiplyAdd(v6, v4, v1);
                    v5 = Helper_MultiplyAdd(v6, v5, v2);

                    // Point.Distance(lineSegment.A, lineSegment.B)
                    v6 = Vector128.Create(segment.distance);

                    // Point.Distance(lineSegment.A, maybeMiddle)
                    v7 = Sse.Subtract(v0, v3);
                    v7 = Sse.Multiply(v7, v7);
                    v8 = Sse.Subtract(v1, v4);
                    v7 = Helper_MultiplyAdd(v8, v8, v7);
                    v8 = Sse.Subtract(v2, v5);
                    v7 = Helper_MultiplyAdd(v8, v8, v7);
                    v7 = Sse.Sqrt(v7);

                    // Point.Distance(maybeMiddle, lineSegment.B)
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

                    // bool isOnLineSegment = Math.Abs(Point.Distance(lineSegment.A, lineSegment.B) - (Point.Distance(lineSegment.A, maybeMiddle) + Point.Distance(maybeMiddle, lineSegment.B))) < 0.001;
                    v7 = Sse.Add(v7, v8);
                    v6 = Sse.Subtract(v6, v7);
                    v6 = Sse.And(v6, Vector128.Create(int.MaxValue).AsSingle());
                    v6 = Sse.CompareLessThan(v6, Vector128.Create(0.001f));

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

                var tmp = Sse.Subtract(point_X, shortest[0]);
                tmp = Sse.Multiply(tmp, tmp);
                var tmp2 = Sse.Subtract(point_Y, shortest[1]);
                tmp = Helper_MultiplyAdd(tmp2, tmp2, tmp);
                tmp2 = Sse.Subtract(point_Z, shortest[2]);
                tmp = Helper_MultiplyAdd(tmp2, tmp2, tmp);
                tmp = Sse.Sqrt(tmp);

                Unsafe.Add(ref Unsafe.As<float, Vector128<float>>(ref MemoryMarshal.GetArrayDataReference(results)), i) = tmp;
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

            Vector256<float>* shortest = stackalloc Vector256<float>[4] //Manual Fully controlled stack spill
            {
                Vector256<float>.Zero, // X
                Vector256<float>.Zero, // Y
                Vector256<float>.Zero, // Z
                Vector256.Create(float.MaxValue) // distance
            };

            for (int i = 0; i < PointData.Vector256Count; ++i)
            {
                // float x = point.X;
                // float y = point.Y;
                // float z = point.Z;
                var point_X = PointData.GetXVector256(i);
                var point_Y = PointData.GetYVector256(i);
                var point_Z = PointData.GetZVector256(i);

                for (int j = 0; j < this.segments.Length; ++j)
                {
                    ref var segment = ref this.segments[j];

                    Vector256<float> v0, v1, v2, v3, v4, v5;

                    // float lox = lineSegment.A.X;
                    // float loy = lineSegment.A.Y;
                    // float loz = lineSegment.A.Z;
                    v0 = Vector256.Create(segment.A.X);
                    v1 = Vector256.Create(segment.A.Y);
                    v2 = Vector256.Create(segment.A.Z);

                    // float lx = lineSegment.B.X - lox;
                    // float ly = lineSegment.B.Y - loy;
                    // float lz = lineSegment.B.Z - loz;
                    v3 = Vector256.Create(segment.B.X - v0.ToScalar());
                    v4 = Vector256.Create(segment.B.Y - v1.ToScalar());
                    v5 = Vector256.Create(segment.B.Z - v2.ToScalar());

                    Vector256<float> v6, v7, v8, v9, v10, v11, v12;

                    // float firstx = x - lox;
                    // float firsty = y - loy;
                    // float firstz = z - loz;
                    v6 = Avx.Subtract(point_X, v0);
                    v7 = Avx.Subtract(point_Y, v1);
                    v8 = Avx.Subtract(point_Z, v2);

                    // float numerator = lx * firstx + ly * firsty + lz * firstz;
                    v6 = Avx.Multiply(v3, v6);
                    v6 = Helper_MultiplyAdd(v4, v7, v6);
                    v6 = Helper_MultiplyAdd(v5, v8, v6);

                    // float denominator = lx * lx + ly * ly + lz * lz;
                    v7 = Avx.Multiply(v3, v3);
                    v7 = Helper_MultiplyAdd(v4, v4, v7);
                    v7 = Helper_MultiplyAdd(v5, v5, v7);

                    // float t = numerator / denominator;
                    v6 = Avx.Divide(v6, v7);

                    // float xx = lox + t * lx;
                    // float yy = loy + t * ly;
                    // float zz = loz + t * lz;
                    // Point maybeMiddle = new Point(xx, yy, zz);
                    v3 = Helper_MultiplyAdd(v6, v3, v0);
                    v4 = Helper_MultiplyAdd(v6, v4, v1);
                    v5 = Helper_MultiplyAdd(v6, v5, v2);

                    // Point.Distance(lineSegment.A, lineSegment.B)
                    v6 = Vector256.Create(segment.distance);

                    // Point.Distance(lineSegment.A, maybeMiddle)
                    v7 = Avx.Subtract(v0, v3);
                    v7 = Avx.Multiply(v7, v7);
                    v8 = Avx.Subtract(v1, v4);
                    v7 = Helper_MultiplyAdd(v8, v8, v7);
                    v8 = Avx.Subtract(v2, v5);
                    v7 = Helper_MultiplyAdd(v8, v8, v7);
                    v7 = Avx.Sqrt(v7);

                    // Point.Distance(maybeMiddle, lineSegment.B)
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

                    // bool isOnLineSegment = Math.Abs(Point.Distance(lineSegment.A, lineSegment.B) - (Point.Distance(lineSegment.A, maybeMiddle) + Point.Distance(maybeMiddle, lineSegment.B))) < 0.001;
                    v7 = Avx.Add(v7, v8);
                    v6 = Avx.Subtract(v6, v7);
                    v6 = Avx.And(v6, Vector256.Create(int.MaxValue).AsSingle());
                    v6 = Avx.Compare(v6, Vector256.Create(0.001f), FloatComparisonMode.OrderedLessThanNonSignaling);

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
                    v6 = Avx.Compare(v5, v4, FloatComparisonMode.OrderedGreaterThanNonSignaling);
                    shortest[3] = Avx.BlendVariable(v5, v4, v6);

                    shortest[0] = Avx.BlendVariable(shortest[0], v0, v6);
                    shortest[1] = Avx.BlendVariable(shortest[1], v1, v6);
                    shortest[2] = Avx.BlendVariable(shortest[2], v2, v6);
                }

                var tmp = Avx.Subtract(point_X, shortest[0]);
                tmp = Avx.Multiply(tmp, tmp);
                var tmp2 = Avx.Subtract(point_Y, shortest[1]);
                tmp = Helper_MultiplyAdd(tmp2, tmp2, tmp);
                tmp2 = Avx.Subtract(point_Z, shortest[2]);
                tmp = Helper_MultiplyAdd(tmp2, tmp2, tmp);
                tmp = Avx.Sqrt(tmp);

                Unsafe.Add(ref Unsafe.As<float, Vector256<float>>(ref MemoryMarshal.GetArrayDataReference(results)), i) = tmp;
            }

            return results.AsSpan(0, NumberOfPoints);
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
            return ref Unsafe.Add(ref Unsafe.As<float, Vector128<float>>(ref MemoryMarshal.GetArrayDataReference(arr)), vectorIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref Vector256<float> GetVector256(float[] arr, int vectorIndex)
        {
            return ref Unsafe.Add(ref Unsafe.As<float, Vector256<float>>(ref MemoryMarshal.GetArrayDataReference(arr)), vectorIndex);
        }

        private static int RoundUp(int value, int alignment)
        {
            return (value + alignment - 1) / alignment * alignment;
        }

        private static void Throw_IndexOutOfRange()
        {
            throw new IndexOutOfRangeException();
        }
    }
}
