// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test.Actuators.CloudFoundry;

public sealed class CloudFoundryManagementOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:cloudfoundry:enabled"] = "false",
            ["management:endpoints:path"] = "/management"
        };

        ManagementOptions options = GetOptionsMonitorFromSettings<ManagementOptions>(appSettings).CurrentValue;

        Assert.False(options.IsCloudFoundryEnabled);
    }

    [Fact]
    public void Constructor_BindsConfigurationDefaultsCorrectly()
    {
        var appSettings = new Dictionary<string, string?>();

        ManagementOptions options = GetOptionsMonitorFromSettings<ManagementOptions>(appSettings).CurrentValue;

        Assert.True(options.IsCloudFoundryEnabled);
    }
}
