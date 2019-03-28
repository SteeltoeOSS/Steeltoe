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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Health.Contributor;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Xunit;

namespace Steeltoe.Management.Endpoint.Health.Test
{
    public class EndpointMiddlewareTest : BaseTest
    {
        private readonly Dictionary<string, string> appSettings = new Dictionary<string, string>()
        {
            ["management:endpoints:enabled"] = "true",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:health:enabled"] = "true"
        };

        [Fact]
        public async void HandleHealthRequestAsync_ReturnsExpected()
        {
            var opts = new HealthEndpointOptions();
            var mgmtOptions = new CloudFoundryManagementOptions();
            mgmtOptions.EndpointOptions.Add(opts);
            var contribs = new List<IHealthContributor>() { new DiskSpaceContributor() };
            var ep = new TestHealthEndpoint(opts, new DefaultHealthAggregator(), contribs);
            var middle = new HealthEndpointMiddleware(null, new List<IManagementOptions> { mgmtOptions });
            middle.Endpoint = ep;

            var context = CreateRequest("GET", "/health");
            await middle.HandleHealthRequestAsync(context);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            StreamReader rdr = new StreamReader(context.Response.Body);
            string json = await rdr.ReadToEndAsync();
            Assert.Equal("{\"status\":\"UNKNOWN\"}", json);
        }

        [Fact]
        public async void HealthActuator_ReturnsOnlyStatus()
        {
            var builder = new WebHostBuilder()
                .UseStartup<Startup>()
                .ConfigureAppConfiguration((context, config) => config.AddInMemoryCollection(appSettings));

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var result = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                var json = await result.Content.ReadAsStringAsync();
                Assert.NotNull(json);

                var health = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                Assert.NotNull(health);
                Assert.True(health.ContainsKey("status"));
            }
        }

        [Fact]
        public async void HealthActuator_ReturnsOnlyStatusWhenAuthorized()
        {
            var settings = new Dictionary<string, string>(appSettings)
            {
                { "management:endpoints:health:showdetails", "whenauthorized" }
            };
            var builder = new WebHostBuilder()
                .UseStartup<AuthStartup>()
                .ConfigureAppConfiguration((context, config) => config.AddInMemoryCollection(settings));

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();

                var result = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                var json = await result.Content.ReadAsStringAsync();
                Assert.NotNull(json);

                var health = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                Assert.NotNull(health);
                Assert.True(health.ContainsKey("status"));
            }
        }

        [Fact]
        public async void HealthActuator_ReturnsDetailsWhenAuthorized()
        {
            var settings = new Dictionary<string, string>(appSettings)
            {
                { "management:endpoints:health:showdetails", "whenauthorized" },
                { "management:endpoints:health:claim:type", "healthdetails" },
                { "management:endpoints:health:claim:value", "show" }
            };
            var builder = new WebHostBuilder()
                .UseStartup<AuthStartup>()
                .ConfigureAppConfiguration((context, config) => config.AddInMemoryCollection(settings));

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();

                var result = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                var json = await result.Content.ReadAsStringAsync();
                Assert.NotNull(json);

                var health = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                Assert.NotNull(health);
                Assert.True(health.ContainsKey("status"));
                Assert.True(health.ContainsKey("diskSpace"));
            }
        }

        [Fact]
        public async void HealthActuator_ReturnsDetails()
        {
            var settings = new Dictionary<string, string>(appSettings);

            var builder = new WebHostBuilder()
                .UseStartup<Startup>()
                .ConfigureAppConfiguration((context, config) => config.AddInMemoryCollection(settings));

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var result = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                var json = await result.Content.ReadAsStringAsync();
                Assert.NotNull(json);

                // { "status":"UP","diskSpace":{ "total":499581448192,"free":407577710592,"threshold":10485760,"status":"UP"} }
                var health = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                Assert.NotNull(health);
                Assert.True(health.ContainsKey("status"));
                Assert.True(health.ContainsKey("diskSpace"));
            }
        }

        [Fact]
        public async void GetStatusCode_ReturnsExpected()
        {
            var builder = new WebHostBuilder()
               .UseStartup<Startup>()
               .ConfigureAppConfiguration((context, config) => config.AddInMemoryCollection(appSettings));

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var result = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                var json = await result.Content.ReadAsStringAsync();
                Assert.NotNull(json);
                Assert.Contains("\"status\":\"UP\"", json);
            }

            builder = new WebHostBuilder()
               .UseStartup<Startup>()
               .ConfigureAppConfiguration((context, config) => config.AddInMemoryCollection(new Dictionary<string, string>(appSettings) { ["HealthCheckType"] = "down" }));

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var downResult = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
                Assert.Equal(HttpStatusCode.ServiceUnavailable, downResult.StatusCode);
                var downJson = await downResult.Content.ReadAsStringAsync();
                Assert.NotNull(downJson);
                Assert.Contains("\"status\":\"DOWN\"", downJson);
            }

            builder = new WebHostBuilder()
               .UseStartup<Startup>()
               .ConfigureAppConfiguration((context, config) => config.AddInMemoryCollection(new Dictionary<string, string>(appSettings) { ["HealthCheckType"] = "out" }));

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var outResult = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
                Assert.Equal(HttpStatusCode.ServiceUnavailable, outResult.StatusCode);
                var outJson = await outResult.Content.ReadAsStringAsync();
                Assert.NotNull(outJson);
                Assert.Contains("\"status\":\"OUT_OF_SERVICE\"", outJson);
            }

            builder = new WebHostBuilder()
               .UseStartup<Startup>()
               .ConfigureAppConfiguration((context, config) => config.AddInMemoryCollection(new Dictionary<string, string>(appSettings) { ["HealthCheckType"] = "unknown" }));

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var unknownResult = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
                Assert.Equal(HttpStatusCode.OK, unknownResult.StatusCode);
                var unknownJson = await unknownResult.Content.ReadAsStringAsync();
                Assert.NotNull(unknownJson);
                Assert.Contains("\"status\":\"UNKNOWN\"", unknownJson);
            }
        }

        [Fact]
        public void HealthEndpointMiddleware_PathAndVerbMatching_ReturnsExpected()
        {
            var opts = new HealthEndpointOptions();
            var contribs = new List<IHealthContributor>() { new DiskSpaceContributor() };
            var ep = new HealthEndpoint(opts, new DefaultHealthAggregator(), contribs);
            var actMOptions = new ActuatorManagementOptions();
            actMOptions.EndpointOptions.Add(opts);
            var middle = new HealthEndpointMiddleware(null, new List<IManagementOptions> { actMOptions });
            middle.Endpoint = ep;

            Assert.True(middle.RequestVerbAndPathMatch("GET", "/actuator/health"));
            Assert.False(middle.RequestVerbAndPathMatch("PUT", "/actuator/health"));
            Assert.False(middle.RequestVerbAndPathMatch("GET", "/actuator/badpath"));
        }

        private HttpContext CreateRequest(string method, string path)
        {
            HttpContext context = new DefaultHttpContext
            {
                TraceIdentifier = Guid.NewGuid().ToString()
            };
            context.Response.Body = new MemoryStream();
            context.Request.Method = method;
            context.Request.Path = new PathString(path);
            context.Request.Scheme = "http";
            context.Request.Host = new HostString("localhost");
            return context;
        }
    }
}
