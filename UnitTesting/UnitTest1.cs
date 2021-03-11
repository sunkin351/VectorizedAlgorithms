using System;
using Xunit;

using VectorizedAlgorithms;

namespace UnitTesting
{
    public class UnitTest1
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

            var sse = benchMarkedFunctions.Sse41_Solution().ToArray();
            var avx = benchMarkedFunctions.Avx2_Solution().ToArray();

            Assert.Equal(sse, avx);
        }
    }
}
