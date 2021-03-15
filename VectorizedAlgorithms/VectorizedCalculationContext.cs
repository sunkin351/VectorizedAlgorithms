using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace VectorizedAlgorithms
{
    public class VectorizedCalculationContext
    {
        public readonly float[] X, Y, Z;
        public readonly Guid[] Guids;

        public int ElementCount { get; }

        public readonly int Vector256Count;
        public int Vector128Count => Vector256Count * 2;

        public VectorizedCalculationContext(int elementCount)
        {
            int revisedElementCount = RoundUp(elementCount, Vector256<float>.Count);

            X = new float[revisedElementCount];
            Y = new float[revisedElementCount];
            Z = new float[revisedElementCount];
            Guids = null;

            ElementCount = elementCount;

            Vector256Count = revisedElementCount / Vector256<float>.Count;
        }

        public VectorizedCalculationContext(Vector3[] points) : this(points.Length)
        {
            for (int i = 0; i < points.Length; ++i)
            {
                ref var point = ref points[i];
                SetElements(i, point.X, point.Y, point.Z);
            }
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
        private static ref Vector128<T> GetVector128<T>(T[] arr, int vectorIndex) where T: unmanaged
        {
            var elemIndex = vectorIndex * Vector128<T>.Count;

            return ref Unsafe.As<T, Vector128<T>>(ref arr[elemIndex]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref Vector256<T> GetVector256<T>(T[] arr, int vectorIndex) where T: unmanaged
        {
            var elemIndex = vectorIndex * Vector256<T>.Count;

            return ref Unsafe.As<T, Vector256<T>>(ref arr[elemIndex]);
        }

        private static int RoundUp(int value, int alignment)
        {
            return (value + alignment - 1) / alignment * alignment;
        }

        public unsafe (int[] indices, float[] distances) SegmentsClosestToPoints(LineSegment[] segments)
        {
            var indicies = new int[ElementCount];
            var distances = new float[ElementCount];

            if (Avx.IsSupported)
            {
                int count = Math.DivRem(ElementCount, Vector256<float>.Count, out var remainder);

                for (int i = 0; i < count; ++i)
                {
                    SegmentsClosestToPoints_Avx2_Impl(i, segments, out GetVector256(indicies, i), out GetVector256(distances, i));
                }

                if (remainder != 0)
                {
                    int* ind = stackalloc int[Vector256<int>.Count];
                    float* dist = stackalloc float[Vector256<float>.Count];

                    SegmentsClosestToPoints_Avx2_Impl(count, segments, out *(Vector256<int>*)ind, out *(Vector256<float>*)dist);

                    for (int i = 0, arrIndex = count * Vector256<float>.Count; i < remainder; ++i, ++arrIndex)
                    {
                        indicies[arrIndex] = ind[i];
                        distances[arrIndex] = dist[i];
                    }
                }
            }
            else if (Sse41.IsSupported)
            {
                int count = Math.DivRem(ElementCount, Vector128<float>.Count, out var remainder);

                for (int i = 0; i < count; ++i)
                {
                    SegmentsClosestToPoints_Sse41_Impl(i, segments, out GetVector128(indicies, i), out GetVector128(distances, i));
                }

                if (remainder != 0)
                {
                    int* ind = stackalloc int[Vector128<int>.Count];
                    float* dist = stackalloc float[Vector128<float>.Count];

                    SegmentsClosestToPoints_Sse41_Impl(count, segments, out *(Vector128<int>*)ind, out *(Vector128<float>*)dist);

                    for (int i = 0, arrIndex = count * Vector128<float>.Count; i < remainder; ++i, ++arrIndex)
                    {
                        indicies[arrIndex] = ind[i];
                        distances[arrIndex] = dist[i];
                    }
                }
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            return (indicies, distances);
        }

        public void SegmentsClosestToPoints_Sse41_Impl(int i, LineSegment[] segments, out Vector128<int> indexes, out Vector128<float> distances)
        {
            Debug.Assert(i < Vector128Count);

            indexes = default;

            distances = Vector128.Create(float.MaxValue);

            // float x = point.X;
            // float y = point.Y;
            // float z = point.Z;
            var point_X = GetXVector128(i);
            var point_Y = GetYVector128(i);
            var point_Z = GetZVector128(i);

            for (int j = 0; j < segments.Length; ++j)
            {
                ref var segment = ref segments[j];

                Vector128<float> seg_AX, seg_AY, seg_AZ, seg_DirX, seg_DirY, seg_DirZ, seg_BX, seg_BY, seg_BZ;

                // float lox = lineSegment.A.X;
                // float loy = lineSegment.A.Y;
                // float loz = lineSegment.A.Z;
                seg_AX = Vector128.Create(segment.A.X);
                seg_AY = Vector128.Create(segment.A.Y);
                seg_AZ = Vector128.Create(segment.A.Z);

                // float lx = lineSegment.Direction.X;
                // float ly = lineSegment.Direction.Y;
                // float lz = lineSegment.Direction.Z;
                seg_DirX = Vector128.Create(segment.Direction.X);
                seg_DirY = Vector128.Create(segment.Direction.Y);
                seg_DirZ = Vector128.Create(segment.Direction.Z);

                Vector128<float> t, intersectX, intersectY, intersectZ;

                // temporary variables
                Vector128<float> v0, v1, v2, v3;

                // float firstx = x - lox;
                // float firsty = y - loy;
                // float firstz = z - loz;
                // float t = (lx * firstx + ly * firsty + lz * firstz) / segment.DirectionDot;
                t = Sse.Multiply(Sse.Subtract(point_X, seg_AX), seg_DirX);
                t = Helper_MultiplyAdd(Sse.Subtract(point_Y, seg_AY), seg_DirY, t);
                t = Helper_MultiplyAdd(Sse.Subtract(point_Z, seg_AZ), seg_DirZ, t);

                t = Sse.Divide(t, Vector128.Create(segment.DirectionDot));

                // float xx = lox + t * lx;
                // float yy = loy + t * ly;
                // float zz = loz + t * lz;
                // Point intersectionPoint = new Point(xx, yy, zz);
                intersectX = Helper_MultiplyAdd(t, seg_DirX, seg_AX);
                intersectY = Helper_MultiplyAdd(t, seg_DirY, seg_AY);
                intersectZ = Helper_MultiplyAdd(t, seg_DirZ, seg_AZ);

                // bool isOnLineSegment = Math.Max(Vector3.Distance(lineSegment.A, intersectionPoint), Vector3.Distance(intersectionPoint, lineSegment.B)) < lineSegment.Length;
                // Vector3.Distance(lineSegment.A, lineSegment.B)

                // distance from A to Intersect
                v0 = Sse.Subtract(seg_AX, intersectX);
                v0 = Sse.Multiply(v0, v0);
                v1 = Sse.Subtract(seg_AY, intersectY);
                v0 = Helper_MultiplyAdd(v1, v1, v0);
                v1 = Sse.Subtract(seg_AZ, intersectZ);
                v0 = Helper_MultiplyAdd(v1, v1, v0);

                // distance from intersect to B
                seg_BX = Vector128.Create(segment.B.X);
                seg_BY = Vector128.Create(segment.B.Y);
                seg_BZ = Vector128.Create(segment.B.Z);

                v1 = Sse.Subtract(intersectX, seg_BX);
                v1 = Sse.Multiply(v1, v1);
                v2 = Sse.Subtract(intersectY, seg_BY);
                v1 = Helper_MultiplyAdd(v2, v2, v1);
                v2 = Sse.Subtract(intersectZ, seg_BZ);
                v1 = Helper_MultiplyAdd(v2, v2, v1);

                //Max of distances
                v0 = Sse.Max(v0, v1);
                v0 = Sse.Sqrt(v0);

                //Compare max distances to segment length
                v0 = Sse.CompareLessThan(v0, Vector128.Create(segment.Length));

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

                v1 = Sse.Subtract(point_X, seg_AX);
                v1 = Sse.Multiply(v1, v1);
                v2 = Sse.Subtract(point_Y, seg_AY);
                v1 = Helper_MultiplyAdd(v2, v2, v1);
                v2 = Sse.Subtract(point_Z, seg_AZ);
                v1 = Helper_MultiplyAdd(v2, v2, v1);

                v2 = Sse.Subtract(point_X, seg_BX);
                v2 = Sse.Multiply(v2, v2);
                v3 = Sse.Subtract(point_Y, seg_BY);
                v2 = Helper_MultiplyAdd(v3, v3, v2);
                v3 = Sse.Subtract(point_Z, seg_BZ);
                v2 = Helper_MultiplyAdd(v3, v3, v2);

                v1 = Sse.CompareLessThan(v1, v2);

                Vector128<float> resX, resY, resZ;

                resX = Sse41.BlendVariable(seg_BX, seg_AX, v1);
                resY = Sse41.BlendVariable(seg_BY, seg_AY, v1);
                resZ = Sse41.BlendVariable(seg_BZ, seg_AZ, v1);
                resX = Sse41.BlendVariable(resX, intersectX, v0);
                resY = Sse41.BlendVariable(resY, intersectY, v0);
                resZ = Sse41.BlendVariable(resZ, intersectZ, v0);

                // distance from point to closest point on line segment
                v0 = Sse.Subtract(point_X, resX);
                v0 = Sse.Multiply(v0, v0);
                v1 = Sse.Subtract(point_Y, resY);
                v0 = Helper_MultiplyAdd(v1, v1, v0);
                v1 = Sse.Subtract(point_Z, resZ);
                v0 = Helper_MultiplyAdd(v1, v1, v0);

                // if (distanceSq > tdist)
                // {
                //     shortest = tmp;
                //     distanceSq = tdist;
                // }
                var tmpDist = distances;

                v1 = Sse.CompareGreaterThan(tmpDist, v0);

                distances = Sse41.BlendVariable(tmpDist, v0, v1);
                
                indexes = Sse41.BlendVariable(indexes, Vector128.Create(j), v1.AsInt32());
            }

            distances = Sse.Sqrt(distances);
        }

        public void SegmentsClosestToPoints_Avx2_Impl(int i, LineSegment[] segments, out Vector256<int> indexes, out Vector256<float> distances)
        {
            Debug.Assert(i < Vector256Count);

            indexes = default;
            distances = Vector256.Create(float.MaxValue);

            // float x = point.X;
            // float y = point.Y;
            // float z = point.Z;
            var point_X = GetXVector256(i);
            var point_Y = GetYVector256(i);
            var point_Z = GetZVector256(i);

            for (int j = 0; j < segments.Length; ++j)
            {
                ref var segment = ref segments[j];

                Vector256<float> seg_AX, seg_AY, seg_AZ, seg_DirX, seg_DirY, seg_DirZ, seg_BX, seg_BY, seg_BZ;

                // float lox = lineSegment.A.X;
                // float loy = lineSegment.A.Y;
                // float loz = lineSegment.A.Z;
                seg_AX = Vector256.Create(segment.A.X);
                seg_AY = Vector256.Create(segment.A.Y);
                seg_AZ = Vector256.Create(segment.A.Z);

                // float lx = lineSegment.Direction.X;
                // float ly = lineSegment.Direction.Y;
                // float lz = lineSegment.Direction.Z;
                seg_DirX = Vector256.Create(segment.Direction.X);
                seg_DirY = Vector256.Create(segment.Direction.Y);
                seg_DirZ = Vector256.Create(segment.Direction.Z);

                Vector256<float> t, intersectX, intersectY, intersectZ;

                // temporary variables
                Vector256<float> v0, v1, v2, v3;

                // float firstx = x - lox;
                // float firsty = y - loy;
                // float firstz = z - loz;
                // float t = (lx * firstx + ly * firsty + lz * firstz) / segment.DirectionDot;
                t = Avx.Multiply(Avx.Subtract(point_X, seg_AX), seg_DirX);
                t = Helper_MultiplyAdd(Avx.Subtract(point_Y, seg_AY), seg_DirY, t);
                t = Helper_MultiplyAdd(Avx.Subtract(point_Z, seg_AZ), seg_DirZ, t);

                t = Avx.Divide(t, Vector256.Create(segment.DirectionDot));

                // float xx = lox + t * lx;
                // float yy = loy + t * ly;
                // float zz = loz + t * lz;
                // Point intersectionPoint = new Point(xx, yy, zz);
                intersectX = Helper_MultiplyAdd(t, seg_DirX, seg_AX);
                intersectY = Helper_MultiplyAdd(t, seg_DirY, seg_AY);
                intersectZ = Helper_MultiplyAdd(t, seg_DirZ, seg_AZ);

                // bool isOnLineSegment = Math.Max(Vector3.Distance(lineSegment.A, intersectionPoint), Vector3.Distance(intersectionPoint, lineSegment.B)) < lineSegment.Length;
                // Vector3.Distance(lineSegment.A, lineSegment.B)

                // distance from A to Intersect
                v0 = Avx.Subtract(seg_AX, intersectX);
                v0 = Avx.Multiply(v0, v0);
                v1 = Avx.Subtract(seg_AY, intersectY);
                v0 = Helper_MultiplyAdd(v1, v1, v0);
                v1 = Avx.Subtract(seg_AZ, intersectZ);
                v0 = Helper_MultiplyAdd(v1, v1, v0);

                // distance from intersect to B
                seg_BX = Vector256.Create(segment.B.X);
                seg_BY = Vector256.Create(segment.B.Y);
                seg_BZ = Vector256.Create(segment.B.Z);

                v1 = Avx.Subtract(intersectX, seg_BX);
                v1 = Avx.Multiply(v1, v1);
                v2 = Avx.Subtract(intersectY, seg_BY);
                v1 = Helper_MultiplyAdd(v2, v2, v1);
                v2 = Avx.Subtract(intersectZ, seg_BZ);
                v1 = Helper_MultiplyAdd(v2, v2, v1);

                //Max of distances
                v0 = Avx.Max(v0, v1);
                v0 = Avx.Sqrt(v0);

                //Compare max distances to segment length
                v0 = Avx.CompareLessThan(v0, Vector256.Create(segment.Length));

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

                v1 = Avx.Subtract(point_X, seg_AX);
                v1 = Avx.Multiply(v1, v1);
                v2 = Avx.Subtract(point_Y, seg_AY);
                v1 = Helper_MultiplyAdd(v2, v2, v1);
                v2 = Avx.Subtract(point_Z, seg_AZ);
                v1 = Helper_MultiplyAdd(v2, v2, v1);

                v2 = Avx.Subtract(point_X, seg_BX);
                v2 = Avx.Multiply(v2, v2);
                v3 = Avx.Subtract(point_Y, seg_BY);
                v2 = Helper_MultiplyAdd(v3, v3, v2);
                v3 = Avx.Subtract(point_Z, seg_BZ);
                v2 = Helper_MultiplyAdd(v3, v3, v2);

                v1 = Avx.CompareLessThan(v1, v2);

                Vector256<float> resX, resY, resZ;

                resX = Avx.BlendVariable(seg_BX, seg_AX, v1);
                resY = Avx.BlendVariable(seg_BY, seg_AY, v1);
                resZ = Avx.BlendVariable(seg_BZ, seg_AZ, v1);
                resX = Avx.BlendVariable(resX, intersectX, v0);
                resY = Avx.BlendVariable(resY, intersectY, v0);
                resZ = Avx.BlendVariable(resZ, intersectZ, v0);

                // distance from point to closest point on line segment
                v0 = Avx.Subtract(point_X, resX);
                v0 = Avx.Multiply(v0, v0);
                v1 = Avx.Subtract(point_Y, resY);
                v0 = Helper_MultiplyAdd(v1, v1, v0);
                v1 = Avx.Subtract(point_Z, resZ);
                v0 = Helper_MultiplyAdd(v1, v1, v0);

                // if (distanceSq > tdist)
                // {
                //     shortest = tmp;
                //     distanceSq = tdist;
                // }
                var tmpDist = distances;

                v1 = Avx.CompareGreaterThan(tmpDist, v0);

                distances = Avx.BlendVariable(tmpDist, v0, v1);

                indexes = Avx.BlendVariable(indexes.AsSingle(), Vector256.Create(j).AsSingle(), v1).AsInt32();
            }

            distances = Avx.Sqrt(distances);
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
    }
}
