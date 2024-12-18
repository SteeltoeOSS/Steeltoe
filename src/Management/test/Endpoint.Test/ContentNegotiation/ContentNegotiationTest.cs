// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Management.Endpoint.Test.ContentNegotiation;

public sealed class ContentNegotiationTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["management:endpoints:actuator:exposure:include:0"] = "*"
    };

    public static TheoryData<EndpointName, string, string[], string> EndpointMiddlewareContentNegotiationTestCases
    {
        get
        {
            List<(EndpointName Name, string Path)> endpoints =
            [
                (EndpointName.Hypermedia, "http://localhost/actuator"),
                (EndpointName.Cloudfoundry, "http://localhost/cloudfoundryapplication"),
                (EndpointName.Info, "http://localhost/actuator/info"),
                (EndpointName.Metrics, "http://localhost/actuator/metrics"),
                (EndpointName.Loggers, "http://localhost/actuator/loggers"),
                (EndpointName.Health, "http://localhost/actuator/health"),
                (EndpointName.HttpExchanges, "http://localhost/actuator/httpexchanges"),
                (EndpointName.Environment, "http://localhost/actuator/env"),
                (EndpointName.Mappings, "http://localhost/actuator/mappings"),
                (EndpointName.Refresh, "http://localhost/actuator/refresh")
            ];

            List<(MediaTypeVersion Version, string[] Accepts, string ContentType, string Name)> negotiations =
            [
                (MediaTypeVersion.V2, [ActuatorMediaTypes.AppJson], ActuatorMediaTypes.AppJson, "AcceptAppJson_ReturnsAppJson"),
                (MediaTypeVersion.V2, ["foo"], ActuatorMediaTypes.AppJson, "AcceptInvalid_ReturnsAppJson"),
                (MediaTypeVersion.V2, [ActuatorMediaTypes.V1Json], ActuatorMediaTypes.AppJson, "AcceptV1_ReturnsAppJson_WhenV2Configured"),
                (MediaTypeVersion.V2, [ActuatorMediaTypes.V2Json], ActuatorMediaTypes.V2Json, "AcceptV2_ReturnsV2_WhenV2Configured"),
                (MediaTypeVersion.V2, [ActuatorMediaTypes.Any], ActuatorMediaTypes.V2Json, "AcceptANY_ReturnsV2_WhenV2Configured"),
                (MediaTypeVersion.V2, [
                    ActuatorMediaTypes.AppJson,
                    ActuatorMediaTypes.V1Json,
                    ActuatorMediaTypes.V2Json
                ], ActuatorMediaTypes.V2Json, "AcceptAllPossibleAscOrdered_ReturnsV2_WhenV2Configured"),
                (MediaTypeVersion.V2, [
                    ActuatorMediaTypes.V2Json,
                    ActuatorMediaTypes.V1Json,
                    ActuatorMediaTypes.AppJson
                ], ActuatorMediaTypes.V2Json, "AcceptAllPossibleDescOrdered_ReturnsV2_WhenV2Configured")
            ];

            TheoryData<EndpointName, string, string[], string> theoryData = [];

            foreach ((EndpointName name, string path) in endpoints)
            {
                foreach ((_, string[] accepts, string contentType, _) in negotiations)
                {
                    theoryData.Add(name, path, accepts, contentType);
                }
            }

            return theoryData;
        }
    }

    [Theory]
    [MemberData(nameof(EndpointMiddlewareContentNegotiationTestCases))]
    public async Task EndpointMiddleware_ContentNegotiation(EndpointName endpointName, string endpointPath, string[] accepts, string contentType)
    {
        string name = endpointName == EndpointName.Cloudfoundry ? "VCAP_APPLICATION" : "unused";
        using var scope = new EnvironmentVariableScope(name, "some"); // Allow routing to /cloudfoundryapplication

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartupForEndpoint(endpointName);
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings));

        using IWebHost host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();

        foreach (string accept in accepts)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", accept);
        }

        HttpResponseMessage response = endpointName == EndpointName.Refresh
            ? await client.PostAsync(new Uri(endpointPath), null)
            : await client.GetAsync(new Uri(endpointPath));

        IEnumerable<string> contentHeaders = response.Content.Headers.GetValues("Content-Type");
        Assert.Contains(contentHeaders, header => header.StartsWith(contentType, StringComparison.Ordinal));
    }
}
