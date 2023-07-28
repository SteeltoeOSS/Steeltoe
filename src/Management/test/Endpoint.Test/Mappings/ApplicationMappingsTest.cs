// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.RouteMappings;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Mappings;

public sealed class ApplicationMappingsTest : BaseTest
{
    [Fact]
    public void Constructor_SetsValues()
    {
        var mappingDictionary = new Dictionary<string, IList<RouteMappingDescription>>
        {
            { "dispatcherServlet", new List<RouteMappingDescription>() }
        };

        var contextMappings = new ContextMappings(mappingDictionary);

        var appMappings = new RouteMappingsResponse(contextMappings);
        IDictionary<string, ContextMappings> appContextMappings = appMappings.ContextMappings;
        Assert.Contains("application", appContextMappings.Keys);
        Assert.Single(appContextMappings.Keys);
        Assert.Same(contextMappings, appContextMappings["application"]);
    }

    [Fact]
    public void JsonSerialization_ReturnsExpected()
    {
        var routeDetails = new AspNetCoreRouteDetails
        {
            HttpMethods = new List<string>
            {
                "GET"
            },
            RouteTemplate = "/Home/Index",
            Consumes = new List<string>
            {
                "application/json"
            },
            Produces = new List<string>
            {
                "application/json"
            }
        };

        var mappingDescriptions = new List<RouteMappingDescription>
        {
            new("foobar", routeDetails)
        };

        var mappingDictionary = new Dictionary<string, IList<RouteMappingDescription>>
        {
            { "controllerTypeName", mappingDescriptions }
        };

        var contextMappings = new ContextMappings(mappingDictionary);
        var appMappings = new RouteMappingsResponse(contextMappings);

        string result = Serialize(appMappings);

        Assert.Equal(
            "{\"contexts\":{\"application\":{\"mappings\":{\"dispatcherServlets\":{\"controllerTypeName\":[{\"handler\":\"foobar\",\"predicate\":\"{[/Home/Index],methods=[GET],produces=[application/json],consumes=[application/json]}\"}]}}}}}",
            result);
    }
}
