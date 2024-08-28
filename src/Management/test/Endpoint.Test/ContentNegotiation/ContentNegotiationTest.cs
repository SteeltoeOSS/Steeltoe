// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.TestResources;
using Steeltoe.Logging.DynamicLogger;

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
                (EndpointName.Trace, "http://localhost/actuator/httptrace"),
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

        // arrange a server and client
        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartupForEndpoint(endpointName)
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings)).ConfigureLogging(
                (webHostContext, loggingBuilder) =>
                {
                    loggingBuilder.AddConfiguration(webHostContext.Configuration);
                    loggingBuilder.AddDynamicConsole();
                });

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();

        foreach (string accept in accepts)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", accept);
        }

        // send the request
        HttpResponseMessage response;

        if (endpointName == EndpointName.Refresh)
        {
            response = await client.PostAsync(new Uri(endpointPath), null);
        }
        else
        {
            response = await client.GetAsync(new Uri(endpointPath));
        }

        IEnumerable<string> contentHeaders = response.Content.Headers.GetValues("Content-Type");
        Assert.Contains(contentHeaders, header => header.StartsWith(contentType, StringComparison.Ordinal));
    }
}
