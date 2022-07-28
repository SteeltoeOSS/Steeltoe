// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using static Steeltoe.Management.Endpoint.ContentNegotiation.Test.TestStartupExtensions;

namespace Steeltoe.Management.Endpoint.ContentNegotiation.Test;

public class ContentNegotiationTests
{
    private static readonly Dictionary<string, string> AppSettings = new ()
    {
        ["management:endpoints:actuator:exposure:include:0"] = "*"
    };

    public static IEnumerable<object[]> EndpointMiddlewareContentNegotiationTestCases
    {
        get
        {
            var endpoints = new[]
            {
                new { epName = EndpointNames.Hypermedia, epPath = "http://localhost/actuator" },
                new { epName = EndpointNames.Cloudfoundry, epPath = "http://localhost/cloudfoundryapplication" },
                new { epName = EndpointNames.Info, epPath = "http://localhost/actuator/info" },
                new { epName = EndpointNames.Metrics, epPath = "http://localhost/actuator/metrics" },
                new { epName = EndpointNames.Loggers, epPath = "http://localhost/actuator/loggers" },
                new { epName = EndpointNames.Health, epPath = "http://localhost/actuator/health" },
                new { epName = EndpointNames.Trace, epPath = "http://localhost/actuator/httptrace" },
                new { epName = EndpointNames.Env, epPath = "http://localhost/actuator/env" },
                new { epName = EndpointNames.Mappings, epPath = "http://localhost/actuator/mappings" },
                new { epName = EndpointNames.Refresh, epPath = "http://localhost/actuator/refresh" }
            };

            var negotiations = new[]
            {
                new { version = MediaTypeVersion.V2, accepts = new[] { ActuatorMediaTypes.AppJson }, contentType = ActuatorMediaTypes.AppJson, name = "AcceptAppJson_ReturnsAppJson" },
                new { version = MediaTypeVersion.V2, accepts = new[] { "foo" }, contentType = ActuatorMediaTypes.AppJson, name = "AcceptInvalid_ReturnsAppJson" },
                new { version = MediaTypeVersion.V2, accepts = new[] { ActuatorMediaTypes.V1Json }, contentType = ActuatorMediaTypes.AppJson, name = "AcceptV1_ReturnsAppJson_WhenV2Configured" },
                new { version = MediaTypeVersion.V2, accepts = new[] { ActuatorMediaTypes.V2Json }, contentType = ActuatorMediaTypes.V2Json, name = "AcceptV2_ReturnsV2_WhenV2Configured" },
                new { version = MediaTypeVersion.V2, accepts = new[] { ActuatorMediaTypes.Any }, contentType = ActuatorMediaTypes.V2Json, name = "AcceptANY_ReturnsV2_WhenV2Configured" },
                new { version = MediaTypeVersion.V2, accepts = new[] { ActuatorMediaTypes.AppJson, ActuatorMediaTypes.V1Json, ActuatorMediaTypes.V2Json }, contentType = ActuatorMediaTypes.V2Json, name = "AcceptAllPossibleAscOrdered_ReturnsV2_WhenV2Configured" },
                new { version = MediaTypeVersion.V2, accepts = new[] { ActuatorMediaTypes.V2Json, ActuatorMediaTypes.V1Json, ActuatorMediaTypes.AppJson }, contentType = ActuatorMediaTypes.V2Json, name = "AcceptAllPossibleDescOrdered_ReturnsV2_WhenV2Configured" }
            };

            foreach (var endpoint in endpoints)
            {
                foreach (var negotiation in negotiations)
                {
                    yield return new object[] { endpoint.epName, endpoint.epPath, negotiation.accepts, negotiation.contentType };
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(EndpointMiddlewareContentNegotiationTestCases))]
    public async Task EndpointMiddleware_ContentNegotiation(EndpointNames epName, string epPath, string[] accepts, string contentType)
    {
        // arrange a server and client
        var builder = new WebHostBuilder()
            .StartupByEpName(epName)
            .ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(AppSettings))
            .ConfigureLogging((webHostContext, loggingBuilder) =>
            {
                loggingBuilder.AddConfiguration(webHostContext.Configuration);
                loggingBuilder.AddDynamicConsole();
            });

        using var server = new TestServer(builder);
        var client = server.CreateClient();
        foreach (var accept in accepts)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", accept);
        }

        // send the request
        var result = await client.GetAsync(epPath);

        var contentHeaders = result.Content.Headers.GetValues("Content-Type");
        Assert.Contains(contentHeaders, header => header.StartsWith(contentType));
    }
}
