// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions.Extensions;
using Steeltoe.Discovery.Consul.Util;

namespace Steeltoe.Discovery.Consul.Test.Util;

public sealed class DateTimeConversionsTest
{
    [Fact]
    public void ToTimeSpan_Null_Throws()
    {
        Action action = () => DateTimeConversions.ToTimeSpan(null!);

        action.Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void ToTimeSpan_Empty_Throws()
    {
        Action action = () => DateTimeConversions.ToTimeSpan(string.Empty);

        action.Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void ToTimeSpan_Space_Throws()
    {
        Action action = () => DateTimeConversions.ToTimeSpan(" ");

        action.Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void ToTimeSpan_Invalid_Throws()
    {
        Action action = () => DateTimeConversions.ToTimeSpan("foobar");

        action.Should().ThrowExactly<FormatException>();
    }

    [Fact]
    public void ToTimeSpan_ReturnsExpected()
    {
        DateTimeConversions.ToTimeSpan("1000ms").Should().Be(1.Seconds());
        DateTimeConversions.ToTimeSpan("1000s").Should().Be(1000.Seconds());
        DateTimeConversions.ToTimeSpan("1h").Should().Be(1.Hours());
        DateTimeConversions.ToTimeSpan("1m").Should().Be(1.Minutes());
        DateTimeConversions.ToTimeSpan("1000Ms").Should().Be(1.Seconds());
        DateTimeConversions.ToTimeSpan("1000S").Should().Be(1000.Seconds());
        DateTimeConversions.ToTimeSpan("1H").Should().Be(1.Hours());
        DateTimeConversions.ToTimeSpan("1M").Should().Be(1.Minutes());
    }
}
