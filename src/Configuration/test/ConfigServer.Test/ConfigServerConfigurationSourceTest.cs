// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed class ConfigServerConfigurationSourceTest
{
    [Fact]
    public void Constructors_InitializesProperties()
    {
        var options = new ConfigServerClientOptions();
        var memSource = new MemoryConfigurationSource();

        List<IConfigurationSource> sources = [memSource];

        var source = new ConfigServerConfigurationSource(options, sources, new Dictionary<string, object>
        {
            { "foo", "bar" }
        }, NullLoggerFactory.Instance);

        Assert.Equal(options, source.DefaultOptions);
        Assert.Null(source.Configuration);
        Assert.NotSame(sources, source.Sources);
        Assert.Single(source.Sources);
        Assert.Equal(memSource, source.Sources[0]);
        Assert.Single(source.Properties);
        Assert.Equal("bar", source.Properties["foo"]);

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection().Build();
        source = new ConfigServerConfigurationSource(options, configurationRoot, NullLoggerFactory.Instance);
        Assert.Equal(options, source.DefaultOptions);
        Assert.NotNull(source.Configuration);
        var root = source.Configuration as IConfigurationRoot;
        Assert.NotNull(root);
        Assert.Same(configurationRoot, root);
    }

    [Fact]
    public void Build_ReturnsProvider()
    {
        var options = new ConfigServerClientOptions();
        var memSource = new MemoryConfigurationSource();

        List<IConfigurationSource> sources = [memSource];

        var source = new ConfigServerConfigurationSource(options, sources, null, NullLoggerFactory.Instance);
        IConfigurationProvider provider = source.Build(new ConfigurationBuilder());
        Assert.IsType<ConfigServerConfigurationProvider>(provider);
    }
}
