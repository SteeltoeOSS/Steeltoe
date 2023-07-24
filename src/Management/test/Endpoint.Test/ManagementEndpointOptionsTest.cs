// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Options;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test;

public sealed class ManagementEndpointOptionsTest : BaseTest
{
    [Fact]
    public void InitializedWithDefaults()
    {
        ManagementEndpointOptions opts = GetOptionsMonitorFromSettings<ManagementEndpointOptions>().CurrentValue;
        Assert.False(opts.Enabled.HasValue);
        Assert.Equal("/actuator", opts.Path);
        Assert.NotNull(opts.Exposure);
        Assert.Contains("health", opts.Exposure.Include);
        Assert.Contains("info", opts.Exposure.Include);
    }

    [Fact]
    public void BindsConfigurationCorrectly()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/management",
            ["management:endpoints:info:enabled"] = "true",
            ["management:endpoints:info:id"] = "/infomanagement"
        };

        ManagementEndpointOptions opts = GetOptionsMonitorFromSettings<ManagementEndpointOptions>(appsettings).CurrentValue;
        Assert.False(opts.Enabled);
        Assert.Equal("/management", opts.Path);
    }

    [Fact]
    public void IsExposedCorrectly()
    {
        var managementOptions = new ManagementEndpointOptions
        {
            Exposure =
            {
                Exclude = new[]
                {
                    "*"
                }.ToList()
            }
        };

        var options = GetOptionsFromSettings<InfoEndpointOptions>();
        Assert.False(options.IsExposed(managementOptions));
    }
}
