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
using Steeltoe.Management.EndpointBase;
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

        [Theory]
        [InlineData(EndpointNames.Hypermedia, "http://localhost/actuator")]
        [InlineData(EndpointNames.Cloudfoundry, "http://localhost/actuator")]
        [InlineData(EndpointNames.Info, "http://localhost/actuator/info")]
        [InlineData(EndpointNames.Metrics, "http://localhost/actuator/metrics")]
        [InlineData(EndpointNames.Loggers, "http://localhost/actuator/loggers")]
        [InlineData(EndpointNames.Health, "http://localhost/actuator/health")]
        [InlineData(EndpointNames.Trace, "http://localhost/actuator/trace")]
        [InlineData(EndpointNames.Env, "http://localhost/actuator/env")]
        [InlineData(EndpointNames.Mappings, "http://localhost/actuator/mappings")]
        [InlineData(EndpointNames.Refresh, "http://localhost/actuator/refresh")]

        public async void EndpointMiddleware_AcceptAppJson_ReturnsExpected(EndpointNames epName, string epPath)
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
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // send the request
                var result = await client.GetAsync(epPath);
                var json = await result.Content.ReadAsStringAsync();

                var contentHeaders = result.Content.Headers.GetValues("Content-Type");
                Assert.Contains("application/json; charset=UTF-8", contentHeaders);
            }
        }

        [Theory]
        [InlineData(EndpointNames.Hypermedia, "http://localhost/actuator")]
        [InlineData(EndpointNames.Cloudfoundry, "http://localhost/actuator")]
        [InlineData(EndpointNames.Info, "http://localhost/actuator/info")]
        [InlineData(EndpointNames.Metrics, "http://localhost/actuator/metrics")]
        [InlineData(EndpointNames.Loggers, "http://localhost/actuator/loggers")]
        [InlineData(EndpointNames.Health, "http://localhost/actuator/health")]
        [InlineData(EndpointNames.Trace, "http://localhost/actuator/trace")]
        [InlineData(EndpointNames.Env, "http://localhost/actuator/env")]
        [InlineData(EndpointNames.Mappings, "http://localhost/actuator/mappings")]
        [InlineData(EndpointNames.Refresh, "http://localhost/actuator/refresh")]
        public async void EndpointMiddleware_AcceptV1_JSON_WhenNotConfigured_ReturnsAppJson(EndpointNames epName, string epPath)
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
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(ActuatorMediaTypes.V1_JSON));

                // send the request
                var result = await client.GetAsync(epPath);
                var json = await result.Content.ReadAsStringAsync();

                var contentHeaders = result.Content.Headers.GetValues("Content-Type");
                Assert.Contains("application/json; charset=UTF-8", contentHeaders);
            }
        }

        [Theory]
        [InlineData(EndpointNames.Hypermedia, "http://localhost/actuator")]
        [InlineData(EndpointNames.Cloudfoundry, "http://localhost/actuator")]
        [InlineData(EndpointNames.Info, "http://localhost/actuator/info")]
        [InlineData(EndpointNames.Metrics, "http://localhost/actuator/metrics")]
        [InlineData(EndpointNames.Loggers, "http://localhost/actuator/loggers")]
        [InlineData(EndpointNames.Health, "http://localhost/actuator/health")]
        [InlineData(EndpointNames.Trace, "http://localhost/actuator/trace")]
        [InlineData(EndpointNames.Env, "http://localhost/actuator/env")]
        [InlineData(EndpointNames.Mappings, "http://localhost/actuator/mappings")]
        [InlineData(EndpointNames.Refresh, "http://localhost/actuator/refresh")]
        public async void EndpointMiddleware_AcceptInvalid_ReturnsAppJson(EndpointNames epName, string epPath)
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
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "foo");

                // send the request
                var result = await client.GetAsync(epPath);
                var json = await result.Content.ReadAsStringAsync();

                var contentHeaders = result.Content.Headers.GetValues("Content-Type");
                Assert.Contains("application/json; charset=UTF-8", contentHeaders);
            }
        }

        [Theory]
        [InlineData(EndpointNames.Hypermedia, "http://localhost/actuator")]
        [InlineData(EndpointNames.Cloudfoundry, "http://localhost/actuator")]
        [InlineData(EndpointNames.Info, "http://localhost/actuator/info")]
        [InlineData(EndpointNames.Metrics, "http://localhost/actuator/metrics")]
        [InlineData(EndpointNames.Loggers, "http://localhost/actuator/loggers")]
        [InlineData(EndpointNames.Health, "http://localhost/actuator/health")]
        [InlineData(EndpointNames.Trace, "http://localhost/actuator/trace")]
        [InlineData(EndpointNames.Env, "http://localhost/actuator/env")]
        [InlineData(EndpointNames.Mappings, "http://localhost/actuator/mappings")]
        [InlineData(EndpointNames.Refresh, "http://localhost/actuator/refresh")]
        public async void EndpointMiddleware_AcceptV2_JSON_ReturnsExpected(EndpointNames epName, string epPath)
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
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(ActuatorMediaTypes.V2_JSON));

                // send the request
                var result = await client.GetAsync(epPath);
                var json = await result.Content.ReadAsStringAsync();

                var contentHeaders = result.Content.Headers.GetValues("Content-Type");
                Assert.Contains(contentHeaders, (header) => header.StartsWith(ActuatorMediaTypes.V2_JSON));
            }
        }

        [Theory]
        [InlineData(EndpointNames.Hypermedia, "http://localhost/actuator")]
        [InlineData(EndpointNames.Cloudfoundry, "http://localhost/actuator")]
        [InlineData(EndpointNames.Info, "http://localhost/actuator/info")]
        [InlineData(EndpointNames.Metrics, "http://localhost/actuator/metrics")]
        [InlineData(EndpointNames.Loggers, "http://localhost/actuator/loggers")]
        [InlineData(EndpointNames.Health, "http://localhost/actuator/health")]
        [InlineData(EndpointNames.Trace, "http://localhost/actuator/trace")]
        [InlineData(EndpointNames.Env, "http://localhost/actuator/env")]
        [InlineData(EndpointNames.Mappings, "http://localhost/actuator/mappings")]
        [InlineData(EndpointNames.Refresh, "http://localhost/actuator/refresh")]
        public async void EndpointMiddleware_Accept_ANY_Returns_MostSpecific(EndpointNames epName, string epPath)
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
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(ActuatorMediaTypes.ANY));

                // send the request
                var result = await client.GetAsync(epPath);
                var json = await result.Content.ReadAsStringAsync();

                var contentHeaders = result.Content.Headers.GetValues("Content-Type");
                Assert.Contains(contentHeaders, (header) => header.StartsWith(ActuatorMediaTypes.V2_JSON));
            }
        }
    }
}
