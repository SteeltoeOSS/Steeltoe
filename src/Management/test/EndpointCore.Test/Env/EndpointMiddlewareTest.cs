// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Endpoint.Env.Test
{
    public class EndpointMiddlewareTest : BaseTest
    {
        private static readonly Dictionary<string, string> AppSettings = new Dictionary<string, string>()
        {
            ["Logging:IncludeScopes"] = "false",
            ["Logging:LogLevel:Default"] = "Warning",
            ["Logging:LogLevel:Pivotal"] = "Information",
            ["Logging:LogLevel:Steeltoe"] = "Information",
            ["management:endpoints:enabled"] = "true",
        };

        private IHostEnvironment host = HostingHelpers.GetHostingEnvironment();

        [Fact]
        public async Task HandleEnvRequestAsync_ReturnsExpected()
        {
            var opts = new EnvEndpointOptions();

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(AppSettings);
            var config = configurationBuilder.Build();
            var mopts = new ActuatorManagementOptions();
            mopts.EndpointOptions.Add(opts);
            var ep = new EnvEndpoint(opts, config, host);
            var middle = new EnvEndpointMiddleware(null, ep, mopts);

            var context = CreateRequest("GET", "/env");
            await middle.HandleEnvRequestAsync(context);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
            var json = await reader.ReadLineAsync();
            var expected = "{\"activeProfiles\":[\"EnvironmentName\"],\"propertySources\":[{\"name\":\"MemoryConfigurationProvider\",\"properties\":{\"Logging:IncludeScopes\":{\"value\":\"false\"},\"Logging:LogLevel:Default\":{\"value\":\"Warning\"},\"Logging:LogLevel:Pivotal\":{\"value\":\"Information\"},\"Logging:LogLevel:Steeltoe\":{\"value\":\"Information\"},\"management:endpoints:enabled\":{\"value\":\"true\"}}}]}";
            Assert.Equal(expected, json);
        }

        [Fact]
        public async Task EnvActuator_ReturnsExpectedData()
        {
            // Some developers set ASPNETCORE_ENVIRONMENT in their environment, which will break this test if we don't un-set it
            var originalEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
            var builder = new WebHostBuilder()
                .UseStartup<Startup>()
                .ConfigureAppConfiguration((builderContext, config) => config.AddInMemoryCollection(AppSettings))
                .ConfigureLogging((webhostContext, loggingBuilder) =>
                {
                    loggingBuilder.AddConfiguration(webhostContext.Configuration);
                    loggingBuilder.AddDynamicConsole();
                });
            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var result = await client.GetAsync("http://localhost/cloudfoundryapplication/env");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                var json = await result.Content.ReadAsStringAsync();
                var expected = "{\"activeProfiles\":[\"Production\"],\"propertySources\":[{\"name\":\"ChainedConfigurationProvider\",\"properties\":{\"applicationName\":{\"value\":\"Steeltoe.Management.EndpointCore.Test\"}}},{\"name\":\"MemoryConfigurationProvider\",\"properties\":{\"Logging:IncludeScopes\":{\"value\":\"false\"},\"Logging:LogLevel:Default\":{\"value\":\"Warning\"},\"Logging:LogLevel:Pivotal\":{\"value\":\"Information\"},\"Logging:LogLevel:Steeltoe\":{\"value\":\"Information\"},\"management:endpoints:enabled\":{\"value\":\"true\"}}}]}";
                Assert.Equal(expected, json);
            }

            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnv);
        }

        [Fact]
        public void RoutesByPathAndVerb()
        {
            var options = new EnvEndpointOptions();
            Assert.True(options.ExactMatch);
            Assert.Equal("/actuator/env", options.GetContextPath(new ActuatorManagementOptions()));
            Assert.Equal("/cloudfoundryapplication/env", options.GetContextPath(new CloudFoundryManagementOptions()));
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
