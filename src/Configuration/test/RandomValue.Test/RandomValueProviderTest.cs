// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Steeltoe.Configuration.RandomValue.Test;

public sealed class RandomValueProviderTest
{
    [Fact]
    public void TryGet_Ignores()
    {
        var provider = new RandomValueProvider("random:", NullLoggerFactory.Instance);
        provider.TryGet("foo:bar", out string? value);
        value.Should().BeNull();
    }

    [Fact]
    public void TryGet_String()
    {
        var provider = new RandomValueProvider("random:", NullLoggerFactory.Instance);
        provider.TryGet("random:string", out string? value);
        value.Should().NotBeNull();
    }

    [Fact]
    public void TryGet_Uuid()
    {
        var provider = new RandomValueProvider("random:", NullLoggerFactory.Instance);
        provider.TryGet("random:uuid", out string? value);
        value.Should().NotBeNull();
    }

    [Fact]
    public void TryGet_RandomInt()
    {
        var provider = new RandomValueProvider("random:", NullLoggerFactory.Instance);
        provider.TryGet("random:int", out string? value);
        value.Should().NotBeNull();
    }

    [Fact]
    public void TryGet_RandomIntRange()
    {
        var provider = new RandomValueProvider("random:", NullLoggerFactory.Instance);
        provider.TryGet("random:int[4,10]", out string? value);
        value.Should().NotBeNull();

        int number = int.Parse(value, CultureInfo.InvariantCulture);
        number.Should().BeInRange(4, 10);
    }

    [Fact]
    public void TryGet_RandomIntMax()
    {
        var provider = new RandomValueProvider("random:", NullLoggerFactory.Instance);
        provider.TryGet("random:int(10)", out string? value);
        value.Should().NotBeNull();

        int number = int.Parse(value, CultureInfo.InvariantCulture);
        number.Should().BeInRange(0, 10);
    }

    [Fact]
    public void TryGet_RandomLong()
    {
        var provider = new RandomValueProvider("random:", NullLoggerFactory.Instance);
        provider.TryGet("random:long", out string? value);
        value.Should().NotBeNull();
    }

    [Fact]
    public void TryGet_RandomLongRange()
    {
        var provider = new RandomValueProvider("random:", NullLoggerFactory.Instance);
        provider.TryGet("random:long[4,10]", out string? value);
        value.Should().NotBeNull();

        int number = int.Parse(value, CultureInfo.InvariantCulture);
        number.Should().BeInRange(4, 10);
    }

    [Fact]
    public void TryGet_RandomLongMax()
    {
        var provider = new RandomValueProvider("random:", NullLoggerFactory.Instance);
        provider.TryGet("random:long(10)", out string? value);
        value.Should().NotBeNull();

        int number = int.Parse(value, CultureInfo.InvariantCulture);
        number.Should().BeInRange(0, 10);
    }
}
