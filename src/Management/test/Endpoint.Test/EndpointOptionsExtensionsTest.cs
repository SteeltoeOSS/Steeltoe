// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.RouteMappings;

namespace Steeltoe.Management.Endpoint.Test;

public sealed class EndpointOptionsExtensionsTest
{
    [Theory]
    [InlineData(null, null, "/")]
    [InlineData(null, "", "/")]
    [InlineData(null, "/", "/")]
    [InlineData(null, "/ping", "/ping")]
    [InlineData(null, "/ping/", "/ping")]
    [InlineData(null, "ping", "/ping")]
    [InlineData(null, "ping/", "/ping")]
    [InlineData("", null, "/")]
    [InlineData("", "", "/")]
    [InlineData("", "/", "/")]
    [InlineData("", "/ping", "/ping")]
    [InlineData("", "/ping/", "/ping")]
    [InlineData("", "ping", "/ping")]
    [InlineData("", "ping/", "/ping")]
    [InlineData("/", null, "/")]
    [InlineData("/", "", "/")]
    [InlineData("/", "/", "/")]
    [InlineData("/", "/ping", "/ping")]
    [InlineData("/", "/ping/", "/ping")]
    [InlineData("/", "ping", "/ping")]
    [InlineData("/", "ping/", "/ping")]
    [InlineData("/actuator", null, "/actuator")]
    [InlineData("/actuator", "", "/actuator")]
    [InlineData("/actuator", "/", "/actuator")]
    [InlineData("/actuator", "/ping", "/actuator/ping")]
    [InlineData("/actuator", "/ping/", "/actuator/ping")]
    [InlineData("/actuator", "ping", "/actuator/ping")]
    [InlineData("/actuator", "ping/", "/actuator/ping")]
    [InlineData("/actuator/", null, "/actuator")]
    [InlineData("/actuator/", "", "/actuator")]
    [InlineData("/actuator/", "/", "/actuator")]
    [InlineData("/actuator/", "/ping", "/actuator/ping")]
    [InlineData("/actuator/", "/ping/", "/actuator/ping")]
    [InlineData("/actuator/", "ping", "/actuator/ping")]
    [InlineData("/actuator/", "ping/", "/actuator/ping")]
    [InlineData("actuator", null, "/actuator")]
    [InlineData("actuator", "", "/actuator")]
    [InlineData("actuator", "/", "/actuator")]
    [InlineData("actuator", "/ping", "/actuator/ping")]
    [InlineData("actuator", "/ping/", "/actuator/ping")]
    [InlineData("actuator", "ping", "/actuator/ping")]
    [InlineData("actuator", "ping/", "/actuator/ping")]
    [InlineData("actuator/", null, "/actuator")]
    [InlineData("actuator/", "", "/actuator")]
    [InlineData("actuator/", "/", "/actuator")]
    [InlineData("actuator/", "/ping", "/actuator/ping")]
    [InlineData("actuator/", "/ping/", "/actuator/ping")]
    [InlineData("actuator/", "ping", "/actuator/ping")]
    [InlineData("actuator/", "ping/", "/actuator/ping")]
    public void GetEndpointPath_combines_segments(string? managementPath, string? endpointPath, string expected)
    {
        var endpointOptions = new RouteMappingsEndpointOptions
        {
            Path = endpointPath
        };

        string result = endpointOptions.GetEndpointPath(managementPath);

        result.Should().Be(expected);
    }
}
