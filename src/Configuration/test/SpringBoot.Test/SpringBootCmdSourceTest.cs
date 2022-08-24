// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Extensions.Configuration.SpringBoot.Test;

public class SpringBootCmdSourceTest
{
    [Fact]
    public void Constructors__InitializesDefaults()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddCommandLine(Array.Empty<string>()).Build();

        var source = new SpringBootCmdSource(configurationRoot);
        Assert.Equal(configurationRoot, source.InnerConfiguration);
    }

    [Fact]
    public void Build__ReturnsProvider()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddCommandLine(Array.Empty<string>()).Build();

        var source = new SpringBootCmdSource(configurationRoot);
        IConfigurationProvider provider = source.Build(new ConfigurationBuilder());
        Assert.IsType<SpringBootCmdProvider>(provider);
    }
}
