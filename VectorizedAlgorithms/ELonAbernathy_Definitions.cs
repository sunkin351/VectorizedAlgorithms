using System;
using System.Numerics;

namespace VectorizedAlgorithms
{
    public struct VecPoint
    {
        public string ID;
        public Vector3 Vec3;

        public VecPoint(Vector3 vec)
        {
            ID = null;
            Vec3 = vec;
        }

        public float X => Vec3.X;

        public float Y => Vec3.Y;

        public float Z => Vec3.Z;

        public static implicit operator VecPoint(Vector3 vec)
        {
            return new VecPoint(vec);
        }
    }

    public struct Point
    {
        public float X;
        public float Y;
        public float Z;

        public Point(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Point(double x, double y, double z)
        {
            X = (float)x;
            Y = (float)y;
            Z = (float)z;
        }

        public static float Distance(Point a, Point b)
        {
            return MathF.Sqrt(DistanceSquared(a, b));
        }

        public static float DistanceSquared(Point a, Point b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            var dz = a.Z - b.Z;
            return dx * dx + dy * dy + dz * dz;
        }

        public static implicit operator Vector3(Point p)
        {
            return new Vector3(p.X, p.Y, p.Z);
        }
    }

    public struct VecSegment
    {
        public VecPoint A, B;
        public float distance;

        public VecSegment(VecPoint a, VecPoint b)
        {
            A = a;
            B = b;

            distance = Vector3.Distance(a.Vec3, b.Vec3);
        }
    }

    public readonly struct LineSegment
    {
        //Points
        public readonly Vector3 A, B;

        //Precalculated

        /// <summary>
        /// Direction Vector, not normalized, B - A
        /// </summary>
        public readonly Vector3 Direction;

        public readonly float DirectionDot;
        public readonly float Length;

        public LineSegment(Vector3 a, Vector3 b)
        {
            A = a;
            B = b;

            var dir = b - a;
            DirectionDot = Vector3.Dot(dir, dir);
            Direction = dir;

            Length = MathF.Sqrt(DirectionDot);
        }
    }

    public class DomainMathFunctions
    {
        public static Vector3 GetClosestPointOnLine_VecMath(Vector3 point, ref LineSegment lineSegment)
        {
            Vector3 vlo = lineSegment.A;
            Vector3 vl = lineSegment.Direction;
            Vector3 vfirst = point - vlo;
            
            float vt = Vector3.Dot(vl, vfirst) / lineSegment.DirectionDot;

            Vector3 intersectionPoint = vl * vt + vlo;

            bool isOnLineSegment = Math.Sqrt(
                Math.Max(
                    Vector3.DistanceSquared(lineSegment.A, intersectionPoint),
                    Vector3.DistanceSquared(intersectionPoint, lineSegment.B)
                )
            ) < lineSegment.Length;

            if (isOnLineSegment)
            {
                return intersectionPoint;
            }
            else
            {
                if (Vector3.DistanceSquared(lineSegment.A, point) < Vector3.DistanceSquared(lineSegment.B, point))
                {
                    return lineSegment.A;
                }
                else
                {
                    return lineSegment.B;
                }
            }
        }

        public static Vector3 GetClosestPointOnLine_ScalarMath(Vector3 point, ref LineSegment lineSegment)
        {
            float x = point.X;
            float y = point.Y;
            float z = point.Z;

            float lox = lineSegment.A.X;
            float loy = lineSegment.A.Y;
            float loz = lineSegment.A.Z;
            float lx = lineSegment.Direction.X;
            float ly = lineSegment.Direction.Y;
            float lz = lineSegment.Direction.Z;
            float firstx = x - lox;
            float firsty = y - loy;
            float firstz = z - loz;
            float t = (lx * firstx + ly * firsty + lz * firstz) / lineSegment.DirectionDot;

            float xx = lox + t * lx;
            float yy = loy + t * ly;
            float zz = loz + t * lz;

            Vector3 intersectionPoint = new(xx, yy, zz);
            
            bool isOnLineSegment = Math.Sqrt(
                Math.Max(
                    Vector3.DistanceSquared(lineSegment.A, intersectionPoint),
                    Vector3.DistanceSquared(intersectionPoint, lineSegment.B)
                )
            ) < lineSegment.Length;
            
            if (isOnLineSegment)
            {
                return intersectionPoint;
            }
            else
            {
                if (Vector3.DistanceSquared(point, lineSegment.A) < Vector3.DistanceSquared(point, lineSegment.B))
                {
                    return lineSegment.A;
                }
                else
                {
                    return lineSegment.B;
                }
            }
        }
    }

}
