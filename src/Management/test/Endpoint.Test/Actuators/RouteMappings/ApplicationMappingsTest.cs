// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings;

namespace Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings;

public sealed class ApplicationMappingsTest : BaseTest
{
    [Fact]
    public void Constructor_SetsValues()
    {
        var mappingDictionary = new Dictionary<string, IList<RouteMappingDescription>>
        {
            { "dispatcherServlet", new List<RouteMappingDescription>() }
        };

        var contextMappings = new ContextMappings(mappingDictionary, null);

        var appMappings = new RouteMappingsResponse(contextMappings);
        IDictionary<string, ContextMappings> appContextMappings = appMappings.ContextMappings;
        Assert.Contains("application", appContextMappings.Keys);
        Assert.Single(appContextMappings.Keys);
        Assert.Same(contextMappings, appContextMappings["application"]);
    }

    [Fact]
    public void JsonSerialization_ReturnsExpected()
    {
        List<string> httpMethods = ["GET"];
        List<string> contentTypes = ["application/json"];
        var routeDetails = new AspNetCoreRouteDetails("/Home/Index", httpMethods, contentTypes, contentTypes, Array.Empty<string>(), Array.Empty<string>());

        List<RouteMappingDescription> mappingDescriptions = [new RouteMappingDescription("foobar", routeDetails)];

        var mappingDictionary = new Dictionary<string, IList<RouteMappingDescription>>
        {
            { "controllerTypeName", mappingDescriptions }
        };

        var contextMappings = new ContextMappings(mappingDictionary, null);
        var appMappings = new RouteMappingsResponse(contextMappings);

        string result = Serialize(appMappings);

        result.Should().BeJson("""
            {
              "contexts": {
                "application": {
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
              }
            }
            """);
    }
}
