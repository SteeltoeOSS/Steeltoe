// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Support;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring;

public class DefaultComparatorUnitTests
{
    [Fact]
    public void TestPrimitives()
    {
        var comparator = new StandardTypeComparator();

        // primitive int
        Assert.True(comparator.Compare(1, 2) < 0);
        Assert.True(comparator.Compare(1, 1) == 0);
        Assert.True(comparator.Compare(2, 1) > 0);

        Assert.True(comparator.Compare(1.0d, 2) < 0);
        Assert.True(comparator.Compare(1.0d, 1) == 0);
        Assert.True(comparator.Compare(2.0d, 1) > 0);

        Assert.True(comparator.Compare(1.0f, 2) < 0);
        Assert.True(comparator.Compare(1.0f, 1) == 0);
        Assert.True(comparator.Compare(2.0f, 1) > 0);

        Assert.True(comparator.Compare(1L, 2) < 0);
        Assert.True(comparator.Compare(1L, 1) == 0);
        Assert.True(comparator.Compare(2L, 1) > 0);

        Assert.True(comparator.Compare(1, 2L) < 0);
        Assert.True(comparator.Compare(1, 1L) == 0);
        Assert.True(comparator.Compare(2, 1L) > 0);

        Assert.True(comparator.Compare(1L, 2L) < 0);
        Assert.True(comparator.Compare(1L, 1L) == 0);
        Assert.True(comparator.Compare(2L, 1L) > 0);
    }

    [Fact]
    public void TestNonPrimitiveNumbers()
    {
        var comparator = new StandardTypeComparator();

        const decimal bdOne = 1;
        const decimal bdTwo = 2;

        Assert.True(comparator.Compare(bdOne, bdTwo) < 0);
        Assert.True(comparator.Compare(bdOne, 1M) == 0);
        Assert.True(comparator.Compare(bdTwo, bdOne) > 0);

        Assert.True(comparator.Compare(1, bdTwo) < 0);
        Assert.True(comparator.Compare(1, bdOne) == 0);
        Assert.True(comparator.Compare(2, bdOne) > 0);

        Assert.True(comparator.Compare(1.0d, bdTwo) < 0);
        Assert.True(comparator.Compare(1.0d, bdOne) == 0);
        Assert.True(comparator.Compare(2.0d, bdOne) > 0);

        Assert.True(comparator.Compare(1.0f, bdTwo) < 0);
        Assert.True(comparator.Compare(1.0f, bdOne) == 0);
        Assert.True(comparator.Compare(2.0f, bdOne) > 0);

        Assert.True(comparator.Compare(1L, bdTwo) < 0);
        Assert.True(comparator.Compare(1L, bdOne) == 0);
        Assert.True(comparator.Compare(2L, bdOne) > 0);
    }

    [Fact]
    public void TestNulls()
    {
        var comparator = new StandardTypeComparator();
        Assert.True(comparator.Compare(null, "abc") < 0);
        Assert.True(comparator.Compare(null, null) == 0);
        Assert.True(comparator.Compare("abc", null) > 0);
    }

    [Fact]
    public void TestObjects()
    {
        var comparator = new StandardTypeComparator();
        Assert.True(comparator.Compare("a", "a") == 0);
        Assert.True(comparator.Compare("a", "b") < 0);
        Assert.True(comparator.Compare("b", "a") > 0);
    }

    [Fact]
    public void TestCanCompare()
    {
        var comparator = new StandardTypeComparator();
        Assert.True(comparator.CanCompare(null, 1));
        Assert.True(comparator.CanCompare(1, null));

        Assert.True(comparator.CanCompare(2, 1));
        Assert.True(comparator.CanCompare("abc", "def"));
        Assert.True(comparator.CanCompare("abc", 3));
        Assert.False(comparator.CanCompare(typeof(string), 3));
    }
}
