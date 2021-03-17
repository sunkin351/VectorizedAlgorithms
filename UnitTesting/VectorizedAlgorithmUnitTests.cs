using System;
using System.Collections.Generic;
using VectorizedAlgorithms;
using Xunit;

namespace UnitTesting
{
    public class VectorizedAlgorithmUnitTests
    {
        [Theory]
        [MemberData(nameof(BoolsTestData))]
        public void EnsureCompliantBools_Test(byte[] data, byte[] expected)
        {
            Algorithms.EnsureCompliantBools(data, data);

            Assert.Equal(expected, data);
        }

        public static IEnumerable<object[]> BoolsTestData
        {
            get
            {
                yield return new object[]
                {
                    new byte[]
                    {
                        0, 1, 2, 3, 4, 5
                    },
                    new byte[]
                    {
                        0, 1, 1, 1, 1, 1
                    }
                };

                yield return new object[]
                {
                    new byte[]
                    {
                        2, 4, 1, 0, 8, 0, 0, 1
                    },
                    new byte[]
                    {
                        1, 1, 1, 0, 1, 0, 0, 1
                    }
                };

                var random = new Random();

                var expected = new byte[31];
                var data = new byte[31];

                for (int i = 0; i < data.Length; ++i)
                {
                    byte tmp = (byte)random.Next(0, 5);

                    data[i] = tmp;
                    expected[i] = (byte)(tmp != 0 ? 1 : 0);
                }

                yield return new object[]
                {
                    data, expected
                };
            }
        }

        [Theory]
        [MemberData(nameof(IndexOfArraySizes))]
        public void IndexOf_Byte(int size)
        {
            var arr = PrepareIndexOfTestData(size, n => (byte)n);

            for (int i = 0; i < arr.Length; ++i)
            {
                int idx = Algorithms.IndexOf(arr, (byte)i);

                Assert.Equal(i, idx);
            }
        }

        [Theory]
        [MemberData(nameof(IndexOfArraySizes))]
        public void IndexOf_UInt16(int size)
        {
            var arr = PrepareIndexOfTestData(size, n => (ushort)n);

            for (int i = 0; i < arr.Length; ++i)
            {
                int idx = Algorithms.IndexOf(arr, (ushort)i);

                Assert.Equal(i, idx);
            }
        }

        [Theory]
        [MemberData(nameof(IndexOfArraySizes))]
        public void IndexOf_UInt32(int size)
        {
            var arr = PrepareIndexOfTestData(size, n => (uint)n);

            for (int i = 0; i < arr.Length; ++i)
            {
                int idx = Algorithms.IndexOf(arr, (uint)i);

                Assert.Equal(i, idx);
            }
        }

        private static T[] PrepareIndexOfTestData<T>(int size, Func<int, T> convert)
        {
            var arr = new T[size];

            for (int i = 0; i < arr.Length; ++i)
            {
                arr[i] = convert(i);
            }

            return arr;
        }

        public static IEnumerable<object[]> IndexOfArraySizes
        {
            get
            {
                yield return new object[] { 8   };
                yield return new object[] { 16  };
                yield return new object[] { 64  };
                yield return new object[] { 255 };
            }
        }
    }
}
