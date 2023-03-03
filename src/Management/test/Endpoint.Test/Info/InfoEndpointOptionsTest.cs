// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Info;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Info;

public class InfoEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var opts = GetOptionsFromSettings<InfoEndpointOptions>();
        Assert.Null(opts.Enabled);
        Assert.Equal("info", opts.Id);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/management",
            ["management:endpoints:info:enabled"] = "false",
            ["management:endpoints:info:id"] = "infomanagement"
        };

        var opts = GetOptionsFromSettings<InfoEndpointOptions, ConfigureInfoEndpointOptions>(appsettings);
        Assert.False(opts.Enabled);
        Assert.Equal("infomanagement", opts.Id);
    }
}
