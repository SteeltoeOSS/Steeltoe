// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Logging.DynamicLogger;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.ContentNegotiation;

public sealed class ContentNegotiationTests
{
    private static readonly Dictionary<string, string> AppSettings = new()
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
                    epName = EndpointName.Hypermedia,
                    epPath = "http://localhost/actuator"
                },
                new
                {
                    epName = EndpointName.Cloudfoundry,
                    epPath = "http://localhost/cloudfoundryapplication"
                },
                new
                {
                    epName = EndpointName.Info,
                    epPath = "http://localhost/actuator/info"
                },
                new
                {
                    epName = EndpointName.Metrics,
                    epPath = "http://localhost/actuator/metrics"
                },
                new
                {
                    epName = EndpointName.Loggers,
                    epPath = "http://localhost/actuator/loggers"
                },
                new
                {
                    epName = EndpointName.Health,
                    epPath = "http://localhost/actuator/health"
                },
                new
                {
                    epName = EndpointName.Trace,
                    epPath = "http://localhost/actuator/httptrace"
                },
                new
                {
                    epName = EndpointName.Environment,
                    epPath = "http://localhost/actuator/env"
                },
                new
                {
                    epName = EndpointName.Mappings,
                    epPath = "http://localhost/actuator/mappings"
                },
                new
                {
                    epName = EndpointName.Refresh,
                    epPath = "http://localhost/actuator/refresh"
                }
            };

            var negotiations = new[]
            {
                new
                {
                    version = MediaTypeVersion.V2,
                    accepts = new[]
                    {
                        ActuatorMediaTypes.AppJson
                    },
                    contentType = ActuatorMediaTypes.AppJson,
                    name = "AcceptAppJson_ReturnsAppJson"
                },
                new
                {
                    version = MediaTypeVersion.V2,
                    accepts = new[]
                    {
                        "foo"
                    },
                    contentType = ActuatorMediaTypes.AppJson,
                    name = "AcceptInvalid_ReturnsAppJson"
                },
                new
                {
                    version = MediaTypeVersion.V2,
                    accepts = new[]
                    {
                        ActuatorMediaTypes.V1Json
                    },
                    contentType = ActuatorMediaTypes.AppJson,
                    name = "AcceptV1_ReturnsAppJson_WhenV2Configured"
                },
                new
                {
                    version = MediaTypeVersion.V2,
                    accepts = new[]
                    {
                        ActuatorMediaTypes.V2Json
                    },
                    contentType = ActuatorMediaTypes.V2Json,
                    name = "AcceptV2_ReturnsV2_WhenV2Configured"
                },
                new
                {
                    version = MediaTypeVersion.V2,
                    accepts = new[]
                    {
                        ActuatorMediaTypes.Any
                    },
                    contentType = ActuatorMediaTypes.V2Json,
                    name = "AcceptANY_ReturnsV2_WhenV2Configured"
                },
                new
                {
                    version = MediaTypeVersion.V2,
                    accepts = new[]
                    {
                        ActuatorMediaTypes.AppJson,
                        ActuatorMediaTypes.V1Json,
                        ActuatorMediaTypes.V2Json
                    },
                    contentType = ActuatorMediaTypes.V2Json,
                    name = "AcceptAllPossibleAscOrdered_ReturnsV2_WhenV2Configured"
                },
                new
                {
                    version = MediaTypeVersion.V2,
                    accepts = new[]
                    {
                        ActuatorMediaTypes.V2Json,
                        ActuatorMediaTypes.V1Json,
                        ActuatorMediaTypes.AppJson
                    },
                    contentType = ActuatorMediaTypes.V2Json,
                    name = "AcceptAllPossibleDescOrdered_ReturnsV2_WhenV2Configured"
                }
            };

            foreach (var endpoint in endpoints)
            {
                foreach (var negotiation in negotiations)
                {
                    yield return new object[]
                    {
                        endpoint.epName,
                        endpoint.epPath,
                        negotiation.accepts,
                        negotiation.contentType
                    };
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(EndpointMiddlewareContentNegotiationTestCases))]
    public async Task EndpointMiddleware_ContentNegotiation(EndpointName epName, string epPath, string[] accepts, string contentType)
    {
        try
        {
            if (epName == EndpointName.Cloudfoundry)
            {
                System.Environment.SetEnvironmentVariable("VCAP_APPLICATION", "some"); // Allow routing to /cloudfoundryapplication
            }

            // arrange a server and client
            IWebHostBuilder builder = new WebHostBuilder().UseStartupForEndpoint(epName)
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
            HttpResponseMessage result;

            if (epName == EndpointName.Refresh)
            {
                result = await client.PostAsync(new Uri(epPath), null);
            }
            else
            {
                result = await client.GetAsync(new Uri(epPath));
            }

            IEnumerable<string> contentHeaders = result.Content.Headers.GetValues("Content-Type");
            Assert.Contains(contentHeaders, header => header.StartsWith(contentType, StringComparison.Ordinal));
        }
        finally
        {
            System.Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
        }
    }
}
