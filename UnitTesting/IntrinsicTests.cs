using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Diagnostics;
using VectorizedAlgorithms;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

namespace UnitTesting
{
    public class IntrinsicTests
    {
        private class FloatComparer : EqualityComparer<float>
        {
            private int Precision;

            public FloatComparer(int precision)
            {
                Precision = precision;
            }

            public override bool Equals(float x, float y)
            {
                return Round(x) == Round(y);
            }

            public override int GetHashCode(float val)
            {
                return Round(val).GetHashCode();
            }

            private float Round(float val)
            {
                return MathF.Round(val, Precision);
            }
        }

        [Fact(DisplayName = "All Answers are Equal")]
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
            var intrin = benchMarkedFunctions.IntrinsicSolution();

            var comparer = new FloatComparer(5);

            Assert.Equal(scalar.distances, intrin.distances, comparer);
            Assert.Equal(scalar.indices, intrin.indices);
        }

        [Fact(DisplayName = "All Indices within Range")]
        public void AllIndicesWithinRangeAndEqual()
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
                    new Vector3(2, 4, 6),
                    new Vector3(3, 7, 11)
                ),
                new LineSegment
                (
                    new Vector3(-10, -11, -12),
                    new Vector3(-9, -9, -15)
                )
            };

            benchMarkedFunctions.Unit_Setup(points, segments);

            var scalar = benchMarkedFunctions.Solution();

            Assert.True(scalar.indices.All(i => i >= 0 && i < points.Length));

            var vec = benchMarkedFunctions.VecSolution();

            Assert.True(vec.indices.All(i => i >= 0 && i < points.Length));

            var intrin = benchMarkedFunctions.IntrinsicSolution();

            Assert.True(intrin.indices.All(i => i >= 0 && i < points.Length));
        }
    }

    public class ScalarTests
    {
        [Theory(DisplayName = "Basic Scalar Test")]
        [MemberData(nameof(TestData))]
        public void BasicScalarTest(double expected, Point[] points, LineSegment[] segments, int precision = 5)
        {
            //Remember that the Solution() returns distance squared.
            ElonAbernathy_Project benchMarkedFunctions = new ElonAbernathy_Project();

            benchMarkedFunctions.Unit_Setup(points, segments);
            
            Assert.Equal(expected, benchMarkedFunctions.Solution().distances.Sum(), precision);
        }

        public static IEnumerable<object[]> TestData
        {
            get
            {
                yield return new object[]
                {
                    Math.Sqrt(210) / 35.0,
                    new Point[] { new Point(2, 5, 8) },
                    new LineSegment[] { new LineSegment(new Vector3(2, 4, 6), new Vector3(3, 7, 11)) },
                    4
                };

                yield return new object[]
                {
                    Math.Sqrt(1 + 1 + 1),
                    new Point[] { new Point(1, 3, 5) },
                    new LineSegment[]
                    {
                        new LineSegment(new Vector3(2, 4, 6), new Vector3(3, 7, 11))
                    }
                };

                yield return new object[]
                {
                    Math.Sqrt(1 + 1 + 1),
                    new Point[]
                    {
                        new Point(4, 8, 12)
                    },
                    new LineSegment[]
                    {
                        new LineSegment(new Vector3(2, 4, 6), new Vector3(3, 7, 11))
                    }
                };

                yield return new object[]
                {
                    0D,
                    new Point[]
                    {
                        new Point(2.5f, 5.5f, 8.5f)
                    },
                    new LineSegment[]
                    {
                        new LineSegment(new Vector3(2, 4, 6), new Vector3(3, 7, 11))
                    }
                };

                yield return new object[]
                {
                    Math.Sqrt(100 + 121 + 144),
                    new Point[]
                    {
                        new Point(0, 0, 0)
                    },
                    new LineSegment[]
                    {
                        new LineSegment(new Vector3(-10, -11, -12), new Vector3(-9, -9, -15))
                    }
                };

                yield return new object[]
                {
                    Math.Sqrt(70) / 28.0,
                    new Point[]
                    {
                        new Point(-9.5f, -10, -14)
                    },
                    new LineSegment[]
                    {
                        new LineSegment(new Vector3(-10, -11, -12), new Vector3(-9, -9, -15))
                    }
                };

                yield return new object[]
                {
                    Math.Sqrt(12100 + 12321 + 12544),
                    new Point[]
                    {
                        new Point(100, 100, 100)
                    },
                    new LineSegment[]
                    {
                        new LineSegment(new Vector3(-10, -11, -12), new Vector3(-9, -9, -15))
                    },
                    4
                };

                yield return new object[]
                {
                    Math.Sqrt(11881 + 11881 + 970225),
                    new Point[]
                    {
                        new Point(100, 100, -1000)
                    },
                    new LineSegment[]
                    {
                        new LineSegment(new Vector3(-10, -11, -12), new Vector3(-9, -9, -15))
                    },
                    4
                };

                yield return new object[]
                {
                    1169.82922,
                    new Point[]
                    {
                        new Point(2, 5, 8),
                        new Point(1, 3, 5),
                        new Point(4, 8, 12),
                        new Point(2.5f, 5.5f, 8.5f),
                        new Point(0, 0, 0),
                        new Point(-9.5f, -10, -14),
                        new Point(100, 100, 100),
                        new Point(100, 100, -1000),
                    },
                    new LineSegment[] {
                        new LineSegment(new Vector3(2, 4, 6), new Vector3(3, 7, 11)),
                        new LineSegment(new Vector3(-10, -11, -12), new Vector3(-9, -9, -15)),
                    }
                };
            }
        }

        [Fact]
        public void LessBasicTest1()
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
            
            Assert.Equal(Math.Sqrt(100 + 121 + 144), benchMarkedFunctions.Solution().distances.Sum(), 5);
            Assert.Equal(Math.Sqrt(100 + 121 + 144), Vector3.Distance(benchMarkedFunctions.Points[0], benchMarkedFunctions.Segments[0].A), 5);
        }

        [Fact]
        public void LessBasicTest2()
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
            
            Assert.Equal(Math.Sqrt(12100 + 12321 + 12544), benchMarkedFunctions.Solution().distances.Sum(), 4);
            Assert.Equal(Math.Sqrt(12100 + 12321 + 12544), Vector3.Distance(benchMarkedFunctions.Points[0], benchMarkedFunctions.Segments[0].A), 4);
        }

        [Fact]
        public void LessBasicTest3()
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
            
            Assert.Equal(Math.Sqrt(11881 + 11881 + 970225), benchMarkedFunctions.Solution().distances.Sum(), 4);
            Assert.Equal(Math.Sqrt(11881 + 11881 + 970225), Vector3.Distance(benchMarkedFunctions.Points[0], benchMarkedFunctions.Segments[0].B), 4);
        }
    }
}
