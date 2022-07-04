// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using Xunit;

namespace Steeltoe.Extensions.Configuration.SpringBoot.Test;

public class SpringBootCmdSourceTest
{
    [Fact]
    public void Constructors__InitializesDefaults()
    {
        var config = new ConfigurationBuilder()
            .AddCommandLine(Array.Empty<string>())
            .Build();

        var source = new SpringBootCmdSource(config);
        Assert.Equal(config, source.Config);
    }

    [Fact]
    public void Build__ReturnsProvider()
    {
        var config = new ConfigurationBuilder()
            .AddCommandLine(Array.Empty<string>())
            .Build();

        var source = new SpringBootCmdSource(config);
        var provider = source.Build(new ConfigurationBuilder());
        Assert.IsType<SpringBootCmdProvider>(provider);
    }
}
