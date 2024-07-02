// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Web.Hypermedia;

namespace Steeltoe.Management.Endpoint.Test.Hypermedia;

public sealed class HypermediaEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var options = GetOptionsFromSettings<HypermediaEndpointOptions>();
        Assert.Equal(string.Empty, options.Id);
        Assert.Equal(string.Empty, options.Path);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appsettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",

            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:info:enabled"] = "true",
            ["management:endpoints:info:path"] = "infopath",

            ["management:endpoints:cloudfoundry:validatecertificates"] = "false",
            ["management:endpoints:cloudfoundry:enabled"] = "true"
        };

        InfoEndpointOptions options = GetOptionsFromSettings<InfoEndpointOptions, ConfigureInfoEndpointOptions>(appsettings);

        Assert.Equal("info", options.Id);
        Assert.Equal("infopath", options.Path);
    }
}
