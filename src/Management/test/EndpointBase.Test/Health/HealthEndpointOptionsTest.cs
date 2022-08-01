// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Test;
using System.Security.Claims;
using Xunit;

namespace Steeltoe.Management.Endpoint.Health.Test;

public class HealthEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var opts = new HealthEndpointOptions();
        Assert.Null(opts.Enabled);
        Assert.Equal("health", opts.Id);
        Assert.Equal(ShowDetails.Always, opts.ShowDetails);
        Assert.Equal(Permissions.Restricted, opts.RequiredPermissions);
    }

    [Fact]
    public void Constructor_ThrowsIfConfigNull()
    {
        const IConfiguration config = null;
        Assert.Throws<ArgumentNullException>(() => new HealthEndpointOptions(config));
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:health:enabled"] = "true",
            ["management:endpoints:health:requiredPermissions"] = "NONE",
            ["management:endpoints:cloudfoundry:validatecertificates"] = "true",
            ["management:endpoints:cloudfoundry:enabled"] = "true",
            ["management:endpoints:health:groups:custom:include"] = "diskSpace",
            ["management:endpoints:health:groups:lIveness:include"] = "diskSpace",
            ["management:endpoints:health:groups:rEadinEss:include"] = "diskSpace"
        };
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        var config = configurationBuilder.Build();

        var opts = new HealthEndpointOptions(config);
        var cloudOpts = new CloudFoundryEndpointOptions(config);

        Assert.True(cloudOpts.Enabled);
        Assert.Equal(string.Empty, cloudOpts.Id);
        Assert.Equal(string.Empty, cloudOpts.Path);
        Assert.True(cloudOpts.ValidateCertificates);

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
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        var config = configurationBuilder.Build();

        var opts = new HealthEndpointOptions(config);
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
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        var config = configurationBuilder.Build();

        var opts = new HealthEndpointOptions(config);
        Assert.NotNull(opts.Claim);
        Assert.Equal(ClaimTypes.Role, opts.Claim.Type);
        Assert.Equal("roleclaimvalue", opts.Claim.Value);
    }
}
