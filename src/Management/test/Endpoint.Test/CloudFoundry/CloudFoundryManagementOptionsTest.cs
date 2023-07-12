// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Options;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.CloudFoundry;

public class CloudFoundryManagementOptionsTest : BaseTest
{

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["management:cloudfoundry:enabled"] = "false",
            ["management:endpoints:path"] = "/management"
        };

        ManagementEndpointOptions opts = GetOptionsMonitorFromSettings<ManagementEndpointOptions>(appsettings).CurrentValue;

        Assert.False(opts.CloudFoundryEnabled);
    }

    [Fact]
    public void Constructor_BindsConfigurationDefaultsCorrectly()
    {
        var appsettings = new Dictionary<string, string>
        {
        };

        ManagementEndpointOptions opts = GetOptionsMonitorFromSettings<ManagementEndpointOptions>(appsettings).CurrentValue;

        Assert.True(opts.CloudFoundryEnabled);
    }
}

    
