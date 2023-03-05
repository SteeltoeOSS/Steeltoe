// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Info;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.CloudFoundry;

public class CloudFoundryEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var opts = GetOptionsFromSettings<CloudFoundryEndpointOptions>();
        Assert.Null(opts.Enabled);
        Assert.True(opts.ValidateCertificates);
        Assert.Equal(string.Empty, opts.Id);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:info:enabled"] = "true",
            ["management:endpoints:cloudfoundry:validatecertificates"] = "false",
            ["management:endpoints:cloudfoundry:enabled"] = "true"
        };


        var opts = GetOptionsFromSettings<InfoEndpointOptions>(appsettings);
        var cloudOpts = GetOptionsFromSettings<CloudFoundryEndpointOptions>(appsettings);

        Assert.True(cloudOpts.Enabled);
        Assert.Equal(string.Empty, cloudOpts.Id);
        Assert.Equal(string.Empty, cloudOpts.Path);
        Assert.False(cloudOpts.ValidateCertificates);

        Assert.True(opts.Enabled);
        Assert.Equal("info", opts.Id);
        Assert.Equal("info", opts.Path);
    }
}
