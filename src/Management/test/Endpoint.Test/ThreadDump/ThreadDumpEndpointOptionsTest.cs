// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
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

      

        var optionsMonitor = GetOptionsMonitorFromSettings<ThreadDumpEndpointOptions, ConfigureThreadDumpEndpointOptions>(appsettings);
        var cloudOpts = GetOptionsFromSettings<CloudFoundryEndpointOptions, ConfigureCloudFoundryEndpointOptions>(appsettings);
        var ep = new ThreadDumpEndpoint(optionsMonitor, new ThreadDumperEp(optionsMonitor));
        var opts = optionsMonitor.CurrentValue;
        Assert.True(cloudOpts.Enabled);
        Assert.Equal(string.Empty, cloudOpts.Id);
        Assert.Equal(string.Empty, cloudOpts.Path);
        Assert.True(cloudOpts.ValidateCertificates);

        Assert.True(opts.Enabled);
        Assert.Equal("threaddump", opts.Id);
        Assert.Equal("threaddump", opts.Path); //Todo: ASsert for "dump" as well

      //  Assert.True(ep.Enabled);
    }
}
