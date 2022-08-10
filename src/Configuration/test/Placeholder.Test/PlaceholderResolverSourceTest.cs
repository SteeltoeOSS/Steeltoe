// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Steeltoe.Extensions.Configuration.Placeholder.Test;

public class PlaceholderResolverSourceTest
{
    [Fact]
    public void Constructor_ThrowsIfNulls()
    {
        const IList<IConfigurationSource> sources = null;

        Assert.Throws<ArgumentNullException>(() => new PlaceholderResolverSource(sources));
    }

    [Fact]
    public void Constructors_InitializesProperties()
    {
        var memSource = new MemoryConfigurationSource();

        var sources = new List<IConfigurationSource>
        {
            memSource
        };

        var factory = new LoggerFactory();

        var source = new PlaceholderResolverSource(sources, factory);
        Assert.Equal(factory, source.LoggerFactory);
        Assert.NotNull(source.Sources);
        Assert.Single(source.Sources);
        Assert.NotSame(sources, source.Sources);
        Assert.Contains(memSource, source.Sources);
    }

    [Fact]
    public void Build_ReturnsProvider()
    {
        var memSource = new MemoryConfigurationSource();

        IList<IConfigurationSource> sources = new List<IConfigurationSource>
        {
            memSource
        };

        var source = new PlaceholderResolverSource(sources);
        IConfigurationProvider provider = source.Build(new ConfigurationBuilder());
        Assert.IsType<PlaceholderResolverProvider>(provider);
    }
}
