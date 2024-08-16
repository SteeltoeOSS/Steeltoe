// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Hypermedia;

public sealed class ActuatorManagementOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        ManagementOptions managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>().CurrentValue;
        Assert.Equal("/actuator", managementOptions.Path);
        Assert.NotNull(managementOptions.Exposure);
        Assert.Contains("health", managementOptions.Exposure.Include);
        Assert.Contains("info", managementOptions.Exposure.Include);
    }

    [Fact]
    public void Constructor_InitializesWithDefaultsOnCF()
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", "something");
        ManagementOptions managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>().CurrentValue;

        Assert.Equal("/actuator", managementOptions.Path);
        Assert.NotNull(managementOptions.Exposure);
        Assert.Contains("health", managementOptions.Exposure.Include);
        Assert.Contains("info", managementOptions.Exposure.Include);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appsettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/management"
        };

        var options = GetOptionsFromSettings<ManagementOptions>(appsettings);

        Assert.Equal("/management", options.Path);
        Assert.False(options.Enabled);

        Assert.NotNull(options.Exposure);
        Assert.Contains("health", options.Exposure.Include);
        Assert.Contains("info", options.Exposure.Include);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly_OnCF()
    {
        var appsettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/management"
        };

        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", "something");

        var options = GetOptionsFromSettings<ManagementOptions>(appsettings);

        Assert.Equal("/management", options.Path);
        Assert.False(options.Enabled);

        Assert.NotNull(options.Exposure);
        Assert.Contains("health", options.Exposure.Include);
        Assert.Contains("info", options.Exposure.Include);
    }
}
