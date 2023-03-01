// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute.Extensions;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Health;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Health;

public class HealthEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        HealthEndpointOptions opts = GetOptionsFromSettings<HealthEndpointOptions, ConfigureHealthEndpointOptions>();
        
        Assert.Null(opts.Enabled);
        Assert.Equal("health", opts.Id);
        Assert.Equal(ShowDetails.Always, opts.ShowDetails);
        Assert.Equal(Permissions.Restricted, opts.RequiredPermissions);
    }


    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appsettings = new Dictionary<string, string>
        {

            ["management:endpoints:health:enabled"] = "true",
            ["management:endpoints:health:requiredPermissions"] = "NONE",
            ["management:endpoints:health:groups:custom:include"] = "diskSpace",
            ["management:endpoints:health:groups:lIveness:include"] = "diskSpace",
            ["management:endpoints:health:groups:rEadinEss:include"] = "diskSpace"
        };

        HealthEndpointOptions opts = GetOptionsFromSettings<HealthEndpointOptions, ConfigureHealthEndpointOptions>( appsettings);

        Assert.True(opts.Enabled);
        Assert.Equal("health", opts.Id);
        Assert.Equal("health", opts.Path);
        Assert.Equal(Permissions.None, opts.RequiredPermissions);
        Assert.Equal(3, opts.Groups.Count);
        Assert.True(opts.Groups.ContainsKey("custom"));
        Assert.True(opts.Groups.ContainsKey("liveness"));
        Assert.True(opts.Groups.ContainsKey("READINESS"));
    }

   

    [Fact]
    public void Constructor_BindsClaimCorrectly()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["management:endpoints:health:claim:type"] = "claimtype",
            ["management:endpoints:health:claim:value"] = "claimvalue",
            ["management:endpoints:health:role"] = "roleclaimvalue"
        };

        HealthEndpointOptions opts = GetOptionsFromSettings<HealthEndpointOptions, ConfigureHealthEndpointOptions> (appsettings);
        Assert.NotNull(opts.Claim);
        Assert.Equal("claimtype", opts.Claim.Type);
        Assert.Equal("claimvalue", opts.Claim.Value);
    }

    [Fact]
    public void Constructor_BindsRoleCorrectly()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["management:endpoints:health:role"] = "roleclaimvalue"
        };

        HealthEndpointOptions opts = GetOptionsFromSettings<HealthEndpointOptions, ConfigureHealthEndpointOptions>(appsettings);
        Assert.NotNull(opts.Claim);
        Assert.Equal(ClaimTypes.Role, opts.Claim.Type);
        Assert.Equal("roleclaimvalue", opts.Claim.Value);
    }
}
