// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test;

public sealed class ConfigServerConfigurationSourceTest
{
    [Fact]
    public void Constructors_ThrowsIfNulls()
    {
        const IConfiguration nullConfiguration = null;
        const ConfigServerClientSettings nullConfigServerClientSettings = null;
        const ILoggerFactory nullLoggerFactory = null;
        const List<IConfigurationSource> nullSources = null;

        var configServerClientSettings = new ConfigServerClientSettings();
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        var sources = new List<IConfigurationSource>();
        var properties = new Dictionary<string, object>();

        Assert.Throws<ArgumentNullException>(() =>
            new ConfigServerConfigurationSource(nullConfigServerClientSettings, configuration, NullLoggerFactory.Instance));

        Assert.Throws<ArgumentNullException>(() =>
            new ConfigServerConfigurationSource(configServerClientSettings, nullConfiguration, NullLoggerFactory.Instance));

        Assert.Throws<ArgumentNullException>(() => new ConfigServerConfigurationSource(configServerClientSettings, configuration, nullLoggerFactory));

        Assert.Throws<ArgumentNullException>(() =>
            new ConfigServerConfigurationSource(nullConfigServerClientSettings, sources, properties, NullLoggerFactory.Instance));

        Assert.Throws<ArgumentNullException>(() =>
            new ConfigServerConfigurationSource(configServerClientSettings, nullSources, properties, NullLoggerFactory.Instance));

        Assert.Throws<ArgumentNullException>(() => new ConfigServerConfigurationSource(configServerClientSettings, sources, properties, null));
    }

    [Fact]
    public void Constructors__InitializesProperties()
    {
        var settings = new ConfigServerClientSettings();
        var memSource = new MemoryConfigurationSource();

        IList<IConfigurationSource> sources = new List<IConfigurationSource>
        {
            memSource
        };

        ILoggerFactory factory = new LoggerFactory();

        var source = new ConfigServerConfigurationSource(settings, sources, new Dictionary<string, object>
        {
            { "foo", "bar" }
        }, factory);

        Assert.Equal(settings, source.DefaultSettings);
        Assert.Equal(factory, source.LoggerFactory);
        Assert.Null(source.Configuration);
        Assert.NotSame(sources, source.Sources);
        Assert.Single(source.Sources);
        Assert.Equal(memSource, source.Sources[0]);
        Assert.Single(source.Properties);
        Assert.Equal("bar", source.Properties["foo"]);

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection().Build();
        source = new ConfigServerConfigurationSource(settings, configurationRoot, factory);
        Assert.Equal(settings, source.DefaultSettings);
        Assert.Equal(factory, source.LoggerFactory);
        Assert.NotNull(source.Configuration);
        var root = source.Configuration as IConfigurationRoot;
        Assert.NotNull(root);
        Assert.Same(configurationRoot, root);
    }

    [Fact]
    public void Build__ReturnsProvider()
    {
        var settings = new ConfigServerClientSettings();
        var memSource = new MemoryConfigurationSource();

        IList<IConfigurationSource> sources = new List<IConfigurationSource>
        {
            memSource
        };

        var source = new ConfigServerConfigurationSource(settings, sources);
        IConfigurationProvider provider = source.Build(new ConfigurationBuilder());
        Assert.IsType<ConfigServerConfigurationProvider>(provider);
    }
}
