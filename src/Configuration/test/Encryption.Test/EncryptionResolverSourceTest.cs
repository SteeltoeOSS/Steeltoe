// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Steeltoe.Configuration.Encryption.Test;

public sealed class EncryptionResolverSourceTest
{
    [Fact]
    public void Constructor_WithSources_ThrowsIfNulls()
    {
        const IList<IConfigurationSource> nullSources = null;
        var sources = new List<IConfigurationSource>();
        var loggerFactory = NullLoggerFactory.Instance;

        Assert.Throws<ArgumentNullException>(() => new EncryptionResolverSource(nullSources, loggerFactory));
        Assert.Throws<ArgumentNullException>(() => new EncryptionResolverSource(sources, null));
    }

    [Fact]
    public void Constructor_WithConfiguration_ThrowsIfNulls()
    {
        const IConfigurationRoot nullConfiguration = null;
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        var loggerFactory = NullLoggerFactory.Instance;

        Assert.Throws<ArgumentNullException>(() => new EncryptionResolverSource(nullConfiguration, loggerFactory));
        Assert.Throws<ArgumentNullException>(() => new EncryptionResolverSource(configuration, null));
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

        var source = new EncryptionResolverSource(sources, factory);
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

        var source = new EncryptionResolverSource(sources, NullLoggerFactory.Instance);
        IConfigurationProvider provider = source.Build(new ConfigurationBuilder());
        Assert.IsType<EncryptionResolverProvider>(provider);
    }
}
