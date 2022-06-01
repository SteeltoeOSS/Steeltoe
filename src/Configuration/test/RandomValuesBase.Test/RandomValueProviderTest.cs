// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Steeltoe.Extensions.Configuration.RandomValue.Test;

public class RandomValueProviderTest
{
    [Fact]
    public void Constructor__ThrowsIfPrefixNull()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new RandomValueProvider(null));
    }

    [Fact]
    public void TryGet_Ignores()
    {
        var prov = new RandomValueProvider("random:");
        prov.TryGet("foo:bar", out var value);
        Assert.Null(value);
    }

    [Fact]
    public void TryGet_String()
    {
        var prov = new RandomValueProvider("random:");
        prov.TryGet("random:string", out var value);
        Assert.NotNull(value);
    }

    [Fact]
    public void TryGet_Uuid()
    {
        var prov = new RandomValueProvider("random:");
        prov.TryGet("random:uuid", out var value);
        Assert.NotNull(value);
    }

    [Fact]
    public void TryGet_RandomInt()
    {
        var prov = new RandomValueProvider("random:");
        prov.TryGet("random:int", out var value);
        Assert.NotNull(value);
    }

    [Fact]
    public void TryGet_RandomIntRange()
    {
        var prov = new RandomValueProvider("random:");
        prov.TryGet("random:int[4,10]", out var value);
        Assert.NotNull(value);
        var val = int.Parse(value);
        Assert.InRange(val, 4, 10);
    }

    [Fact]
    public void TryGet_RandomIntMax()
    {
        var prov = new RandomValueProvider("random:");
        prov.TryGet("random:int(10)", out var value);
        Assert.NotNull(value);
        var val = int.Parse(value);
        Assert.InRange(val, 0, 10);
    }

    [Fact]
    public void TryGet_RandomLong()
    {
        var prov = new RandomValueProvider("random:");
        prov.TryGet("random:long", out var value);
        Assert.NotNull(value);
    }

    [Fact]
    public void TryGet_RandomLongRange()
    {
        var prov = new RandomValueProvider("random:");
        prov.TryGet("random:long[4,10]", out var value);
        Assert.NotNull(value);
        var val = int.Parse(value);
        Assert.InRange(val, 4, 10);
    }

    [Fact]
    public void TryGet_RandomLongMax()
    {
        var prov = new RandomValueProvider("random:");
        prov.TryGet("random:long(10)", out var value);
        Assert.NotNull(value);
        var val = int.Parse(value);
        Assert.InRange(val, 0, 10);
    }
}
