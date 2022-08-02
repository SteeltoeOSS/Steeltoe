// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Common.Util.Test;

public class ObjectUtilsTest
{
    [Fact]
    public void NullSafeEqualsWithArrays()
    {
        Assert.True(ObjectUtils.NullSafeEquals(new[]
        {
            "a",
            "b",
            "c"
        }, new[]
        {
            "a",
            "b",
            "c"
        }));

        Assert.True(ObjectUtils.NullSafeEquals(new[]
        {
            1,
            2,
            3
        }, new[]
        {
            1,
            2,
            3
        }));
    }

    [Fact]
    public void NullSafeHashCodeWithBooleanArray()
    {
        int expected = 31 * 7 + true.GetHashCode();
        expected = 31 * expected + false.GetHashCode();

        bool[] array =
        {
            true,
            false
        };

        int actual = ObjectUtils.NullSafeHashCode(array);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullSafeHashCodeWithBooleanArrayEqualToNull()
    {
        const bool[] value = null;
        Assert.Equal(0, ObjectUtils.NullSafeHashCode(value));
    }

    [Fact]
    public void NullSafeHashCodeWithByteArray()
    {
        int expected = 31 * 7 + 8;
        expected = 31 * expected + 10;

        byte[] array =
        {
            8,
            10
        };

        int actual = ObjectUtils.NullSafeHashCode(array);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullSafeHashCodeWithByteArrayEqualToNull()
    {
        const byte[] value = null;
        Assert.Equal(0, ObjectUtils.NullSafeHashCode(value));
    }

    [Fact]
    public void NullSafeHashCodeWithCharArray()
    {
        int expected = 31 * 7 + 'a'.GetHashCode();
        expected = 31 * expected + 'E'.GetHashCode();

        char[] array =
        {
            'a',
            'E'
        };

        int actual = ObjectUtils.NullSafeHashCode(array);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullSafeHashCodeWithCharArrayEqualToNull()
    {
        const char[] value = null;
        Assert.Equal(0, ObjectUtils.NullSafeHashCode(value));
    }

    [Fact]
    public void NullSafeHashCodeWithDoubleArray()
    {
        int expected = 31 * 7 + 8449.65d.GetHashCode();
        expected = 31 * expected + 9944.923d.GetHashCode();

        double[] array =
        {
            8449.65,
            9944.923
        };

        int actual = ObjectUtils.NullSafeHashCode(array);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullSafeHashCodeWithDoubleArrayEqualToNull()
    {
        const double[] value = null;
        Assert.Equal(0, ObjectUtils.NullSafeHashCode(value));
    }

    [Fact]
    public void NullSafeHashCodeWithFloatArray()
    {
        int expected = 31 * 7 + 9.6f.GetHashCode();
        expected = 31 * expected + 7.4f.GetHashCode();

        float[] array =
        {
            9.6f,
            7.4f
        };

        int actual = ObjectUtils.NullSafeHashCode(array);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullSafeHashCodeWithFloatArrayEqualToNull()
    {
        const float[] value = null;
        Assert.Equal(0, ObjectUtils.NullSafeHashCode(value));
    }

    [Fact]
    public void NullSafeHashCodeWithIntArray()
    {
        int expected = 31 * 7 + 884;
        expected = 31 * expected + 340;

        int[] array =
        {
            884,
            340
        };

        int actual = ObjectUtils.NullSafeHashCode(array);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullSafeHashCodeWithIntArrayEqualToNull()
    {
        const int[] value = null;
        Assert.Equal(0, ObjectUtils.NullSafeHashCode(value));
    }

    [Fact]
    public void NullSafeHashCodeWithLongArray()
    {
        long lng = 7993L;
        int expected = 31 * 7 + (int)(lng ^ ((lng >> 32) & 0x0000FFFF));
        lng = 84320L;
        expected = 31 * expected + (int)(lng ^ ((lng >> 32) & 0x0000FFFF));

        long[] array =
        {
            7993L,
            84320L
        };

        int actual = ObjectUtils.NullSafeHashCode(array);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullSafeHashCodeWithLongArrayEqualToNull()
    {
        const long[] value = null;
        Assert.Equal(0, ObjectUtils.NullSafeHashCode(value));
    }

    [Fact]
    public void NullSafeHashCodeWithObject()
    {
        string str = "Luke";
        Assert.Equal(str.GetHashCode(), ObjectUtils.NullSafeHashCode(str));
    }

    [Fact]
    public void NullSafeHashCodeWithObjectArray()
    {
        int expected = 31 * 7 + "Leia".GetHashCode();
        expected = 31 * expected + "Han".GetHashCode();

        object[] array =
        {
            "Leia",
            "Han"
        };

        int actual = ObjectUtils.NullSafeHashCode(array);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullSafeHashCodeWithObjectArrayEqualToNull()
    {
        const object[] value = null;
        Assert.Equal(0, ObjectUtils.NullSafeHashCode(value));
    }

    [Fact]
    public void NullSafeHashCodeWithObjectBeingBooleanArray()
    {
        object array = new[]
        {
            true,
            false
        };

        int expected = ObjectUtils.NullSafeHashCode((bool[])array);
        AssertEqualHashCodes(expected, array);
    }

    [Fact]
    public void NullSafeHashCodeWithObjectBeingByteArray()
    {
        object array = new byte[]
        {
            6,
            39
        };

        int expected = ObjectUtils.NullSafeHashCode((byte[])array);
        AssertEqualHashCodes(expected, array);
    }

    [Fact]
    public void NullSafeHashCodeWithObjectBeingCharArray()
    {
        object array = new[]
        {
            'l',
            'M'
        };

        int expected = ObjectUtils.NullSafeHashCode((char[])array);
        AssertEqualHashCodes(expected, array);
    }

    [Fact]
    public void NullSafeHashCodeWithObjectBeingDoubleArray()
    {
        object array = new[]
        {
            68930.993,
            9022.009
        };

        int expected = ObjectUtils.NullSafeHashCode((double[])array);
        AssertEqualHashCodes(expected, array);
    }

    [Fact]
    public void NullSafeHashCodeWithObjectBeingFloatArray()
    {
        object array = new[]
        {
            9.9f,
            9.54f
        };

        int expected = ObjectUtils.NullSafeHashCode((float[])array);
        AssertEqualHashCodes(expected, array);
    }

    [Fact]
    public void NullSafeHashCodeWithObjectBeingIntArray()
    {
        object array = new[]
        {
            89,
            32
        };

        int expected = ObjectUtils.NullSafeHashCode((int[])array);
        AssertEqualHashCodes(expected, array);
    }

    [Fact]
    public void NullSafeHashCodeWithObjectBeingLongArray()
    {
        object array = new long[]
        {
            4389,
            320
        };

        int expected = ObjectUtils.NullSafeHashCode((long[])array);
        AssertEqualHashCodes(expected, array);
    }

    [Fact]
    public void NullSafeHashCodeWithObjectBeingObjectArray()
    {
        object array = new object[]
        {
            "Luke",
            "Anakin"
        };

        int expected = ObjectUtils.NullSafeHashCode((object[])array);
        AssertEqualHashCodes(expected, array);
    }

    [Fact]
    public void NullSafeHashCodeWithObjectBeingShortArray()
    {
        object array = new short[]
        {
            5,
            3
        };

        int expected = ObjectUtils.NullSafeHashCode((short[])array);
        AssertEqualHashCodes(expected, array);
    }

    [Fact]
    public void NullSafeHashCodeWithObjectEqualToNull()
    {
        const object value = null;
        Assert.Equal(0, ObjectUtils.NullSafeHashCode(value));
    }

    [Fact]
    public void NullSafeHashCodeWithShortArray()
    {
        int expected = 31 * 7 + ((short)70).GetHashCode();
        expected = 31 * expected + ((short)8).GetHashCode();

        short[] array =
        {
            70,
            8
        };

        int actual = ObjectUtils.NullSafeHashCode(array);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullSafeHashCodeWithShortArrayEqualToNull()
    {
        const short[] value = null;
        Assert.Equal(0, ObjectUtils.NullSafeHashCode(value));
    }

    private void AssertEqualHashCodes(int expected, object array)
    {
        int actual = ObjectUtils.NullSafeHashCode(array);
        Assert.Equal(expected, actual);
        Assert.True(array.GetHashCode() != actual);
    }
}
