// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Health.Contributor;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Health.Contributor;

public sealed class DiskSpaceContributorOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var options = new DiskSpaceContributorOptions();
        Assert.Equal(".", options.Path);
        Assert.Equal(10 * 1024 * 1024, options.Threshold);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appsettings = new Dictionary<string, string?>
        {
            ["management:endpoints:health:enabled"] = "true",
            ["management:endpoints:health:diskspace:path"] = "foobar",
            ["management:endpoints:health:diskspace:threshold"] = "5"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var options = new DiskSpaceContributorOptions(configurationRoot);
        Assert.Equal("foobar", options.Path);
        Assert.Equal(5, options.Threshold);
    }
}
