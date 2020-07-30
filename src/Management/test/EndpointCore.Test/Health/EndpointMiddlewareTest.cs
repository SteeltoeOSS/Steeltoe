// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Health.Contributor;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Security;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
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
            var middle = new HealthEndpointMiddleware(null, mgmtOptions);
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

            using var server = new TestServer(builder);
            var client = server.CreateClient();
            var result = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var json = await result.Content.ReadAsStringAsync();
            Assert.NotNull(json);

            var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            Assert.NotNull(health);
            Assert.True(health.ContainsKey("status"));
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

            using var server = new TestServer(builder);
            var client = server.CreateClient();

            var result = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var json = await result.Content.ReadAsStringAsync();
            Assert.NotNull(json);

            var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            Assert.NotNull(health);
            Assert.True(health.ContainsKey("status"));
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

            using var server = new TestServer(builder);
            var client = server.CreateClient();

            var result = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var json = await result.Content.ReadAsStringAsync();
            Assert.NotNull(json);

            var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            Assert.NotNull(health);
            Assert.True(health.ContainsKey("status"));
            Assert.True(health.ContainsKey("diskSpace"));
        }

        [Fact]
        public async void HealthActuator_ReturnsDetails()
        {
            var settings = new Dictionary<string, string>(appSettings);

            var builder = new WebHostBuilder()
                .UseStartup<Startup>()
                .ConfigureAppConfiguration((context, config) => config.AddInMemoryCollection(settings));

            using var server = new TestServer(builder);
            var client = server.CreateClient();
            var result = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var json = await result.Content.ReadAsStringAsync();
            Assert.NotNull(json);

            // { "status":"UP","diskSpace":{ "total":499581448192,"free":407577710592,"threshold":10485760,"status":"UP"} }
            var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            Assert.NotNull(health);
            Assert.True(health.ContainsKey("status"));
            Assert.True(health.ContainsKey("diskSpace"));
        }

        [Fact]
        public async void HealthActuator_ReturnsMicrosoftHealthDetails()
        {
            var settings = new Dictionary<string, string>(appSettings);

            var builder = new WebHostBuilder()
                .UseStartup<Startup>()
                .ConfigureAppConfiguration((context, config) => config.AddInMemoryCollection(settings));

            using var server = new TestServer(builder);
            var client = server.CreateClient();
            var result = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var json = await result.Content.ReadAsStringAsync();
            Assert.NotNull(json);

            // { "status":"UP","diskSpace":{ "total":499581448192,"free":407577710592,"threshold":10485760,"status":"UP"} }
            var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            Assert.NotNull(health);
            Assert.True(health.ContainsKey("status"));
            Assert.True(health.ContainsKey("diskSpace"));
        }

        [Fact]
        public async void TestDI()
        {
            var settings = new Dictionary<string, string>(appSettings);

            var builder = new WebHostBuilder()
                .UseStartup<Startup>()
                .ConfigureAppConfiguration((context, config) => config.AddInMemoryCollection(settings));
            builder.ConfigureServices(services =>
            {
                var foo = services.BuildServiceProvider().GetServices<HealthEndpoint>();
                var foo2 = services.BuildServiceProvider().GetServices<HealthEndpointCore>();
                var foo3 = services.BuildServiceProvider().GetServices<IEndpoint<HealthCheckResult, ISecurityContext>>();
            });

            using var server = new TestServer(builder);
            var client = server.CreateClient();
            var result = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var json = await result.Content.ReadAsStringAsync();
            Assert.NotNull(json);
        }

        [Fact]
        public async void GetStatusCode_ReturnsExpected()
        {
            var builder = new WebHostBuilder().ConfigureAppConfiguration((context, config) => config.AddInMemoryCollection(appSettings)).UseStartup<Startup>();

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var result = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                var json = await result.Content.ReadAsStringAsync();
                Assert.NotNull(json);
                Assert.Contains("\"status\":\"UP\"", json);
            }

            builder = new WebHostBuilder().ConfigureAppConfiguration((context, config) => config.AddInMemoryCollection(new Dictionary<string, string>(appSettings) { ["HealthCheckType"] = "down" })).UseStartup<Startup>();

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var downResult = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
                Assert.Equal(HttpStatusCode.ServiceUnavailable, downResult.StatusCode);
                var downJson = await downResult.Content.ReadAsStringAsync();
                Assert.NotNull(downJson);
                Assert.Contains("\"status\":\"DOWN\"", downJson);
            }

            builder = new WebHostBuilder().ConfigureAppConfiguration((context, config) => config.AddInMemoryCollection(new Dictionary<string, string>(appSettings) { ["HealthCheckType"] = "out" })).UseStartup<Startup>();

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

            builder = new WebHostBuilder()
              .UseStartup<Startup>()
              .ConfigureAppConfiguration((context, config) => config.AddInMemoryCollection(new Dictionary<string, string>(appSettings) { ["HealthCheckType"] = "defaultAggregator" }));

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var unknownResult = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
                Assert.Equal(HttpStatusCode.OK, unknownResult.StatusCode);
                var unknownJson = await unknownResult.Content.ReadAsStringAsync();
                Assert.NotNull(unknownJson);
                Assert.Contains("\"status\":\"UP\"", unknownJson);
            }
        }

        [Fact]
        public async void GetStatusCode_MicrosoftAggregator_ReturnsExpected()
        {
            var builder = new WebHostBuilder()
              .UseStartup<Startup>()
              .ConfigureAppConfiguration((context, config) => config.AddInMemoryCollection(new Dictionary<string, string>(appSettings) { ["HealthCheckType"] = "microsoftHealthAggregator" }));

            using var server = new TestServer(builder);
            var client = server.CreateClient();
            var unknownResult = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
            Assert.Equal(HttpStatusCode.OK, unknownResult.StatusCode);
            var unknownJson = await unknownResult.Content.ReadAsStringAsync();
            Assert.NotNull(unknownJson);
            Assert.Contains("\"status\":\"UP\"", unknownJson);
        }

        [Fact]
        public void RoutesByPathAndVerb()
        {
            var options = new HealthEndpointOptions();
            Assert.False(options.ExactMatch);
            Assert.Equal("/actuator/health/{**_}", options.GetContextPath(new ActuatorManagementOptions()));
            Assert.Equal("/cloudfoundryapplication/health/{**_}", options.GetContextPath(new CloudFoundryManagementOptions()));
            Assert.Null(options.AllowedVerbs);
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
