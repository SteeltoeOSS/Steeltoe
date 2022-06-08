// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Steeltoe.Extensions.Configuration.RandomValue.Test;

public class RandomValueSourceTest
{
    [Fact]
    public void Constructors__InitializesDefaults()
    {
        ILoggerFactory factory = new LoggerFactory();

        var source = new RandomValueSource(factory);
        Assert.Equal(factory, source._loggerFactory);
        Assert.NotNull(source._prefix);
        Assert.Equal(RandomValueSource.PREFIX, source._prefix);

        source = new RandomValueSource("foobar:", factory);
        Assert.Equal(factory, source._loggerFactory);
        Assert.NotNull(source._prefix);
        Assert.Equal("foobar:", source._prefix);
    }

    [Fact]
    public void Build__ReturnsProvider()
    {
        ILoggerFactory factory = new LoggerFactory();

        var source = new RandomValueSource();
        var provider = source.Build(new ConfigurationBuilder());
        Assert.IsType<RandomValueProvider>(provider);
    }
}
