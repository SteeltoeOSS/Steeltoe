// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Options;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Hypermedia;

public class ActuatorManagementOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        ManagementEndpointOptions opts = GetOptionsMonitorFromSettings<ManagementEndpointOptions>().Get(EndpointContexts.Actuator);
        Assert.Equal("/actuator", opts.Path);
        Assert.Contains("health", opts.Exposure.Include);
        Assert.Contains("info", opts.Exposure.Include);
    }

    [Fact]
    public void Constructor_InitializesWithDefaultsOnCF()
    {
        System.Environment.SetEnvironmentVariable("VCAP_APPLICATION", "something");
        ManagementEndpointOptions opts = GetOptionsMonitorFromSettings<ManagementEndpointOptions>().Get(EndpointContexts.Actuator);

        Assert.Equal("/actuator", opts.Path);
        Assert.Contains("health", opts.Exposure.Include);
        Assert.Contains("info", opts.Exposure.Include);

        System.Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/management"
        };

        var opts = GetOptionsFromSettings<ManagementEndpointOptions>(appsettings);

        Assert.Equal("/management", opts.Path);
        Assert.False(opts.Enabled);

        Assert.Contains("health", opts.Exposure.Include);
        Assert.Contains("info", opts.Exposure.Include);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly_OnCF()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/management"
        };

        System.Environment.SetEnvironmentVariable("VCAP_APPLICATION", "something");

        var opts = GetOptionsFromSettings<ManagementEndpointOptions>(appsettings);

        Assert.Equal("/management", opts.Path);
        Assert.False(opts.Enabled);

        Assert.Contains("health", opts.Exposure.Include);
        Assert.Contains("info", opts.Exposure.Include);

        System.Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
    }
}
