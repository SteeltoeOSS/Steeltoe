// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.Environment;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Environment;

public sealed class EnvironmentEndpointOptionsTest : BaseTest
{
    private static readonly string[] DefaultKeysToSanitize =
    [
        "password",
        "secret",
        "key",
        "token",
        ".*credentials.*",
        "vcap_services"
    ];

    [Fact]
    public void AppliesDefaults()
    {
        EnvironmentEndpointOptions options = GetOptionsFromSettings<EnvironmentEndpointOptions, ConfigureEnvironmentEndpointOptions>();

        options.Id.Should().Be("env");
        options.KeysToSanitize.Should().BeEquivalentTo(DefaultKeysToSanitize);
        options.RequiredPermissions.Should().Be(EndpointPermissions.Restricted);
    }

    [Fact]
    public void CanClearDefaults()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:env:keysToSanitize:0"] = ""
        };

        EnvironmentEndpointOptions options = GetOptionsFromSettings<EnvironmentEndpointOptions, ConfigureEnvironmentEndpointOptions>(appSettings);

        options.Id.Should().Be("env");
        options.KeysToSanitize.Should().BeEquivalentTo([]);
        options.RequiredPermissions.Should().Be(EndpointPermissions.Restricted);
    }

    [Fact]
    public void CanOverrideDefaults()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:env:keysToSanitize:0"] = "accessToken"
        };

        EnvironmentEndpointOptions options = GetOptionsFromSettings<EnvironmentEndpointOptions, ConfigureEnvironmentEndpointOptions>(appSettings);

        options.Id.Should().Be("env");
        options.KeysToSanitize.Should().BeEquivalentTo(["accessToken"]);
        options.RequiredPermissions.Should().Be(EndpointPermissions.Restricted);
    }
}
