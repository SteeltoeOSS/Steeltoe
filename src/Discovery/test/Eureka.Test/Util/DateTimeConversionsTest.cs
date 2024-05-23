// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.Util;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test.Util;

public sealed class DateTimeConversionsTest
{
    [Fact]
    public void ToJavaMillis_Throws_IfNotUTC()
    {
        DateTime dt = new DateTime(2016, 3, 14, 16, 42, 21, DateTimeKind.Local).AddMilliseconds(708);
        var ex = Assert.Throws<ArgumentException>(() => DateTimeConversions.ToJavaMilliseconds(dt));
        Assert.Contains("DateTime kind must be UTC.", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ToJavaMillis()
    {
        DateTime dt = new DateTime(2016, 3, 14, 16, 42, 21, DateTimeKind.Utc).AddMilliseconds(708);
        long millis = DateTimeConversions.ToJavaMilliseconds(dt);
        Assert.Equal(1_457_973_741_708, millis);
    }

    [Fact]
    public void FromJavaMillis()
    {
        const long millis = 1_457_973_741_708;
        DateTime dt = DateTimeConversions.FromJavaMilliseconds(millis);
        Assert.Equal(3, dt.Month);
        Assert.Equal(14, dt.Day);
        Assert.Equal(2016, dt.Year);
        Assert.Equal(16, dt.Hour);
        Assert.Equal(42, dt.Minute);
        Assert.Equal(21, dt.Second);
        Assert.Equal(708, dt.Millisecond);
    }
}
