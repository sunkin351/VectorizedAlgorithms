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
        public double X;
        public double Y;
        public double Z;

        public Point(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public static double Distance(Point a, Point b)
        {
            return Math.Sqrt(DistanceSquared(a, b));
        }

        public static double DistanceSquared(Point a, Point b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            var dz = a.Z - b.Z;
            return dx * dx + dy * dy + dz * dz;
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

    public struct LineSegment
    {
        public Point A, B;

        public LineSegment(Point a, Point b)
        {
            A = a;
            B = b;
        }
    }

    public class DomainMathFunctions
    {
        public static Vector3 GetClosestPointOnLine(VecPoint point, VecSegment lineSegment)
        {
            Vector3 vPoint = point.Vec3;
            Vector3 vlo = lineSegment.A.Vec3;
            Vector3 vl = lineSegment.B.Vec3 - vlo;
            Vector3 vfirst = vPoint - vlo;
            float num = Vector3.Dot(vl, vfirst);
            float den = Vector3.Dot(vl, vl);
            float vt = num / den;
            Vector3 middle = vl * vt + vlo;
            bool isInside = Math.Abs(Vector3.Distance(lineSegment.A.Vec3, lineSegment.B.Vec3) - (Vector3.Distance(lineSegment.A.Vec3, middle) + Vector3.Distance(lineSegment.B.Vec3, middle))) < 0.001;
            if (isInside)
            {
                return middle;
            }
            else
            {
                if (Vector3.Distance(lineSegment.A.Vec3, point.Vec3) < Vector3.Distance(lineSegment.B.Vec3, point.Vec3))
                {
                    return lineSegment.A.Vec3;
                }
                else
                {
                    return lineSegment.B.Vec3;
                }
            }
        }

        public static Point GetClosestPointOnLine(Point point, LineSegment lineSegment)
        {
            double x = point.X;
            double y = point.Y;
            double z = point.Z;

            double lox = lineSegment.A.X;
            double loy = lineSegment.A.Y;
            double loz = lineSegment.A.Z;
            double lx = lineSegment.B.X - lox;
            double ly = lineSegment.B.Y - loy;
            double lz = lineSegment.B.Z - loz;
            double firstx = x - lox;
            double firsty = y - loy;
            double firstz = z - loz;
            double numerator = lx * firstx + ly * firsty + lz * firstz;
            double denominator = lx * lx + ly * ly + lz * lz;
            double t = numerator / denominator;

            double xx = lox + t * lx;
            double yy = loy + t * ly;
            double zz = loz + t * lz;
            Point maybeMiddle = new Point(xx, yy, zz);
            bool isOnLineSegment = Math.Abs(Point.Distance(lineSegment.A, lineSegment.B) - (Point.Distance(lineSegment.A, maybeMiddle) + Point.Distance(maybeMiddle, lineSegment.B))) < 0.001;
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
        }
    }

}
