// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.Info;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Info;

public sealed class InfoEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var options = GetOptionsFromSettings<InfoEndpointOptions>();
        Assert.Null(options.Enabled);
        Assert.Equal("info", options.Id);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/management",
            ["management:endpoints:info:enabled"] = "false",
            ["management:endpoints:info:id"] = "infomanagement"
        };

        InfoEndpointOptions options = GetOptionsFromSettings<InfoEndpointOptions, ConfigureInfoEndpointOptions>(appSettings);
        Assert.False(options.Enabled);
        Assert.Equal("infomanagement", options.Id);
    }
}
