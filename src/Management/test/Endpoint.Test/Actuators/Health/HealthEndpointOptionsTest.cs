// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Claims;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.Health;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health;

public sealed class HealthEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        HealthEndpointOptions options = GetOptionsFromSettings<HealthEndpointOptions, ConfigureHealthEndpointOptions>();

        Assert.Null(options.Enabled);
        Assert.Equal("health", options.Id);
        Assert.Equal(ShowDetails.Always, options.ShowDetails);
        Assert.Equal(Permissions.Restricted, options.RequiredPermissions);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appsettings = new Dictionary<string, string?>
        {
            ["management:endpoints:health:enabled"] = "true",
            ["management:endpoints:health:requiredPermissions"] = "NONE",
            ["management:endpoints:health:groups:custom:include"] = "diskSpace",
            ["management:endpoints:health:groups:lIveness:include"] = "diskSpace",
            ["management:endpoints:health:groups:rEadinEss:include"] = "diskSpace"
        };

        HealthEndpointOptions options = GetOptionsFromSettings<HealthEndpointOptions, ConfigureHealthEndpointOptions>(appsettings);

        Assert.True(options.Enabled);
        Assert.Equal("health", options.Id);
        Assert.Equal("health", options.Path);
        Assert.Equal(Permissions.None, options.RequiredPermissions);
        Assert.Equal(3, options.Groups.Count);
        Assert.True(options.Groups.ContainsKey("custom"));
        Assert.True(options.Groups.ContainsKey("liveness"));
        Assert.True(options.Groups.ContainsKey("READINESS"));
    }

    [Fact]
    public void Constructor_BindsClaimCorrectly()
    {
        var appsettings = new Dictionary<string, string?>
        {
            ["management:endpoints:health:claim:type"] = "claimtype",
            ["management:endpoints:health:claim:value"] = "claimvalue",
            ["management:endpoints:health:role"] = "roleclaimvalue"
        };

        HealthEndpointOptions options = GetOptionsFromSettings<HealthEndpointOptions, ConfigureHealthEndpointOptions>(appsettings);
        Assert.NotNull(options.Claim);
        Assert.Equal("claimtype", options.Claim.Type);
        Assert.Equal("claimvalue", options.Claim.Value);
    }

    [Fact]
    public void Constructor_BindsRoleCorrectly()
    {
        var appsettings = new Dictionary<string, string?>
        {
            ["management:endpoints:health:role"] = "roleclaimvalue"
        };

        HealthEndpointOptions options = GetOptionsFromSettings<HealthEndpointOptions, ConfigureHealthEndpointOptions>(appsettings);
        Assert.NotNull(options.Claim);
        Assert.Equal(ClaimTypes.Role, options.Claim.Type);
        Assert.Equal("roleclaimvalue", options.Claim.Value);
    }
}
