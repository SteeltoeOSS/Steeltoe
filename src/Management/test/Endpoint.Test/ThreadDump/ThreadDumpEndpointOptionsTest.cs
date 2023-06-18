// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.ThreadDump;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.ThreadDump;

public class ThreadDumpEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var opts = GetOptionsFromSettings<ThreadDumpEndpointOptions>();
        Assert.Null(opts.Enabled);
        Assert.Equal("threaddump", opts.Id);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:loggers:enabled"] = "false",
            ["management:endpoints:dump:enabled"] = "true",
            ["management:endpoints:cloudfoundry:validatecertificates"] = "true",
            ["management:endpoints:cloudfoundry:enabled"] = "true"
        };

        IOptionsMonitor<ThreadDumpEndpointOptions> optionsMonitor =
            GetOptionsMonitorFromSettings<ThreadDumpEndpointOptions, ConfigureThreadDumpEndpointOptions>(appsettings);

        CloudFoundryEndpointOptions cloudOpts = GetOptionsFromSettings<CloudFoundryEndpointOptions, ConfigureCloudFoundryEndpointOptions>(appsettings);
        ThreadDumpEndpointOptions opts = optionsMonitor.CurrentValue;
        Assert.True(cloudOpts.Enabled);
        Assert.Equal(string.Empty, cloudOpts.Id);
        Assert.Equal(string.Empty, cloudOpts.Path);
        Assert.True(cloudOpts.ValidateCertificates);

        Assert.True(opts.Enabled);
        Assert.Equal("threaddump", opts.Id);
        Assert.Equal("threaddump", opts.Path);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectlyV1()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:loggers:enabled"] = "false",
            ["management:endpoints:dump:enabled"] = "true",
            ["management:endpoints:cloudfoundry:validatecertificates"] = "true",
            ["management:endpoints:cloudfoundry:enabled"] = "true"
        };

        IOptionsMonitor<ThreadDumpEndpointOptions> optionsMonitor =
            GetOptionsMonitorFromSettings<ThreadDumpEndpointOptions, ConfigureThreadDumpEndpointOptionsV1>(appsettings);

        CloudFoundryEndpointOptions cloudOpts = GetOptionsFromSettings<CloudFoundryEndpointOptions, ConfigureCloudFoundryEndpointOptions>(appsettings);
        ThreadDumpEndpointOptions opts = optionsMonitor.CurrentValue;
        Assert.True(cloudOpts.Enabled);
        Assert.Equal(string.Empty, cloudOpts.Id);
        Assert.Equal(string.Empty, cloudOpts.Path);
        Assert.True(cloudOpts.ValidateCertificates);

        Assert.True(opts.Enabled);
        Assert.Equal("dump", opts.Id);
        Assert.Equal("dump", opts.Path);
    }
}
