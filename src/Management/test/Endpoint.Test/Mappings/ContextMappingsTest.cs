// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.RouteMappings;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Mappings;

public class ContextMappingsTest : BaseTest
{
    [Fact]
    public void Constructor_SetsValues()
    {
        var mappingList = new List<RouteMappingDescription>();

        var mappingDict = new Dictionary<string, IList<RouteMappingDescription>>
        {
            { "dispatcherServlet", mappingList }
        };

        var contextMappings = new ContextMappings(mappingDict);
        IDictionary<string, IDictionary<string, IList<RouteMappingDescription>>> contextMappingsMappings = contextMappings.Mappings;
        Assert.Contains("dispatcherServlets", contextMappingsMappings.Keys);
        IDictionary<string, IList<RouteMappingDescription>> mappings = contextMappingsMappings["dispatcherServlets"];
        Assert.Contains("dispatcherServlet", mappings.Keys);
        Assert.Same(mappingList, mappings["dispatcherServlet"]);
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
        string result = Serialize(contextMappings);

        Assert.Equal(
            "{\"mappings\":{\"dispatcherServlets\":{\"controllerTypeName\":[{\"handler\":\"foobar\",\"predicate\":\"{[/Home/Index],methods=[GET],produces=[application/json],consumes=[application/json]}\"}]}}}",
            result);
    }
}
