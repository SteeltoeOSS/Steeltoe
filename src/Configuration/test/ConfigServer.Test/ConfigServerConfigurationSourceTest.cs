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
            ["foo"] = "bar"
        }, NullLoggerFactory.Instance);

        source.DefaultOptions.Should().Be(options);
        source.Configuration.Should().BeNull();
        source.Sources.Should().NotBeSameAs(sources);
        source.Sources.Should().ContainSingle().Which.Should().Be(memSource);
        source.Properties.Should().ContainSingle();
        source.Properties.Should().ContainKey("foo").WhoseValue.Should().Be("bar");

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection().Build();
        source = new ConfigServerConfigurationSource(options, configurationRoot, NullLoggerFactory.Instance);
        source.DefaultOptions.Should().Be(options);

        ConfigurationRoot? root = source.Configuration.Should().BeOfType<ConfigurationRoot>().Subject;

        root.Should().BeSameAs(configurationRoot);
    }

    [Fact]
    public void Build_ReturnsProvider()
    {
        var options = new ConfigServerClientOptions();
        var memSource = new MemoryConfigurationSource();
        List<IConfigurationSource> sources = [memSource];

        var source = new ConfigServerConfigurationSource(options, sources, null, NullLoggerFactory.Instance);
        IConfigurationProvider provider = source.Build(new ConfigurationBuilder());

        provider.Should().BeOfType<ConfigServerConfigurationProvider>();
    }
}
