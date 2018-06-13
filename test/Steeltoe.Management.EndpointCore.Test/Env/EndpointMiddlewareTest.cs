// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Xunit;

namespace Steeltoe.Management.Endpoint.Env.Test
{
    public class EndpointMiddlewareTest : BaseTest
    {
        private static Dictionary<string, string> appsettings = new Dictionary<string, string>()
        {
            ["Logging:IncludeScopes"] = "false",
            ["Logging:LogLevel:Default"] = "Warning",
            ["Logging:LogLevel:Pivotal"] = "Information",
            ["Logging:LogLevel:Steeltoe"] = "Information",
            ["management:endpoints:enabled"] = "true",
            ["management:endpoints:sensitive"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication"
        };

        [Fact]
        public void IsEnvRequest_ReturnsExpected()
        {
            var opts = new EnvOptions();

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();
            var host = new HostingEnvironment()
            {
                EnvironmentName = "EnvironmentName"
            };
            var ep = new EnvEndpoint(opts, config, host);
            var middle = new EnvEndpointMiddleware(null, ep);

            var context = CreateRequest("GET", "/env");
            Assert.True(middle.IsEnvRequest(context));
            var context2 = CreateRequest("PUT", "/env");
            Assert.False(middle.IsEnvRequest(context2));
            var context3 = CreateRequest("GET", "/badpath");
            Assert.False(middle.IsEnvRequest(context3));
        }

        [Fact]
        public async void HandleEnvRequestAsync_ReturnsExpected()
        {
            var opts = new EnvOptions();

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();
            var host = new HostingEnvironment()
            {
                EnvironmentName = "EnvironmentName"
            };
            var ep = new EnvEndpoint(opts, config, host);
            var middle = new EnvEndpointMiddleware(null, ep);

            var context = CreateRequest("GET", "/env");
            await middle.HandleEnvRequestAsync(context);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
            var json = await reader.ReadLineAsync();
            var expected = "{\"activeProfiles\":[\"EnvironmentName\"],\"propertySources\":[{\"properties\":{\"management:endpoints:sensitive\":{\"value\":\"false\"},\"management:endpoints:path\":{\"value\":\"/cloudfoundryapplication\"},\"management:endpoints:enabled\":{\"value\":\"true\"},\"Logging:LogLevel:Steeltoe\":{\"value\":\"Information\"},\"Logging:LogLevel:Pivotal\":{\"value\":\"Information\"},\"Logging:LogLevel:Default\":{\"value\":\"Warning\"},\"Logging:IncludeScopes\":{\"value\":\"false\"}},\"name\":\"MemoryConfigurationProvider\"}]}";
            Assert.Equal(expected, json);
        }

        [Fact]
        public async void EnvActuator_ReturnsExpectedData()
        {
            var builder = new WebHostBuilder()
            .UseStartup<Startup>()
            .ConfigureAppConfiguration((builderContext, config) => config.AddInMemoryCollection(appsettings))
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
                var expected = "{\"activeProfiles\":[\"Production\"],\"propertySources\":[{\"properties\":{\"applicationName\":{\"value\":\"Steeltoe.Management.EndpointCore.Test\"}},\"name\":\"ChainedConfigurationProvider\"},{\"properties\":{\"management:endpoints:sensitive\":{\"value\":\"false\"},\"management:endpoints:path\":{\"value\":\"/cloudfoundryapplication\"},\"management:endpoints:enabled\":{\"value\":\"true\"},\"Logging:LogLevel:Steeltoe\":{\"value\":\"Information\"},\"Logging:LogLevel:Pivotal\":{\"value\":\"Information\"},\"Logging:LogLevel:Default\":{\"value\":\"Warning\"},\"Logging:IncludeScopes\":{\"value\":\"false\"}},\"name\":\"MemoryConfigurationProvider\"}]}";
                Assert.Equal(expected, json);
            }
        }

        private HttpContext CreateRequest(string method, string path)
        {
            HttpContext context = new DefaultHttpContext();
            context.TraceIdentifier = Guid.NewGuid().ToString();
            context.Response.Body = new MemoryStream();
            context.Request.Method = method;
            context.Request.Path = new PathString(path);
            context.Request.Scheme = "http";
            context.Request.Host = new HostString("localhost");
            return context;
        }
    }
}
