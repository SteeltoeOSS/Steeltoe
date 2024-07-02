// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Steeltoe.Configuration.Placeholder.Test;

public sealed class PlaceholderResolverSourceTest
{
    [Fact]
    public void Constructors_InitializesProperties()
    {
        var memorySource = new MemoryConfigurationSource();

        var sources = new List<IConfigurationSource>
        {
            memorySource
        };

        var factory = new LoggerFactory();

        var placeholderSource = new PlaceholderResolverSource(sources, factory);
        Assert.Equal(factory, placeholderSource.LoggerFactory);
        Assert.NotNull(placeholderSource.Sources);
        Assert.Single(placeholderSource.Sources);
        Assert.NotSame(sources, placeholderSource.Sources);
        Assert.Contains(memorySource, placeholderSource.Sources);
    }

    [Fact]
    public void Build_ReturnsProvider()
    {
        var memorySource = new MemoryConfigurationSource();

        IList<IConfigurationSource> sources = new List<IConfigurationSource>
        {
            memorySource
        };

        var placeholderSource = new PlaceholderResolverSource(sources, NullLoggerFactory.Instance);
        IConfigurationProvider provider = placeholderSource.Build(new ConfigurationBuilder());
        Assert.IsType<PlaceholderResolverProvider>(provider);
    }
}
