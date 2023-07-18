// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.RouteMappings;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Mappings;

public class ApplicationMappingsTest : BaseTest
{
    [Fact]
    public void Constructor_SetsValues()
    {
        var mappingDict = new Dictionary<string, IList<RouteMappingDescription>>
        {
            { "dispatcherServlet", new List<RouteMappingDescription>() }
        };

        var contextMappings = new ContextMappings(mappingDict);

        var appMappings = new RouteMappingsResponse(contextMappings);
        IDictionary<string, ContextMappings> ctxMappings = appMappings.ContextMappings;
        Assert.Contains("application", ctxMappings.Keys);
        Assert.Single(ctxMappings.Keys);
        Assert.Same(contextMappings, ctxMappings["application"]);
    }

    [Fact]
    public void JsonSerialization_ReturnsExpected()
    {
        var routeDetail = new AspNetCoreRouteDetails
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
            new("foobar", routeDetail)
        };

        var mappingDict = new Dictionary<string, IList<RouteMappingDescription>>
        {
            { "controllerTypeName", mappingDescriptions }
        };

        var contextMappings = new ContextMappings(mappingDict);
        var appMappings = new RouteMappingsResponse(contextMappings);

        string result = Serialize(appMappings);

        Assert.Equal(
            "{\"contexts\":{\"application\":{\"mappings\":{\"dispatcherServlets\":{\"controllerTypeName\":[{\"handler\":\"foobar\",\"predicate\":\"{[/Home/Index],methods=[GET],produces=[application/json],consumes=[application/json]}\"}]}}}}}",
            result);
    }
}
