// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Info;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.CloudFoundry;

public sealed class CloudFoundryEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var options = GetOptionsFromSettings<CloudFoundryEndpointOptions>();
        Assert.Null(options.Enabled);
        Assert.True(options.ValidateCertificates);
        Assert.Equal(string.Empty, options.Id);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appsettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:info:enabled"] = "true",
            ["management:endpoints:cloudfoundry:validatecertificates"] = "false",
            ["management:endpoints:cloudfoundry:enabled"] = "true"
        };

        var infoEndpointOptions = GetOptionsFromSettings<InfoEndpointOptions>(appsettings);
        var cloudFoundryEndpointOptions = GetOptionsFromSettings<CloudFoundryEndpointOptions>(appsettings);

        Assert.True(cloudFoundryEndpointOptions.Enabled);
        Assert.Equal(string.Empty, cloudFoundryEndpointOptions.Id);
        Assert.Equal(string.Empty, cloudFoundryEndpointOptions.Path);
        Assert.False(cloudFoundryEndpointOptions.ValidateCertificates);

        Assert.True(infoEndpointOptions.Enabled);
        Assert.Equal("info", infoEndpointOptions.Id);
        Assert.Equal("info", infoEndpointOptions.Path);
    }
}
