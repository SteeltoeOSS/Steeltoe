// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.HeapDump;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.HeapDump;

public class HeapDumpEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var opts = GetOptionsFromSettings<HeapDumpEndpointOptions>();
        Assert.Null(opts.Enabled);
        Assert.Equal("heapdump", opts.Id);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["management:endpoints:enabled"] = "false",

            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:loggers:enabled"] = "false",
            ["management:endpoints:heapdump:enabled"] = "true",
            ["management:endpoints:cloudfoundry:validatecertificates"] = "true",
            ["management:endpoints:cloudfoundry:enabled"] = "true"
        };

        var opts = GetOptionsFromSettings<HeapDumpEndpointOptions>(appsettings);
        var cloudOpts = GetOptionsFromSettings<CloudFoundryEndpointOptions>(appsettings);

        Assert.True(cloudOpts.Enabled);
        Assert.Equal(string.Empty, cloudOpts.Id);
        Assert.Equal(string.Empty, cloudOpts.Path);
        Assert.True(cloudOpts.ValidateCertificates);

        Assert.True(opts.Enabled);
        Assert.Equal("heapdump", opts.Id);
        Assert.Equal("heapdump", opts.Path);
    }
}
