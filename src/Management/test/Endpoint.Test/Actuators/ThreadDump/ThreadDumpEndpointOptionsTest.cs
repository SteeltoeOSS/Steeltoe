// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Actuators.ThreadDump;

namespace Steeltoe.Management.Endpoint.Test.Actuators.ThreadDump;

public sealed class ThreadDumpEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var options = GetOptionsFromSettings<ThreadDumpEndpointOptions>();
        Assert.Null(options.Enabled);
        Assert.Equal("threaddump", options.Id);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:loggers:enabled"] = "false",
            ["management:endpoints:dump:enabled"] = "true",
            ["management:endpoints:cloudfoundry:validateCertificates"] = "true",
            ["management:endpoints:cloudfoundry:enabled"] = "true"
        };

        IOptionsMonitor<ThreadDumpEndpointOptions> optionsMonitor =
            GetOptionsMonitorFromSettings<ThreadDumpEndpointOptions, ConfigureThreadDumpEndpointOptions>(appSettings);

        CloudFoundryEndpointOptions cloudFoundryEndpointOptions =
            GetOptionsFromSettings<CloudFoundryEndpointOptions, ConfigureCloudFoundryEndpointOptions>(appSettings);

        ThreadDumpEndpointOptions options = optionsMonitor.CurrentValue;
        Assert.True(cloudFoundryEndpointOptions.Enabled);
        Assert.Equal(string.Empty, cloudFoundryEndpointOptions.Id);
        Assert.Equal(string.Empty, cloudFoundryEndpointOptions.Path);
        Assert.True(cloudFoundryEndpointOptions.ValidateCertificates);

        Assert.True(options.Enabled);
        Assert.Equal("threaddump", options.Id);
        Assert.Equal("threaddump", options.Path);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectlyV1()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:loggers:enabled"] = "false",
            ["management:endpoints:dump:enabled"] = "true",
            ["management:endpoints:cloudfoundry:validateCertificates"] = "true",
            ["management:endpoints:cloudfoundry:enabled"] = "true"
        };

        IOptionsMonitor<ThreadDumpEndpointOptions> optionsMonitor =
            GetOptionsMonitorFromSettings<ThreadDumpEndpointOptions, ConfigureThreadDumpEndpointOptionsV1>(appSettings);

        CloudFoundryEndpointOptions cloudFoundryEndpointOptions =
            GetOptionsFromSettings<CloudFoundryEndpointOptions, ConfigureCloudFoundryEndpointOptions>(appSettings);

        ThreadDumpEndpointOptions options = optionsMonitor.CurrentValue;
        Assert.True(cloudFoundryEndpointOptions.Enabled);
        Assert.Equal(string.Empty, cloudFoundryEndpointOptions.Id);
        Assert.Equal(string.Empty, cloudFoundryEndpointOptions.Path);
        Assert.True(cloudFoundryEndpointOptions.ValidateCertificates);

        Assert.True(options.Enabled);
        Assert.Equal("dump", options.Id);
        Assert.Equal("dump", options.Path);
    }
}
