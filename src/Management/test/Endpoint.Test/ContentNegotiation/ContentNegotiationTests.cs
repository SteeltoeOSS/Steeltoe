// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.TestResources;
using Steeltoe.Logging.DynamicLogger;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.ContentNegotiation;

public sealed class ContentNegotiationTests
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["management:endpoints:actuator:exposure:include:0"] = "*"
    };

    public static IEnumerable<object[]> EndpointMiddlewareContentNegotiationTestCases
    {
        get
        {
            var endpoints = new[]
            {
                new
                {
                    Name = EndpointName.Hypermedia,
                    Path = "http://localhost/actuator"
                },
                new
                {
                    Name = EndpointName.Cloudfoundry,
                    Path = "http://localhost/cloudfoundryapplication"
                },
                new
                {
                    Name = EndpointName.Info,
                    Path = "http://localhost/actuator/info"
                },
                new
                {
                    Name = EndpointName.Metrics,
                    Path = "http://localhost/actuator/metrics"
                },
                new
                {
                    Name = EndpointName.Loggers,
                    Path = "http://localhost/actuator/loggers"
                },
                new
                {
                    Name = EndpointName.Health,
                    Path = "http://localhost/actuator/health"
                },
                new
                {
                    Name = EndpointName.Trace,
                    Path = "http://localhost/actuator/httptrace"
                },
                new
                {
                    Name = EndpointName.Environment,
                    Path = "http://localhost/actuator/env"
                },
                new
                {
                    Name = EndpointName.Mappings,
                    Path = "http://localhost/actuator/mappings"
                },
                new
                {
                    Name = EndpointName.Refresh,
                    Path = "http://localhost/actuator/refresh"
                }
            };

            var negotiations = new[]
            {
                new
                {
                    version = MediaTypeVersion.V2,
                    Accepts = new[]
                    {
                        ActuatorMediaTypes.AppJson
                    },
                    ContentType = ActuatorMediaTypes.AppJson,
                    name = "AcceptAppJson_ReturnsAppJson"
                },
                new
                {
                    version = MediaTypeVersion.V2,
                    Accepts = new[]
                    {
                        "foo"
                    },
                    ContentType = ActuatorMediaTypes.AppJson,
                    name = "AcceptInvalid_ReturnsAppJson"
                },
                new
                {
                    version = MediaTypeVersion.V2,
                    Accepts = new[]
                    {
                        ActuatorMediaTypes.V1Json
                    },
                    ContentType = ActuatorMediaTypes.AppJson,
                    name = "AcceptV1_ReturnsAppJson_WhenV2Configured"
                },
                new
                {
                    version = MediaTypeVersion.V2,
                    Accepts = new[]
                    {
                        ActuatorMediaTypes.V2Json
                    },
                    ContentType = ActuatorMediaTypes.V2Json,
                    name = "AcceptV2_ReturnsV2_WhenV2Configured"
                },
                new
                {
                    version = MediaTypeVersion.V2,
                    Accepts = new[]
                    {
                        ActuatorMediaTypes.Any
                    },
                    ContentType = ActuatorMediaTypes.V2Json,
                    name = "AcceptANY_ReturnsV2_WhenV2Configured"
                },
                new
                {
                    version = MediaTypeVersion.V2,
                    Accepts = new[]
                    {
                        ActuatorMediaTypes.AppJson,
                        ActuatorMediaTypes.V1Json,
                        ActuatorMediaTypes.V2Json
                    },
                    ContentType = ActuatorMediaTypes.V2Json,
                    name = "AcceptAllPossibleAscOrdered_ReturnsV2_WhenV2Configured"
                },
                new
                {
                    version = MediaTypeVersion.V2,
                    Accepts = new[]
                    {
                        ActuatorMediaTypes.V2Json,
                        ActuatorMediaTypes.V1Json,
                        ActuatorMediaTypes.AppJson
                    },
                    ContentType = ActuatorMediaTypes.V2Json,
                    name = "AcceptAllPossibleDescOrdered_ReturnsV2_WhenV2Configured"
                }
            };

            foreach (var endpoint in endpoints)
            {
                foreach (var negotiation in negotiations)
                {
                    yield return new object[]
                    {
                        endpoint.Name,
                        endpoint.Path,
                        negotiation.Accepts,
                        negotiation.ContentType
                    };
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(EndpointMiddlewareContentNegotiationTestCases))]
    public async Task EndpointMiddleware_ContentNegotiation(EndpointName endpointName, string endpointPath, string[] accepts, string contentType)
    {
        string name = endpointName == EndpointName.Cloudfoundry ? "VCAP_APPLICATION" : "unused";
        using var scope = new EnvironmentVariableScope(name, "some"); // Allow routing to /cloudfoundryapplication

        // arrange a server and client
        IWebHostBuilder builder = new WebHostBuilder().UseStartupForEndpoint(endpointName)
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
