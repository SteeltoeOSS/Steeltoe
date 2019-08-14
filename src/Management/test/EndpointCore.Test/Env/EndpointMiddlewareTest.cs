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
#if !NETCOREAPP3_0
using Microsoft.AspNetCore.Hosting.Internal;
#endif

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
#if NETCOREAPP3_0
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
#endif
using Microsoft.Extensions.Logging;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
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
            ["management:endpoints:path"] = "/cloudfoundryapplication"
        };

        [Fact]
        public async void HandleEnvRequestAsync_ReturnsExpected()
        {
            var opts = new EnvEndpointOptions();

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(AppSettings);
            var config = configurationBuilder.Build();
            var host = new HostingEnvironment()
            {
                EnvironmentName = "EnvironmentName"
            };
            var mgmtOptions = TestHelpers.GetManagementOptions(opts);
            var ep = new EnvEndpoint(opts, config, host);
            var middle = new EnvEndpointMiddleware(null, ep, mgmtOptions);

            var context = CreateRequest("GET", "/env");
            await middle.HandleEnvRequestAsync(context);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
            var json = await reader.ReadLineAsync();
            var expected = "{\"activeProfiles\":[\"EnvironmentName\"],\"propertySources\":[{\"properties\":{\"Logging:IncludeScopes\":{\"value\":\"false\"},\"Logging:LogLevel:Default\":{\"value\":\"Warning\"},\"Logging:LogLevel:Pivotal\":{\"value\":\"Information\"},\"Logging:LogLevel:Steeltoe\":{\"value\":\"Information\"},\"management:endpoints:enabled\":{\"value\":\"true\"},\"management:endpoints:path\":{\"value\":\"/cloudfoundryapplication\"}},\"name\":\"MemoryConfigurationProvider\"}]}";
            Assert.Equal(expected, json);
        }

        [Fact]
        public async void EnvActuator_ReturnsExpectedData()
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
                var expected = "{\"activeProfiles\":[\"Production\"],\"propertySources\":[{\"properties\":{\"applicationName\":{\"value\":\"Steeltoe.Management.EndpointCore.Test\"}},\"name\":\"ChainedConfigurationProvider\"},{\"properties\":{\"Logging:IncludeScopes\":{\"value\":\"false\"},\"Logging:LogLevel:Default\":{\"value\":\"Warning\"},\"Logging:LogLevel:Pivotal\":{\"value\":\"Information\"},\"Logging:LogLevel:Steeltoe\":{\"value\":\"Information\"},\"management:endpoints:enabled\":{\"value\":\"true\"},\"management:endpoints:path\":{\"value\":\"/cloudfoundryapplication\"}},\"name\":\"MemoryConfigurationProvider\"}]}";
                Assert.Equal(expected, json);
            }

            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnv);
        }

        [Fact]
        public void EnvEndpointMiddleware_PathAndVerbMatching_ReturnsExpected()
        {
            var opts = new EnvEndpointOptions();
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(AppSettings);
            var config = configurationBuilder.Build();
            var host = new HostingEnvironment() { EnvironmentName = "EnvironmentName" };
            var ep = new EnvEndpoint(opts, config, host);
            var mgmt = new CloudFoundryManagementOptions() { Path = "/" };
            mgmt.EndpointOptions.Add(opts);
            var middle = new EnvEndpointMiddleware(null, ep, new List<IManagementOptions> { mgmt });

            Assert.True(middle.RequestVerbAndPathMatch("GET", "/env"));
            Assert.False(middle.RequestVerbAndPathMatch("PUT", "/env"));
            Assert.False(middle.RequestVerbAndPathMatch("GET", "/badpath"));
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
