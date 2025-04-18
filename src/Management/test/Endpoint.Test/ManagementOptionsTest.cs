// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.Info;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test;

public sealed class ManagementOptionsTest : BaseTest
{
    [Fact]
    public void InitializedWithDefaults()
    {
        ManagementOptions managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>().CurrentValue;
        Assert.False(managementOptions.Enabled.HasValue);
        Assert.Equal("/actuator", managementOptions.Path);
        Assert.NotNull(managementOptions.Exposure);
        Assert.Contains("health", managementOptions.Exposure.Include);
        Assert.Contains("info", managementOptions.Exposure.Include);
    }

    [Fact]
    public void BindsConfigurationCorrectly()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/management"
        };

        ManagementOptions options = GetOptionsMonitorFromSettings<ManagementOptions>(appSettings).CurrentValue;
        Assert.False(options.Enabled);
        Assert.Equal("/management", options.Path);
    }

    [Fact]
    public void IsExposedCorrectly()
    {
        var managementOptions = new ManagementOptions
        {
            Exposure =
            {
                Exclude =
                {
                    "*"
                }
            }
        };

        var options = GetOptionsFromSettings<InfoEndpointOptions>();
        Assert.False(options.IsExposed(managementOptions));
    }
}
