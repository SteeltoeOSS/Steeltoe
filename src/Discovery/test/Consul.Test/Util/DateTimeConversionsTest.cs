// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions.Extensions;
using Steeltoe.Discovery.Consul.Util;

namespace Steeltoe.Discovery.Consul.Test.Util;

public sealed class DateTimeConversionsTest
{
    [Fact]
    public void ToTimeSpan_ReturnsExpected()
    {
        Assert.Throws<ArgumentNullException>(() => DateTimeConversions.ToTimeSpan(null!));
        Assert.Throws<ArgumentException>(() => DateTimeConversions.ToTimeSpan(string.Empty));
        Assert.Throws<ArgumentException>(() => DateTimeConversions.ToTimeSpan(" "));
        Assert.Throws<FormatException>(() => DateTimeConversions.ToTimeSpan("foobar"));
        Assert.Equal(1.Seconds(), DateTimeConversions.ToTimeSpan("1000ms"));
        Assert.Equal(1000.Seconds(), DateTimeConversions.ToTimeSpan("1000s"));
        Assert.Equal(1.Hours(), DateTimeConversions.ToTimeSpan("1h"));
        Assert.Equal(1.Minutes(), DateTimeConversions.ToTimeSpan("1m"));
        Assert.Equal(1.Seconds(), DateTimeConversions.ToTimeSpan("1000Ms"));
        Assert.Equal(1000.Seconds(), DateTimeConversions.ToTimeSpan("1000S"));
        Assert.Equal(1.Hours(), DateTimeConversions.ToTimeSpan("1H"));
        Assert.Equal(1.Minutes(), DateTimeConversions.ToTimeSpan("1M"));
    }
}
