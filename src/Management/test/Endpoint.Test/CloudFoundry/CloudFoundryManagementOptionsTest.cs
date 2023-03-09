// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Options;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.CloudFoundry;

public class CloudfoundryManagementOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var opts = GetOptionsMonitorFromSettings<ManagementEndpointOptions>().Get(CFContext.Name);
        Assert.Equal("/cloudfoundryapplication", opts.Path);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["management:cloudfoundry:enabled"] = "false",
            ["management:endpoints:path"] = "/management"
        };


        var opts = GetOptionsMonitorFromSettings<ManagementEndpointOptions>(appsettings).Get(CFContext.Name);

        Assert.Equal("/cloudfoundryapplication", opts.Path);
        Assert.False(opts.Enabled);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly_OnCF()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", "somestuff");

        var appsettings = new Dictionary<string, string>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/management"
        };

        var opts = GetOptionsMonitorFromSettings<ManagementEndpointOptions>(appsettings).Get(CFContext.Name);

        Assert.Equal("/cloudfoundryapplication", opts.Path);
        Assert.False(opts.Enabled);
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
    }
}
