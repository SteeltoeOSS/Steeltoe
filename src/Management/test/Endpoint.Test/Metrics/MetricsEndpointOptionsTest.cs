// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Test;
using Xunit;

namespace Steeltoe.Management.Endpoint.Metrics.Test;

public class MetricsEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var opts = new MetricsEndpointOptions();
        Assert.Null(opts.Enabled);
        Assert.Equal("metrics", opts.Id);
    }

    [Fact]
    public void Constructor_ThrowsIfConfigNull()
    {
        const IConfiguration config = null;
        Assert.Throws<ArgumentNullException>(() => new MetricsEndpointOptions(config));
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/management",
            ["management:endpoints:metrics:enabled"] = "false",
            ["management:endpoints:metrics:id"] = "metricsmanagement"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        IConfigurationRoot config = configurationBuilder.Build();

        var opts = new MetricsEndpointOptions(config);
        Assert.False(opts.Enabled);
        Assert.Equal("metricsmanagement", opts.Id);
    }
}
