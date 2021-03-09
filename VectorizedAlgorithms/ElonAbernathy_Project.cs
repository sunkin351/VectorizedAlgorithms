using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace VectorizationPlayground
{
/*
|                  Method | NumberOfPoints | NumberOfSegments |           Mean |        Error |       StdDev |         Median | Ratio | RatioSD |       Gen 0 |     Gen 1 | Gen 2 |     Allocated |
|------------------------ |--------------- |----------------- |---------------:|-------------:|-------------:|---------------:|------:|--------:|------------:|----------:|------:|--------------:|
|            LinqSolution |            200 |              100 |     1,107.8 us |     10.54 us |      8.80 us |     1,105.6 us |  1.00 |    0.00 |    224.6094 |         - |     - |    1378.59 KB |
|    ParallelLinqSolution |            200 |              100 |       358.0 us |      5.69 us |      5.32 us |       357.5 us |  0.32 |    0.01 |    226.0742 |    5.8594 |     - |    1382.66 KB |
|         VecLinqSolution |            200 |              100 |     1,192.0 us |      8.71 us |      7.72 us |     1,189.7 us |  1.08 |    0.01 |     11.7188 |         - |     - |         75 KB |
| ParallelVecLinqSolution |            200 |              100 |       284.6 us |      3.76 us |      3.14 us |       285.1 us |  0.26 |    0.00 |     12.6953 |         - |     - |      78.74 KB |
|                         |                |                  |                |              |              |                |       |         |             |           |       |               |
|            LinqSolution |            200 |            10000 |   108,680.2 us |  1,251.32 us |  1,170.49 us |   108,191.1 us |  1.00 |    0.00 |  20800.0000 |         - |     - |  127849.36 KB |
|    ParallelLinqSolution |            200 |            10000 |    27,452.8 us |    545.30 us |    997.10 us |    27,890.4 us |  0.24 |    0.01 |  20906.2500 |  312.5000 |     - |  127857.15 KB |
|         VecLinqSolution |            200 |            10000 |   117,633.9 us |  1,101.86 us |  1,030.68 us |   117,714.4 us |  1.08 |    0.01 |           - |         - |     - |      75.26 KB |
| ParallelVecLinqSolution |            200 |            10000 |    16,583.2 us |    254.05 us |    237.64 us |    16,566.5 us |  0.15 |    0.00 |           - |         - |     - |      79.16 KB |
|                         |                |                  |                |              |              |                |       |         |             |           |       |               |
|            LinqSolution |           5000 |              100 |    27,017.4 us |    278.57 us |    232.62 us |    26,927.5 us |  1.00 |    0.00 |   5625.0000 |         - |     - |   34477.97 KB |
|    ParallelLinqSolution |           5000 |              100 |     6,634.3 us |     96.04 us |     85.14 us |     6,641.3 us |  0.25 |    0.00 |   5632.8125 |  125.0000 |     - |   34483.66 KB |
|         VecLinqSolution |           5000 |              100 |    30,273.9 us |    424.98 us |    376.73 us |    30,214.4 us |  1.12 |    0.02 |    281.2500 |         - |     - |       1875 KB |
| ParallelVecLinqSolution |           5000 |              100 |     4,012.8 us |     64.80 us |     60.61 us |     3,986.4 us |  0.15 |    0.00 |    304.6875 |         - |     - |    1879.04 KB |
|                         |                |                  |                |              |              |                |       |         |             |           |       |               |
|            LinqSolution |           5000 |            10000 | 2,731,117.1 us | 52,753.36 us | 51,810.85 us | 2,710,610.5 us |  1.00 |    0.00 | 522000.0000 |         - |     - | 3200436.99 KB |
|    ParallelLinqSolution |           5000 |            10000 |   626,047.4 us |  8,770.81 us |  7,775.10 us |   624,161.2 us |  0.23 |    0.01 | 522000.0000 | 7000.0000 |     - | 3200443.24 KB |
|         VecLinqSolution |           5000 |            10000 | 2,968,166.5 us | 18,816.72 us | 16,680.53 us | 2,968,306.2 us |  1.08 |    0.02 |           - |         - |     - |       1875 KB |
| ParallelVecLinqSolution |           5000 |            10000 |   317,296.8 us |  4,565.56 us |  4,483.99 us |   315,231.3 us |  0.12 |    0.00 |           - |         - |     - |     1879.3 KB |
*/

    [SimpleJob(RuntimeMoniker.NetCoreApp50), MemoryDiagnoser, DisassemblyDiagnoser(maxDepth: 2)]
    public class ElonAbernathy_Project
    {
        readonly int seed = 1;
        [Params(200, 5_000)]
        public int NumberOfPoints;
        [Params(100, 10_000)]
        public int NumberOfSegments;
        private Point[] points;
        private LineSegment[] segments;
        private VecPoint[] vecPoints;
        private VecSegment[] vecSegments;

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

        public Point[] GetPoints()
        {
            Point[] points = new Point[NumberOfPoints];
            
            vecPoints = new VecPoint[NumberOfPoints];
            
            Random random = new Random(seed);
            
            for (int i = 0; i < NumberOfPoints; i++)
            {
                Point point = new Point((random.NextDouble() - 0.5) * 1000, (random.NextDouble() - 0.5) * 1000, (random.NextDouble() - 0.5) * 1000);
                
                points[i] = point;

                vecPoints[i] = new Vector3((float)point.X, (float)point.Y, (float)point.Z);
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
                Point a = new Point((random.NextDouble() - 0.5) * 1000, (random.NextDouble() - 0.5) * 1000, (random.NextDouble() - 0.5) * 1000);
                Point b = new Point((random.NextDouble() - 0.5) * 1000, (random.NextDouble() - 0.5) * 1000, (random.NextDouble() - 0.5) * 1000);

                lineSegments[i] = new LineSegment(a, b);

                vecSegments[i] = new VecSegment(
                    new Vector3((float)a.X, (float)a.Y, (float)a.Z),
                    new Vector3((float)b.X, (float)b.Y, (float)b.Z)
                );
            }

            return lineSegments;
        }

        [Benchmark(Baseline = true)]
        public double Solution()
        {
            double result = 0;
            foreach (Point point in this.points)
            {
                Point shortest = default;
                double distanceSq = double.MaxValue;

                foreach (var segment in this.segments)
                {
                    var tmp = DomainMathFunctions.GetClosestPointOnLine(point, segment);

                    double tdist = Point.DistanceSquared(point, tmp);

                    if (distanceSq > tdist)
                    {
                        shortest = tmp;
                        distanceSq = tdist;
                    }
                }

                result += Point.DistanceSquared(point, shortest);
            }
            return result;
        }

        [Benchmark]
        public double ParallelSolution()
        {
            double[] results = new double[points.Length];

            Parallel.For(0, results.Length, _options, index =>
            {
                var point = this.points[index];

                Point shortest = default;
                double distanceSq = double.MaxValue;

                foreach (var segment in this.segments)
                {
                    var tmp = DomainMathFunctions.GetClosestPointOnLine(point, segment);

                    double tdist = Point.DistanceSquared(point, tmp);

                    if (distanceSq > tdist)
                    {
                        shortest = tmp;
                        distanceSq = tdist;
                    }
                }

                results[index] = Point.DistanceSquared(point, shortest);

            });

            return results.Sum();
        }

        [Benchmark]
        public double VecSolution()
        {
            double result = 0;
            foreach (var point in this.vecPoints)
            {
                Vector3 shortest = default;
                double distanceSq = double.MaxValue;

                foreach (var segment in this.vecSegments)
                {
                    var tmp = DomainMathFunctions.GetClosestPointOnLine(point, segment);

                    double tdist = Vector3.DistanceSquared(point.vector3, tmp);

                    if (distanceSq > tdist)
                    {
                        shortest = tmp;
                        distanceSq = tdist;
                    }
                }

                result += Vector3.Distance(point.vector3, shortest);
            }
            return result;
        }

        [Benchmark]
        public double ParallelVecSolution()
        {
            double[] results = new double[vecPoints.Length];

            Parallel.For(0, vecPoints.Length, _options, index =>
            {
                var point = this.vecPoints[index];

                Vector3 shortest = default;
                double distanceSq = double.MaxValue;

                foreach (var segment in this.vecSegments)
                {
                    var tmp = DomainMathFunctions.GetClosestPointOnLine(point, segment);

                    double tdist = Vector3.DistanceSquared(point.vector3, tmp);

                    if (distanceSq > tdist)
                    {
                        shortest = tmp;
                        distanceSq = tdist;
                    }
                }

                results[index] = Vector3.Distance(point.vector3, shortest);

            });

            return results.Sum();
        }

    }

    public struct VecPoint
    {
        public string ID;
        public Vector3 vector3;

        public VecPoint(Vector3 vec)
        {
            ID = null;
            vector3 = vec;
        }

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

        public VecSegment(VecPoint a, VecPoint b)
        {
            A = a;
            B = b;
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
            Vector3 vPoint = point.vector3;
            Vector3 vlo = lineSegment.A.vector3;
            Vector3 vl = lineSegment.B.vector3 - vlo;
            Vector3 vfirst = vPoint - vlo;
            float num = Vector3.Dot(vl, vfirst);
            float den = Vector3.Dot(vl, vl);
            float vt = num / den;
            Vector3 middle = vl * vt + vlo;
            bool isInside = Math.Abs(Vector3.Distance(lineSegment.A.vector3, lineSegment.B.vector3) - (Vector3.Distance(lineSegment.A.vector3, middle) + Vector3.Distance(lineSegment.B.vector3, middle))) < 0.001;
            if (isInside)
            {
                return middle;
            }
            else
            {
                if (Vector3.Distance(lineSegment.A.vector3, point.vector3) < Vector3.Distance(lineSegment.B.vector3, point.vector3))
                {
                    return lineSegment.A.vector3;
                }
                else
                {
                    return lineSegment.B.vector3;
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
