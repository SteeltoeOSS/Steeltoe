// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Util.Test;

public class DateTimeConversionsTest
{
    [Fact]
    public void ToJavaMillis_Throws_IfNotUTC()
    {
        var dt = new DateTime(2016, 3, 14, 16, 42, 21, DateTimeKind.Local).AddMilliseconds(708);
        var ex = Assert.Throws<ArgumentException>(() => DateTimeConversions.ToJavaMillis(dt));
        Assert.Contains("Kind != UTC", ex.Message);
    }

    [Fact]
    public void ToJavaMillis()
    {
        var dt = new DateTime(2016, 3, 14, 16, 42, 21, DateTimeKind.Utc).AddMilliseconds(708);
        var millis = DateTimeConversions.ToJavaMillis(dt);
        Assert.Equal(1_457_973_741_708, millis);
    }

    [Fact]
    public void FromJavaMillis()
    {
        var millis = 1_457_973_741_708;
        var dt = DateTimeConversions.FromJavaMillis(millis);
        Assert.Equal(3, dt.Month);
        Assert.Equal(14, dt.Day);
        Assert.Equal(2016, dt.Year);
        Assert.Equal(16, dt.Hour);
        Assert.Equal(42, dt.Minute);
        Assert.Equal(21, dt.Second);
        Assert.Equal(708, dt.Millisecond);
    }
}
