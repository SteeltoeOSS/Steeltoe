﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Common.Util.Test
{
    public class ObjectUtilsTest
    {
        [Fact]
        public void NullSafeEqualsWithArrays()
        {
            Assert.True(ObjectUtils.NullSafeEquals(new string[] { "a", "b", "c" }, new string[] { "a", "b", "c" }));
            Assert.True(ObjectUtils.NullSafeEquals(new int[] { 1, 2, 3 }, new int[] { 1, 2, 3 }));
        }

        [Fact]
        public void NullSafeHashCodeWithBooleanArray()
        {
            var expected = (31 * 7) + true.GetHashCode();
            expected = (31 * expected) + false.GetHashCode();

            bool[] array = { true, false };
            var actual = ObjectUtils.NullSafeHashCode(array);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void NullSafeHashCodeWithBooleanArrayEqualToNull()
        {
            Assert.Equal(0, ObjectUtils.NullSafeHashCode((bool[])null));
        }

        [Fact]
        public void NullSafeHashCodeWithByteArray()
        {
            var expected = (31 * 7) + 8;
            expected = (31 * expected) + 10;

            byte[] array = { 8, 10 };
            var actual = ObjectUtils.NullSafeHashCode(array);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void NullSafeHashCodeWithByteArrayEqualToNull()
        {
            Assert.Equal(0, ObjectUtils.NullSafeHashCode((byte[])null));
        }

        [Fact]
        public void NullSafeHashCodeWithCharArray()
        {
            var expected = (31 * 7) + 'a'.GetHashCode();
            expected = (31 * expected) + 'E'.GetHashCode();

            char[] array = { 'a', 'E' };
            var actual = ObjectUtils.NullSafeHashCode(array);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void NullSafeHashCodeWithCharArrayEqualToNull()
        {
            Assert.Equal(0, ObjectUtils.NullSafeHashCode((char[])null));
        }

        [Fact]
        public void NullSafeHashCodeWithDoubleArray()
        {
            var expected = (31 * 7) + ((double)8449.65d).GetHashCode();
            expected = (31 * expected) + ((double)9944.923d).GetHashCode();

            double[] array = { 8449.65, 9944.923 };
            var actual = ObjectUtils.NullSafeHashCode(array);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void NullSafeHashCodeWithDoubleArrayEqualToNull()
        {
            Assert.Equal(0, ObjectUtils.NullSafeHashCode((double[])null));
        }

        [Fact]
        public void NullSafeHashCodeWithFloatArray()
        {
            var expected = (31 * 7) + ((float)9.6f).GetHashCode();
            expected = (31 * expected) + ((float)7.4f).GetHashCode();

            float[] array = { 9.6f, 7.4f };
            var actual = ObjectUtils.NullSafeHashCode(array);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void NullSafeHashCodeWithFloatArrayEqualToNull()
        {
            Assert.Equal(0, ObjectUtils.NullSafeHashCode((float[])null));
        }

        [Fact]
        public void NullSafeHashCodeWithIntArray()
        {
            var expected = (31 * 7) + 884;
            expected = (31 * expected) + 340;

            int[] array = { 884, 340 };
            var actual = ObjectUtils.NullSafeHashCode(array);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void NullSafeHashCodeWithIntArrayEqualToNull()
        {
            Assert.Equal(0, ObjectUtils.NullSafeHashCode((int[])null));
        }

        [Fact]
        public void NullSafeHashCodeWithLongArray()
        {
            var lng = 7993L;
            var expected = (31 * 7) + (int)(lng ^ ((lng >> 32) & 0x0000FFFF));
            lng = 84320L;
            expected = (31 * expected) + (int)(lng ^ ((lng >> 32) & 0x0000FFFF));

            long[] array = { 7993L, 84320L };
            var actual = ObjectUtils.NullSafeHashCode(array);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void NullSafeHashCodeWithLongArrayEqualToNull()
        {
            Assert.Equal(0, ObjectUtils.NullSafeHashCode((long[])null));
        }

        [Fact]
        public void NullSafeHashCodeWithobject()
        {
            var str = "Luke";
            Assert.Equal(str.GetHashCode(), ObjectUtils.NullSafeHashCode(str));
        }

        [Fact]
        public void NullSafeHashCodeWithobjectArray()
        {
            var expected = (31 * 7) + "Leia".GetHashCode();
            expected = (31 * expected) + "Han".GetHashCode();

            object[] array = { "Leia", "Han" };
            var actual = ObjectUtils.NullSafeHashCode(array);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void NullSafeHashCodeWithobjectArrayEqualToNull()
        {
            Assert.Equal(0, ObjectUtils.NullSafeHashCode((object[])null));
        }

        [Fact]
        public void NullSafeHashCodeWithobjectBeingBooleanArray()
        {
            object array = new bool[] { true, false };
            var expected = ObjectUtils.NullSafeHashCode((bool[])array);
            AssertEqualHashCodes(expected, array);
        }

        [Fact]
        public void NullSafeHashCodeWithobjectBeingByteArray()
        {
            object array = new byte[] { 6, 39 };
            var expected = ObjectUtils.NullSafeHashCode((byte[])array);
            AssertEqualHashCodes(expected, array);
        }

        [Fact]
        public void NullSafeHashCodeWithobjectBeingCharArray()
        {
            object array = new char[] { 'l', 'M' };
            var expected = ObjectUtils.NullSafeHashCode((char[])array);
            AssertEqualHashCodes(expected, array);
        }

        [Fact]
        public void NullSafeHashCodeWithobjectBeingDoubleArray()
        {
            object array = new double[] { 68930.993, 9022.009 };
            var expected = ObjectUtils.NullSafeHashCode((double[])array);
            AssertEqualHashCodes(expected, array);
        }

        [Fact]
        public void NullSafeHashCodeWithobjectBeingFloatArray()
        {
            object array = new float[] { 9.9f, 9.54f };
            var expected = ObjectUtils.NullSafeHashCode((float[])array);
            AssertEqualHashCodes(expected, array);
        }

        [Fact]
        public void NullSafeHashCodeWithobjectBeingIntArray()
        {
            object array = new int[] { 89, 32 };
            var expected = ObjectUtils.NullSafeHashCode((int[])array);
            AssertEqualHashCodes(expected, array);
        }

        [Fact]
        public void NullSafeHashCodeWithobjectBeingLongArray()
        {
            object array = new long[] { 4389, 320 };
            var expected = ObjectUtils.NullSafeHashCode((long[])array);
            AssertEqualHashCodes(expected, array);
        }

        [Fact]
        public void NullSafeHashCodeWithobjectBeingobjectArray()
        {
            object array = new object[] { "Luke", "Anakin" };
            var expected = ObjectUtils.NullSafeHashCode((object[])array);
            AssertEqualHashCodes(expected, array);
        }

        [Fact]
        public void NullSafeHashCodeWithobjectBeingShortArray()
        {
            object array = new short[] { 5, 3 };
            var expected = ObjectUtils.NullSafeHashCode((short[])array);
            AssertEqualHashCodes(expected, array);
        }

        [Fact]
        public void NullSafeHashCodeWithobjectEqualToNull()
        {
            Assert.Equal(0, ObjectUtils.NullSafeHashCode((object)null));
        }

        [Fact]
        public void NullSafeHashCodeWithShortArray()
        {
            var expected = (31 * 7) + ((short)70).GetHashCode();
            expected = (31 * expected) + ((short)8).GetHashCode();

            short[] array = { 70, 8 };
            var actual = ObjectUtils.NullSafeHashCode(array);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void NullSafeHashCodeWithShortArrayEqualToNull()
        {
            Assert.Equal(0, ObjectUtils.NullSafeHashCode((short[])null));
        }

        private void AssertEqualHashCodes(int expected, object array)
        {
            var actual = ObjectUtils.NullSafeHashCode(array);
            Assert.Equal(expected, actual);
            Assert.True(array.GetHashCode() != actual);
        }
    }
}
