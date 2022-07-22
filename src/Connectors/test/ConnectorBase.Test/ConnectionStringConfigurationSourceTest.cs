// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Connector.Test;

public class ConnectionStringConfigurationSourceTest
{
    [Fact]
    public void Constructor_ThrowsOnNullSources()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new ConnectionStringConfigurationSource(null));
        Assert.Equal("sources", ex.ParamName);
    }

    [Fact]
    public void Constructor_InitializesProperties()
    {
        var memSource = new MemoryConfigurationSource();
        IList<IConfigurationSource> sources = new List<IConfigurationSource>() { memSource };

        var source = new ConnectionStringConfigurationSource(sources);
        Assert.NotNull(source._sources);
        Assert.Single(source._sources);
        Assert.NotSame(sources, source._sources);
        Assert.Contains(memSource, source._sources);
    }

    [Fact]
    public void Build_ReturnsProvider()
    {
        var memSource = new MemoryConfigurationSource();
        IList<IConfigurationSource> sources = new List<IConfigurationSource>() { memSource };

        var source = new ConnectionStringConfigurationSource(sources);
        var provider = source.Build(new ConfigurationBuilder());
        Assert.IsType<ConnectionStringConfigurationProvider>(provider);
    }
}