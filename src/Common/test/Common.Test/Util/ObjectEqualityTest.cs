// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using Steeltoe.Common.Util;
using Xunit;

namespace Steeltoe.Common.Test.Util;

public sealed class ObjectEqualityTest
{
    [Fact]
    public void EqualsWithArrays()
    {
        Assert.True(ObjectEquality.ObjectOrCollectionEquals(new[]
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

        Assert.True(ObjectEquality.ObjectOrCollectionEquals(new[]
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
    public void NullSafeHashCodeWithCharArray()
    {
        char[] array =
        {
            'a',
            'E'
        };

        int expected = ComputeCollectionHashCode(array);
        int actual = ObjectEquality.GetObjectOrCollectionHashCode(array);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullSafeHashCodeWithCharArrayEqualToNull()
    {
        const char[] value = null;
        Assert.Equal(0, ObjectEquality.GetObjectOrCollectionHashCode(value));
    }

    [Fact]
    public void NullSafeHashCodeWithDoubleArray()
    {
        double[] array =
        {
            8449.65,
            9944.923
        };

        int expected = ComputeCollectionHashCode(array);
        int actual = ObjectEquality.GetObjectOrCollectionHashCode(array);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullSafeHashCodeWithDoubleArrayEqualToNull()
    {
        const double[] value = null;
        Assert.Equal(0, ObjectEquality.GetObjectOrCollectionHashCode(value));
    }

    [Fact]
    public void NullSafeHashCodeWithObjectArray()
    {
        object[] array =
        {
            "Leia",
            "Han"
        };

        int expected = ComputeCollectionHashCode(array);
        int actual = ObjectEquality.GetObjectOrCollectionHashCode(array);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullSafeHashCodeWithObjectArrayEqualToNull()
    {
        const object[] value = null;
        Assert.Equal(0, ObjectEquality.GetObjectOrCollectionHashCode(value));
    }

    [Fact]
    public void NullSafeHashCodeWithObjectBeingBooleanArray()
    {
        object array = new[]
        {
            true,
            false
        };

        int expected = ComputeCollectionHashCode((bool[])array);
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

        int expected = ComputeCollectionHashCode((object[])array);
        AssertEqualHashCodes(expected, array);
    }

    [Fact]
    public void NullSafeHashCodeWithObjectEqualToNull()
    {
        const object value = null;
        Assert.Equal(0, ObjectEquality.GetObjectOrCollectionHashCode(value));
    }

    private void AssertEqualHashCodes(int expected, object array)
    {
        int actual = ObjectEquality.GetObjectOrCollectionHashCode(array);
        Assert.Equal(expected, actual);
        Assert.True(array.GetHashCode() != actual);
    }

    private int ComputeCollectionHashCode(IEnumerable enumerable)
    {
        HashCode hashCode = default;

        foreach (object item in enumerable)
        {
            hashCode.Add(item);
        }

        return hashCode.ToHashCode();
    }
}
