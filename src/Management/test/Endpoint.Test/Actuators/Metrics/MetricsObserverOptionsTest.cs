// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.Metrics;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Metrics;

public sealed class MetricsObserverOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        MetricsObserverOptions options = GetOptionsFromSettings<MetricsObserverOptions, ConfigureMetricsObserverOptions>();

        Assert.Equal(ConfigureMetricsObserverOptions.DefaultIngressIgnorePattern, options.IngressIgnorePattern);
        Assert.Equal(ConfigureMetricsObserverOptions.DefaultEgressIgnorePattern, options.EgressIgnorePattern);
        Assert.True(options.AspNetCoreHosting);
        Assert.True(options.GCEvents);
        Assert.False(options.EventCounterEvents);
        Assert.Equal(1, options.EventCounterIntervalSec);
        Assert.True(options.ThreadPoolEvents);
        Assert.False(options.HttpClientCore);
        Assert.False(options.HttpClientDesktop);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:metrics:observer:ingressIgnorePattern"] = "pattern",
            ["management:metrics:observer:egressIgnorePattern"] = "pattern",
            ["management:metrics:observer:aspnetcoreHosting"] = "false",
            ["management:metrics:observer:gcEvents"] = "false",
            ["management:metrics:observer:eventCounterEvents"] = "true",
            ["management:metrics:observer:eventCounterIntervalSec"] = "5",
            ["management:metrics:observer:threadPoolEvents"] = "false",
            ["management:metrics:observer:httpClientCore"] = "true",
            ["management:metrics:observer:httpClientDesktop"] = "true"
        };

        MetricsObserverOptions options = GetOptionsFromSettings<MetricsObserverOptions, ConfigureMetricsObserverOptions>(appSettings);

        Assert.Equal("pattern", options.IngressIgnorePattern);
        Assert.Equal("pattern", options.EgressIgnorePattern);
        Assert.False(options.AspNetCoreHosting);
        Assert.False(options.GCEvents);
        Assert.True(options.EventCounterEvents);
        Assert.Equal(5, options.EventCounterIntervalSec);
        Assert.False(options.ThreadPoolEvents);
        Assert.True(options.HttpClientCore);
        Assert.True(options.HttpClientDesktop);
    }
}
