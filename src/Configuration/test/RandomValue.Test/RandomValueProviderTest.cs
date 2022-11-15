// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Steeltoe.Configuration.RandomValue.Test;

public sealed class RandomValueProviderTest
{
    [Fact]
    public void Constructor_ThrowsIfNulls()
    {
        var loggerFactory = NullLoggerFactory.Instance;

        Assert.Throws<ArgumentNullException>(() => new RandomValueProvider(null, loggerFactory));
        Assert.Throws<ArgumentNullException>(() => new RandomValueProvider("random:", null));
    }

    [Fact]
    public void TryGet_Ignores()
    {
        var prov = new RandomValueProvider("random:", NullLoggerFactory.Instance);
        prov.TryGet("foo:bar", out string value);
        Assert.Null(value);
    }

    [Fact]
    public void TryGet_String()
    {
        var prov = new RandomValueProvider("random:", NullLoggerFactory.Instance);
        prov.TryGet("random:string", out string value);
        Assert.NotNull(value);
    }

    [Fact]
    public void TryGet_Uuid()
    {
        var prov = new RandomValueProvider("random:", NullLoggerFactory.Instance);
        prov.TryGet("random:uuid", out string value);
        Assert.NotNull(value);
    }

    [Fact]
    public void TryGet_RandomInt()
    {
        var prov = new RandomValueProvider("random:", NullLoggerFactory.Instance);
        prov.TryGet("random:int", out string value);
        Assert.NotNull(value);
    }

    [Fact]
    public void TryGet_RandomIntRange()
    {
        var prov = new RandomValueProvider("random:", NullLoggerFactory.Instance);
        prov.TryGet("random:int[4,10]", out string value);
        Assert.NotNull(value);
        int val = int.Parse(value, CultureInfo.InvariantCulture);
        Assert.InRange(val, 4, 10);
    }

    [Fact]
    public void TryGet_RandomIntMax()
    {
        var prov = new RandomValueProvider("random:", NullLoggerFactory.Instance);
        prov.TryGet("random:int(10)", out string value);
        Assert.NotNull(value);
        int val = int.Parse(value, CultureInfo.InvariantCulture);
        Assert.InRange(val, 0, 10);
    }

    [Fact]
    public void TryGet_RandomLong()
    {
        var prov = new RandomValueProvider("random:", NullLoggerFactory.Instance);
        prov.TryGet("random:long", out string value);
        Assert.NotNull(value);
    }

    [Fact]
    public void TryGet_RandomLongRange()
    {
        var prov = new RandomValueProvider("random:", NullLoggerFactory.Instance);
        prov.TryGet("random:long[4,10]", out string value);
        Assert.NotNull(value);
        int val = int.Parse(value, CultureInfo.InvariantCulture);
        Assert.InRange(val, 4, 10);
    }

    [Fact]
    public void TryGet_RandomLongMax()
    {
        var prov = new RandomValueProvider("random:", NullLoggerFactory.Instance);
        prov.TryGet("random:long(10)", out string value);
        Assert.NotNull(value);
        int val = int.Parse(value, CultureInfo.InvariantCulture);
        Assert.InRange(val, 0, 10);
    }
}
