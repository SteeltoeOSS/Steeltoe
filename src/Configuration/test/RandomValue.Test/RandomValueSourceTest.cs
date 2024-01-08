// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Steeltoe.Configuration.RandomValue.Test;

public sealed class RandomValueSourceTest
{
    [Fact]
    public void Constructors_InitializesDefaults()
    {
        ILoggerFactory factory = new LoggerFactory();

        var source = new RandomValueSource(factory);
        Assert.Equal(factory, source.LoggerFactory);
        Assert.NotNull(source.Prefix);
        Assert.Equal("random:", source.Prefix);

        source = new RandomValueSource("foobar:", factory);
        Assert.Equal(factory, source.LoggerFactory);
        Assert.NotNull(source.Prefix);
        Assert.Equal("foobar:", source.Prefix);
    }

    [Fact]
    public void Build_ReturnsProvider()
    {
        var source = new RandomValueSource(NullLoggerFactory.Instance);
        IConfigurationProvider provider = source.Build(new ConfigurationBuilder());
        Assert.IsType<RandomValueProvider>(provider);
    }
}
