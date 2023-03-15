// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Diagnostics;
using Steeltoe.Management.Endpoint.Metrics;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Metrics;

public class MetricsObserverOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        MetricsObserverOptions opts = GetOptionsFromSettings<MetricsObserverOptions, ConfigureMetricsObserverOptions>();

        Assert.Equal(ConfigureMetricsObserverOptions.DefaultIngressIgnorePattern, opts.IngressIgnorePattern);
        Assert.Equal(ConfigureMetricsObserverOptions.DefaultEgressIgnorePattern, opts.EgressIgnorePattern);
        Assert.True(opts.AspNetCoreHosting);
        Assert.True(opts.GCEvents);
        Assert.False(opts.EventCounterEvents);
        Assert.True(opts.ThreadPoolEvents);
        Assert.False(opts.HttpClientCore);
        Assert.False(opts.HttpClientDesktop);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["management:metrics:observer:ingressIgnorePattern"] = "pattern",
            ["management:metrics:observer:egressIgnorePattern"] = "pattern",
            ["management:metrics:observer:aspnetcoreHosting"] = "false",
            ["management:metrics:observer:gcEvents"] = "false",
            ["management:metrics:observer:eventCounterEvents"] = "true",
            ["management:metrics:observer:threadPoolEvents"] = "false",
            ["management:metrics:observer:httpClientCore"] = "true",
            ["management:metrics:observer:httpClientDesktop"] = "true"
        };

        MetricsObserverOptions opts = GetOptionsFromSettings<MetricsObserverOptions, ConfigureMetricsObserverOptions>(appsettings);

        Assert.Equal("pattern", opts.IngressIgnorePattern);
        Assert.Equal("pattern", opts.EgressIgnorePattern);
        Assert.False(opts.AspNetCoreHosting);
        Assert.False(opts.GCEvents);
        Assert.True(opts.EventCounterEvents);
        Assert.False(opts.ThreadPoolEvents);
        Assert.True(opts.HttpClientCore);
        Assert.True(opts.HttpClientDesktop);
    }
}
