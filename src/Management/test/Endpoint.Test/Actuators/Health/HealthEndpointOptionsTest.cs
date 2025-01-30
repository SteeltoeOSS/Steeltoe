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
        Assert.Equal(ShowValues.Never, options.ShowComponents);
        Assert.Equal(ShowValues.Never, options.ShowDetails);
        Assert.Equal(EndpointPermissions.Restricted, options.RequiredPermissions);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:health:enabled"] = "true",
            ["management:endpoints:health:requiredPermissions"] = "FULL",
            ["management:endpoints:health:groups:custom:include"] = "diskSpace",
            ["management:endpoints:health:groups:lIveness:include"] = "diskSpace",
            ["management:endpoints:health:groups:rEadinEss:include"] = "diskSpace"
        };

        HealthEndpointOptions options = GetOptionsFromSettings<HealthEndpointOptions, ConfigureHealthEndpointOptions>(appSettings);

        Assert.True(options.Enabled);
        Assert.Equal("health", options.Id);
        Assert.Equal("health", options.Path);
        Assert.Equal(EndpointPermissions.Full, options.RequiredPermissions);
        Assert.Equal(3, options.Groups.Count);
        Assert.True(options.Groups.ContainsKey("custom"));
        Assert.True(options.Groups.ContainsKey("liveness"));
        Assert.True(options.Groups.ContainsKey("READINESS"));
    }

    [Fact]
    public void Constructor_BindsClaimCorrectly()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:health:claim:type"] = "claim-type",
            ["management:endpoints:health:claim:value"] = "claim-value",
            ["management:endpoints:health:role"] = "role-claim-value"
        };

        HealthEndpointOptions options = GetOptionsFromSettings<HealthEndpointOptions, ConfigureHealthEndpointOptions>(appSettings);
        Assert.NotNull(options.Claim);
        Assert.Equal("claim-type", options.Claim.Type);
        Assert.Equal("claim-value", options.Claim.Value);
    }

    [Fact]
    public void Constructor_BindsRoleCorrectly()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:health:role"] = "role-claim-value"
        };

        HealthEndpointOptions options = GetOptionsFromSettings<HealthEndpointOptions, ConfigureHealthEndpointOptions>(appSettings);
        Assert.NotNull(options.Claim);
        Assert.Equal(ClaimTypes.Role, options.Claim.Type);
        Assert.Equal("role-claim-value", options.Claim.Value);
    }
}
