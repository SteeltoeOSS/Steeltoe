// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Order;
using Xunit;

namespace Steeltoe.Common.Test.Order;

public class OrderCompareTest
{
    private readonly OrderComparer orderComparer = OrderComparer.Instance;

    [Fact]
    public void CompareOrderedInstancesBefore()
    {
        Assert.Equal(-1, orderComparer.Compare(new StubOrdered(100), new StubOrdered(2000)));
    }

    [Fact]
    public void CompareOrderedInstancesSame()
    {
        Assert.Equal(0, orderComparer.Compare(new StubOrdered(100), new StubOrdered(100)));
    }

    [Fact]
    public void CcompareOrderedInstancesAfter()
    {
        Assert.Equal(1, orderComparer.Compare(new StubOrdered(982300), new StubOrdered(100)));
    }

    [Fact]
    public void CompareOrderedInstancesNullFirst()
    {
        Assert.Equal(1, orderComparer.Compare(null, new StubOrdered(100)));
    }

    [Fact]
    public void CompareOrderedInstancesNullLast()
    {
        Assert.Equal(-1, orderComparer.Compare(new StubOrdered(100), null));
    }

    [Fact]
    public void CompareOrderedInstancesDoubleNull()
    {
        Assert.Equal(0, orderComparer.Compare(null, null));
    }

    private sealed class StubOrdered : IOrdered
    {
        public StubOrdered(int order)
        {
            Order = order;
        }

        public int Order { get; }
    }
}