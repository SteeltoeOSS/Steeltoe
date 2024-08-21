// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.Metrics;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Metrics;

public sealed class MetricsEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var options = GetOptionsFromSettings<MetricsEndpointOptions>();
        Assert.Null(options.Enabled);
        Assert.Equal("metrics", options.Id);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appsettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/management",
            ["management:endpoints:metrics:enabled"] = "false",
            ["management:endpoints:metrics:id"] = "metricsmanagement"
        };

        MetricsEndpointOptions options = GetOptionsFromSettings<MetricsEndpointOptions, ConfigureMetricsEndpointOptions>(appsettings);
        Assert.False(options.Enabled);
        Assert.Equal("metricsmanagement", options.Id);
    }
}
