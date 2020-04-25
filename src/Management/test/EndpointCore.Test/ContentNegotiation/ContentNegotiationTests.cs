// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using Xunit;
using static Steeltoe.Management.EndpointCore.Test.ContentNegotiation.TestStartupExtensions;

namespace Steeltoe.Management.EndpointCore.Test.ContentNegotiation
{
    public class ContentNegotiationTests
    {
        private static readonly Dictionary<string, string> AppSettings = new Dictionary<string, string>()
        {
            ["management:endpoints:actuator:exposure:include:0"] = "*"
        };

        public static IEnumerable<object[]> EndpointMiddleware_ContentNegotiation_TestCases
        {
            get
            {
                var endpoints = new[]
                {
                    new { epName = EndpointNames.Hypermedia, epPath = "http://localhost/actuator" },
                    new { epName = EndpointNames.Cloudfoundry, epPath = "http://localhost/actuator" },
                    new { epName = EndpointNames.Info, epPath = "http://localhost/actuator/info" },
                    new { epName = EndpointNames.Metrics, epPath = "http://localhost/actuator/metrics" },
                    new { epName = EndpointNames.Loggers, epPath = "http://localhost/actuator/loggers" },
                    new { epName = EndpointNames.Health, epPath = "http://localhost/actuator/health" },
                    new { epName = EndpointNames.Trace, epPath = "http://localhost/actuator/trace" },
                    new { epName = EndpointNames.Env, epPath = "http://localhost/actuator/env" },
                    new { epName = EndpointNames.Mappings, epPath = "http://localhost/actuator/mappings" },
                    new { epName = EndpointNames.Refresh, epPath = "http://localhost/actuator/refresh" }
                };

                var negotations = new[]
                {
                    new { version = MediaTypeVersion.V2, accepts = new string[] { ActuatorMediaTypes.APP_JSON }, contentType = ActuatorMediaTypes.APP_JSON, name = "AcceptAppJson_RetrunsAppJson" },
                    new { version = MediaTypeVersion.V2, accepts = new string[] { "foo" }, contentType = ActuatorMediaTypes.APP_JSON, name = "AcceptInvalid_RetrunsAppJson" },
                    new { version = MediaTypeVersion.V2, accepts = new string[] { ActuatorMediaTypes.V1_JSON }, contentType = ActuatorMediaTypes.APP_JSON, name = "AcceptV1_RetrunsAppJson_WhenV2Configured" },
                    new { version = MediaTypeVersion.V2, accepts = new string[] { ActuatorMediaTypes.V2_JSON }, contentType = ActuatorMediaTypes.V2_JSON, name = "AcceptV2_RetrunsV2_WhenV2Configured" },
                    new { version = MediaTypeVersion.V2, accepts = new string[] { ActuatorMediaTypes.ANY }, contentType = ActuatorMediaTypes.V2_JSON, name = "AcceptANY_RetrunsV2_WhenV2Configured" },
                    new { version = MediaTypeVersion.V2, accepts = new string[] { ActuatorMediaTypes.APP_JSON, ActuatorMediaTypes.V1_JSON, ActuatorMediaTypes.V2_JSON }, contentType = ActuatorMediaTypes.V2_JSON, name = "AcceptAllPossibleAscOrdered_RetrunsV2_WhenV2Configured" },
                    new { version = MediaTypeVersion.V2, accepts = new string[] { ActuatorMediaTypes.V2_JSON, ActuatorMediaTypes.V1_JSON, ActuatorMediaTypes.APP_JSON }, contentType = ActuatorMediaTypes.V2_JSON, name = "AcceptAllPossibleDescOrdered_RetrunsV2_WhenV2Configured" }
                };

                foreach (var endpoint in endpoints)
                {
                    foreach (var negotation in negotations)
                    {
                        yield return new object[] { endpoint.epName, endpoint.epPath, negotation.version, negotation.accepts, negotation.contentType, negotation.name };
                    }
                }
            }
        }

        /// <param name="version">For now there is no way to configure version - defined for future use</param>
        [Theory]
        [MemberData(nameof(EndpointMiddleware_ContentNegotiation_TestCases))]
        public async void EndpointMiddleware_ContentNegotiation(EndpointNames epName, string epPath, MediaTypeVersion version, string[] accepts, string contentType, string name)
        {
            // arrange a server and client
            var builder = new WebHostBuilder()
                .StartupByEpName(epName)
                .ConfigureAppConfiguration((builderContext, config) => config.AddInMemoryCollection(AppSettings))
                .ConfigureLogging((webhostContext, loggingBuilder) =>
                {
                    loggingBuilder.AddConfiguration(webhostContext.Configuration);
                    loggingBuilder.AddDynamicConsole();
                });

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                foreach (var accept in accepts)
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", accept);
                }

                // send the request
                var result = await client.GetAsync(epPath);
                var json = await result.Content.ReadAsStringAsync();

                var contentHeaders = result.Content.Headers.GetValues("Content-Type");
                Assert.Contains(contentHeaders, (header) => header.StartsWith(contentType));
            }
        }
    }
}
