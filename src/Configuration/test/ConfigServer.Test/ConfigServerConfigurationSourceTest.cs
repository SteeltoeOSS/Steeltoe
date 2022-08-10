// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test;

public class ConfigServerConfigurationSourceTest
{
    [Fact]
    public void Constructors__ThrowsIfNulls()
    {
        const ConfigServerClientSettings settings = null;

        Assert.Throws<ArgumentNullException>(() => new ConfigServerConfigurationSource((IConfiguration)null));
        Assert.Throws<ArgumentNullException>(() => new ConfigServerConfigurationSource(settings, null, null));
        Assert.Throws<ArgumentNullException>(() => new ConfigServerConfigurationSource((IList<IConfigurationSource>)null));
        Assert.Throws<ArgumentNullException>(() => new ConfigServerConfigurationSource(settings, new List<IConfigurationSource>()));
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
        Assert.Equal(factory, source.LogFactory);
        Assert.Null(source.Configuration);
        Assert.NotSame(sources, source.Sources);
        Assert.Single(source.Sources);
        Assert.Equal(memSource, source.Sources[0]);
        Assert.Single(source.Properties);
        Assert.Equal("bar", source.Properties["foo"]);

        IConfigurationRoot config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        source = new ConfigServerConfigurationSource(settings, config, factory);
        Assert.Equal(settings, source.DefaultSettings);
        Assert.Equal(factory, source.LogFactory);
        Assert.NotNull(source.Configuration);
        var root = source.Configuration as IConfigurationRoot;
        Assert.NotNull(root);
        Assert.Same(config, root);
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
