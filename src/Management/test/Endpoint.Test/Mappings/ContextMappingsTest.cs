// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.RouteMappings;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Mappings;

public sealed class ContextMappingsTest : BaseTest
{
    [Fact]
    public void Constructor_SetsValues()
    {
        var mappingList = new List<RouteMappingDescription>();

        var mappingDictionary = new Dictionary<string, IList<RouteMappingDescription>>
        {
            { "dispatcherServlet", mappingList }
        };

        var contextMappings = new ContextMappings(mappingDictionary, null);
        IDictionary<string, IDictionary<string, IList<RouteMappingDescription>>> contextMappingsMappings = contextMappings.Mappings;
        Assert.Contains("dispatcherServlets", contextMappingsMappings.Keys);
        IDictionary<string, IList<RouteMappingDescription>> mappings = contextMappingsMappings["dispatcherServlets"];
        Assert.Contains("dispatcherServlet", mappings.Keys);
        Assert.Same(mappingList, mappings["dispatcherServlet"]);
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

        var routeDetails = new AspNetCoreRouteDetails(httpMethods, "/Home/Index", contentTypes, contentTypes);

        var mappingDescriptions = new List<RouteMappingDescription>
        {
            new("foobar", routeDetails)
        };

        var mappingDictionary = new Dictionary<string, IList<RouteMappingDescription>>
        {
            { "controllerTypeName", mappingDescriptions }
        };

        var contextMappings = new ContextMappings(mappingDictionary, null);
        string result = Serialize(contextMappings);

        Assert.Equal(
            "{\"mappings\":{\"dispatcherServlets\":{\"controllerTypeName\":[{\"handler\":\"foobar\",\"predicate\":\"{[/Home/Index],methods=[GET],produces=[application/json],consumes=[application/json]}\"}]}}}",
            result);
    }
}
