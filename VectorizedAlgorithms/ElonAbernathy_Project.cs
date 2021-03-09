using System;
using System.Collections.Generic;
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
        private List<Point> points;
        private List<LineSegment> segments;
        private List<VecPoint> vecPoints = new List<VecPoint>();
        private List<VecSegment> vecSegments = new List<VecSegment>();

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.points = GetPoints();
            this.segments = GetSegments();
        }

        public List<Point> GetPoints()
        {
            List<Point> points = new List<Point>(NumberOfPoints);
            vecPoints = new List<VecPoint>(NumberOfPoints);
            Random random = new Random(seed);
            for (int i = 0; i < NumberOfPoints; i++)
            {
                Point point = new Point((random.NextDouble() - 0.5) * 1000, (random.NextDouble() - 0.5) * 1000, (random.NextDouble() - 0.5) * 1000);
                points.Add(point);
                vecPoints.Add(new VecPoint() { vector3 = new Vector3((float)point.x, (float)point.y, (float)point.z) });
            }
            return points;
        }

        public List<LineSegment> GetSegments()
        {
            List<LineSegment> lineSegments = new List<LineSegment>(NumberOfSegments);
            Random random = new Random(seed * 2); //Mersenne, take the wheel!
            for (int i = 0; i < NumberOfSegments; i++)
            {
                Point a = new Point((random.NextDouble() - 0.5) * 1000, (random.NextDouble() - 0.5) * 1000, (random.NextDouble() - 0.5) * 1000);
                Point b = new Point((random.NextDouble() - 0.5) * 1000, (random.NextDouble() - 0.5) * 1000, (random.NextDouble() - 0.5) * 1000);
                lineSegments.Add(new LineSegment()
                {
                    a = a,
                    b = b
                });
                vecSegments.Add(new VecSegment()
                {
                    a = new VecPoint() { vector3 = new Vector3((float)a.x, (float)a.y, (float)a.z) },
                    b = new VecPoint() { vector3 = new Vector3((float)b.x, (float)b.y, (float)b.z) },
                });
            }
            return lineSegments;
        }

        [Benchmark(Baseline = true)]
        public double LinqSolution()
        {
            double result = 0;
            foreach (Point point in this.points)
            {
                Point shortest = this.segments
                            .Select(x => DomainMathFunctions.GetClosestPointOnLine(point, x))
                            .OrderBy(y => Point.DistanceSquared(point, y)).FirstOrDefault();
                result += Point.DistanceSquared(point, shortest);
            }
            return result;
        }

        [Benchmark]
        public double ParallelLinqSolution()
        {
            double result = 0;
            Parallel.ForEach(points, (point) =>
            {
                Point shortest = this.segments
                            .Select(x => DomainMathFunctions.GetClosestPointOnLine(point, x))
                            .OrderBy(y => Point.DistanceSquared(point, y)).FirstOrDefault();
                result += Point.DistanceSquared(point, shortest);

            });
            return result;
        }

        [Benchmark]
        public double VecLinqSolution()
        {
            double result = 0;
            foreach (var point in this.vecPoints)
            {
                Vector3 shortest = this.vecSegments
                            .Select(x => DomainMathFunctions.GetClosestPointOnLine(point, x))
                            .OrderBy(y => Vector3.DistanceSquared(point.vector3, y)).FirstOrDefault();
                result += Vector3.Distance(point.vector3, shortest);
            }
            return result;
        }

        [Benchmark]
        public double ParallelVecLinqSolution()
        {
            double result = 0;
            Parallel.ForEach(vecPoints, (point) =>
            {
                Vector3 shortest = this.vecSegments
                                 .Select(x => DomainMathFunctions.GetClosestPointOnLine(point, x))
                                 .OrderBy(y => Vector3.DistanceSquared(point.vector3, y)).FirstOrDefault();
                result += Vector3.Distance(point.vector3, shortest);

            });
            return result;
        }

    }

    public struct VecPoint
    {
        public string ID;
        public Vector3 vector3;
    }

    public class Point
    {
        public double x;
        public double y;
        public double z;

        public Point(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static double Distance(Point a, Point b)
        {
            var dx = a.x - b.x;
            var dy = a.y - b.y;
            var dz = a.z - b.z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public static double DistanceSquared(Point a, Point b)
        {
            var dx = a.x - b.x;
            var dy = a.y - b.y;
            var dz = a.z - b.z;
            return dx * dx + dy * dy + dz * dz;
        }
    }

    public struct VecSegment
    {
        public VecPoint a, b;
    }

    public class LineSegment
    {
        public Point a, b;
    }

    public class DomainMathFunctions
    {
        public static Vector3 GetClosestPointOnLine(VecPoint point, VecSegment lineSegment)
        {
            Vector3 vPoint = point.vector3;
            Vector3 vlo = lineSegment.a.vector3;
            Vector3 vl = lineSegment.b.vector3 - vlo;
            Vector3 vfirst = vPoint - vlo;
            float num = Vector3.Dot(vl, vfirst);
            float den = Vector3.Dot(vl, vl);
            float vt = num / den;
            Vector3 middle = vl * vt + vlo;
            bool isInside = Math.Abs(Vector3.Distance(lineSegment.a.vector3, lineSegment.b.vector3) - (Vector3.Distance(lineSegment.a.vector3, middle) + Vector3.Distance(lineSegment.b.vector3, middle))) < 0.001;
            if (isInside)
            {
                return middle;
            }
            else
            {
                if (Vector3.Distance(lineSegment.a.vector3, point.vector3) < Vector3.Distance(lineSegment.b.vector3, point.vector3))
                {
                    return lineSegment.a.vector3;
                }
                else
                {
                    return lineSegment.b.vector3;
                }
            }
        }

        public static Point GetClosestPointOnLine(Point point, LineSegment lineSegment)
        {
            double x = point.x;
            double y = point.y;
            double z = point.z;

            double lox = lineSegment.a.x;
            double loy = lineSegment.a.y;
            double loz = lineSegment.a.z;
            double lx = lineSegment.b.x - lox;
            double ly = lineSegment.b.y - loy;
            double lz = lineSegment.b.z - loz;
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
            bool isOnLineSegment = Math.Abs(Point.Distance(lineSegment.a, lineSegment.b) - (Point.Distance(lineSegment.a, maybeMiddle) + Point.Distance(maybeMiddle, lineSegment.b))) < 0.001;
            if (isOnLineSegment)
            {
                return new Point(xx, yy, zz);
            }
            else
            {
                if (Point.Distance(point, lineSegment.a) < Point.Distance(point, lineSegment.b))
                {
                    return lineSegment.a;
                }
                else
                {
                    return lineSegment.b;
                }
            }
        }
    }
}
