// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings;

namespace Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings;

public sealed class ContextMappingsTest : BaseTest
{
    [Fact]
    public void Constructor_SetsValues()
    {
        List<RouteMappingDescription> mappingList = [];

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
        List<string> httpMethods = ["GET"];
        List<string> contentTypes = ["application/json"];
        var routeDetails = new AspNetCoreRouteDetails("/Home/Index", httpMethods, contentTypes, contentTypes, Array.Empty<string>(), Array.Empty<string>());

        List<RouteMappingDescription> mappingDescriptions = [new("foobar", routeDetails)];

        var mappingDictionary = new Dictionary<string, IList<RouteMappingDescription>>
        {
            { "controllerTypeName", mappingDescriptions }
        };

        var contextMappings = new ContextMappings(mappingDictionary, null);
        string result = Serialize(contextMappings);

        result.Should().BeJson("""
            {
              "mappings": {
                "dispatcherServlets": {
                  "controllerTypeName": [
                    {
                      "handler": "foobar",
                      "predicate": "{[/Home/Index],methods=[GET],produces=[application/json],consumes=[application/json]}"
                    }
                  ]
                }
              }
            }
            """);
    }
}
