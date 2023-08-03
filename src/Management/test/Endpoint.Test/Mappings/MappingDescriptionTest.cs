// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.RouteMappings;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Mappings;

public sealed class MappingDescriptionTest : BaseTest
{
    [Fact]
    public void Constructor_SetsValues()
    {
        var httpMethods = new List<string>
        {
            "GET"
        };

        var contentTypes = new List<string>
        {
            "application/json"
        };

        var routeDetails = new AspNetCoreRouteDetails("/Home/Index", httpMethods, contentTypes, contentTypes, Array.Empty<string>(), Array.Empty<string>());

        var mapDesc = new RouteMappingDescription("foobar", routeDetails);

        Assert.Null(mapDesc.Details);
        Assert.Equal("foobar", mapDesc.Handler);
        Assert.Equal("{[/Home/Index],methods=[GET],produces=[application/json],consumes=[application/json]}", mapDesc.Predicate);
    }

    [Fact]
    public void JsonSerialization_ReturnsExpected()
    {
        var httpMethods = new List<string>
        {
            "GET"
        };

        var contentTypes = new List<string>
        {
            "application/json"
        };

        var routeDetails = new AspNetCoreRouteDetails("/Home/Index", httpMethods, contentTypes, contentTypes, Array.Empty<string>(), Array.Empty<string>());

        var mapDesc = new RouteMappingDescription("foobar", routeDetails);

        string result = Serialize(mapDesc);

        Assert.Equal("{\"handler\":\"foobar\",\"predicate\":\"{[/Home/Index],methods=[GET],produces=[application/json],consumes=[application/json]}\"}",
            result);
    }
}
