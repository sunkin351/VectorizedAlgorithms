using System;
using System.Numerics;
using Xunit;

using VectorizedAlgorithms;
using System.Linq;

namespace UnitTesting
{
    public class IntrinsicTests
    {
        [Fact]
        public void AllAnswersAreEqual()
        {
            ElonAbernathy_Project benchMarkedFunctions = new ElonAbernathy_Project();

            var points = new Point[]
            {
                new Point(2, 5, 8),
                new Point(1, 3, 5),
                new Point(4, 8, 12),
                new Point(2.5f, 5.5f, 8.5f),
                new Point(0, 0, 0),
                new Point(-9.5f, -10, -1.4f),
                new Point(100, 100, 100),
                new Point(100, 100, -1000)
            };

            var segments = new LineSegment[]
            {
                new LineSegment
                (
                    new Point(2, 4, 6),
                    new Point(3, 7, 11)
                ),
                new LineSegment
                (
                    new Point(-10, -11, -12),
                    new Point(-9, -9, -15)
                )
            };

            benchMarkedFunctions.Unit_Setup(points, segments);

            var scalar = benchMarkedFunctions.Solution();
            var sse = benchMarkedFunctions.Sse41_Solution().ToArray();
            var avx = benchMarkedFunctions.Avx2_Solution().ToArray();

            Assert.Equal(scalar, sse);
            Assert.Equal(scalar, avx);
        }
    }

    public class ScalarTests
    {

        [Fact]
        public void Base_Unit_Test01()
        {
            //Remember that the Solution() returns distance squared.
            ElonAbernathy_Project benchMarkedFunctions = new ElonAbernathy_Project();

            var points = new Point[]
            {
                new Point(2, 5, 8)
            };

            var segs = new LineSegment[]
            {
                new LineSegment(new Vector3(2, 4, 6), new Vector3(3, 7, 11))
            };

            benchMarkedFunctions.Unit_Setup(points, segs);

            Assert.Equal(Math.Sqrt(210) / 35.0, benchMarkedFunctions.Solution().Sum(), 4);
        }

        [Fact]
        public void Base_Unit_Test02()
        {
            ElonAbernathy_Project benchMarkedFunctions = new ElonAbernathy_Project();

            var points = new Point[]
            {
                new Point(1, 3, 5)
            };

            var segs = new LineSegment[]
            {
                new LineSegment(new Vector3(2, 4, 6), new Vector3(3, 7, 11))
            };

            benchMarkedFunctions.Unit_Setup(points, segs);

            Assert.Equal(Math.Sqrt(1 + 1 + 1), benchMarkedFunctions.Solution().Sum(), 5);
        }

        [Fact]
        public void Base_Unit_Test03()
        {
            ElonAbernathy_Project benchMarkedFunctions = new ElonAbernathy_Project();

            var points = new Point[]
            {
                new Point(4, 8, 12)
            };

            var segs = new LineSegment[]
            {
                new LineSegment(new Vector3(2, 4, 6), new Vector3(3, 7, 11))
            };

            benchMarkedFunctions.Unit_Setup(points, segs);

            Assert.Equal(Math.Sqrt(1 + 1 + 1), benchMarkedFunctions.Solution().Sum(), 5);
        }

        [Fact]
        public void Base_Unit_Test04()
        {
            ElonAbernathy_Project benchMarkedFunctions = new ElonAbernathy_Project();

            var points = new Point[]
            {
                new Point(2.5f, 5.5f, 8.5f)
            };

            var segs = new LineSegment[]
            {
                new LineSegment(new Vector3(2, 4, 6), new Vector3(3, 7, 11))
            };

            benchMarkedFunctions.Unit_Setup(points, segs);

            Assert.Equal(0, benchMarkedFunctions.Solution().Sum(), 5);
        }

        [Fact]
        public void Base_Unit_Test05()
        {
            ElonAbernathy_Project benchMarkedFunctions = new ElonAbernathy_Project();

            var points = new Point[]
            {
                new Point(0, 0, 0)
            };

            var segs = new LineSegment[]
            {
                new LineSegment(new Vector3(-10, -11, -12), new Vector3(-9, -9, -15))
            };

            benchMarkedFunctions.Unit_Setup(points, segs);

            Assert.Equal(Math.Sqrt(100 + 121 + 144), benchMarkedFunctions.Solution().Sum(), 5);
            Assert.Equal(Math.Sqrt(100 + 121 + 144), Vector3.Distance(benchMarkedFunctions.Points[0], benchMarkedFunctions.Segments[0].A), 5);
        }

        [Fact]
        public void Base_Unit_Test06()
        {
            ElonAbernathy_Project benchMarkedFunctions = new ElonAbernathy_Project();

            var points = new Point[]
            {
                new Point(-9.5f, -10, -14)
            };

            var segs = new LineSegment[]
            {
                new LineSegment(new Vector3(-10, -11, -12), new Vector3(-9, -9, -15))
            };

            benchMarkedFunctions.Unit_Setup(points, segs);

            Assert.Equal(Math.Sqrt(70) / 28.0, benchMarkedFunctions.Solution().Sum(), 5);
        }

        [Fact]
        public void Base_Unit_Test07()
        {
            ElonAbernathy_Project benchMarkedFunctions = new ElonAbernathy_Project();

            var points = new Point[]
            {
                new Point(100, 100, 100)
            };

            var segs = new LineSegment[]
            {
                new LineSegment(new Vector3(-10, -11, -12), new Vector3(-9, -9, -15))
            };

            benchMarkedFunctions.Unit_Setup(points, segs);

            Assert.Equal(Math.Sqrt(12100 + 12321 + 12544), benchMarkedFunctions.Solution().Sum(), 4);
            Assert.Equal(Math.Sqrt(12100 + 12321 + 12544), Vector3.Distance(benchMarkedFunctions.Points[0], benchMarkedFunctions.Segments[0].A), 4);
        }

        [Fact]
        public void Base_Unit_Test08()
        {
            ElonAbernathy_Project benchMarkedFunctions = new ElonAbernathy_Project();

            var points = new Point[]
            {
                new Point(100, 100, -1000)
            };

            var segs = new LineSegment[]
            {
                new LineSegment(new Vector3(-10, -11, -12), new Vector3(-9, -9, -15))
            };

            benchMarkedFunctions.Unit_Setup(points, segs);

            Assert.Equal(Math.Sqrt(11881 + 11881 + 970225), benchMarkedFunctions.Solution().Sum(), 4);
            Assert.Equal(Math.Sqrt(11881 + 11881 + 970225), Vector3.Distance(benchMarkedFunctions.Points[0], benchMarkedFunctions.Segments[0].B), 4);
        }

        [Fact]
        public void Base_Unit_Test99()
        {
            ElonAbernathy_Project benchMarkedFunctions = new ElonAbernathy_Project();

            var points = new Point[] {
                new Point(2, 5, 8),
                new Point(1, 3, 5),
                new Point(4, 8, 12),
                new Point(2.5f, 5.5f, 8.5f),
                new Point(0, 0, 0),
                new Point(-9.5f, -10, -14),
                new Point(100, 100, 100),
                new Point(100, 100, -1000),
            };

            var segs = new LineSegment[] {
                new LineSegment(new Vector3(2, 4, 6), new Vector3(3, 7, 11)),
                new LineSegment(new Vector3(-10, -11, -12), new Vector3(-9, -9, -15)),
            };

            benchMarkedFunctions.Unit_Setup(points, segs);

            Assert.Equal(1169.82922, benchMarkedFunctions.Solution().Sum(), 5);
        }

    }
}
