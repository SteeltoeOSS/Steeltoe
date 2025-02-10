// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings;

public sealed class RouteMappingsEndpointOptionsTest : BaseTest
{
    [Fact]
    public void ConfiguresOptionsWithDefaults()
    {
        var endpointOptions = GetOptionsFromSettings<RouteMappingsEndpointOptions>();
        var managementOptions = GetOptionsFromSettings<ManagementOptions>();

        endpointOptions.Enabled.Should().BeNull();
        endpointOptions.Id.Should().Be("mappings");
        endpointOptions.Path.Should().Be("mappings");
        endpointOptions.AllowedVerbs.Should().HaveCount(1);
        endpointOptions.AllowedVerbs[0].Should().Be("Get");
        endpointOptions.RequiredPermissions.Should().Be(EndpointPermissions.Restricted);
        endpointOptions.IncludeActuators.Should().BeTrue();
        endpointOptions.RequiresExactMatch().Should().BeTrue();

        endpointOptions.GetPathMatchPattern(managementOptions.Path).Should().Be("/actuator/mappings");
        endpointOptions.GetPathMatchPattern(ConfigureManagementOptions.DefaultCloudFoundryPath).Should().Be("/cloudfoundryapplication/mappings");
    }
}
