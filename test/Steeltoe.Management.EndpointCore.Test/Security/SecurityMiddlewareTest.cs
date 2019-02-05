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
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Test;
using System.Collections.Generic;
using System.Net;
using Xunit;

namespace Steeltoe.Management.Endpoint.Security.Test
{
    public class SecurityMiddlewareTest : BaseTest
    {
        [Fact]
        public async void SecurityMiddleWare_ReturnsOKWhenNotSensitive()
        {
            var builder = GetBuilder<Startup>(new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "true",
                ["management:endpoints:info:sensitive"] = "false"
            });

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var result = await client.GetAsync("http://localhost/actuator/info");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            }
        }

        [Fact]
        public async void SecurityMiddleWare_ReturnsOKWhenNotSensitiveWhenAuthenticated()
        {
            var builder = GetBuilder<Startup>(new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "true",
                ["management:endpoints:info:sensitive"] = "false"
            });

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                client.DefaultRequestHeaders.Add("X-Test-Header", "value");

                var result = await client.GetAsync("http://localhost/actuator/info");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            }
        }

        [Fact]
        public async void SecurityMiddleWare_ReturnsUnauthorizedWhenSensitive()
        {
            var builder = GetBuilder<Startup>(new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "true",
                ["management:endpoints:info:sensitive"] = "true"
            });

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var result = await client.GetAsync("http://localhost/actuator/info");
                Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
            }
        }

        [Fact]
        public async void SecurityMiddleWareActuator_ReturnsUnauthorizedWhenSensitive()
        {
            var builder = GetBuilder<Startup>(new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "true",
                ["management:endpoints:sensitive"] = "true"
            });

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var result = await client.GetAsync("http://localhost/actuator/info");
                Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
            }
        }

        [Fact]
        public async void SecurityMiddleWareCloudFoundry_ReturnsOKWhenNOTSensitive()
        {
            var builder = GetBuilder<Startup>(new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "true",
                ["management:endpoints:sensitive"] = "false"
            });

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var result = await client.GetAsync("http://localhost/actuator/info");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            }
        }

        [Fact]
        public async void SecurityMiddleWare_ReturnsOkWhenSensitive()
        {
            var builder = GetBuilder<SecureStartup>(new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "true",
                ["management:endpoints:info:sensitive"] = "true",
                ["management:endpoints:sensitiveclaim:type"] = "scope",
                ["management:endpoints:sensitiveclaim:value"] = "actuator.read",
            });

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                client.DefaultRequestHeaders.Add("X-Test-Header", "value");
                var result = await client.GetAsync("http://localhost/actuator/info");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            }
        }

       private IWebHostBuilder GetBuilder<T>(Dictionary<string, string> settings)
            where T : Startup
        {
            return new WebHostBuilder()
                .UseStartup<T>()
                .ConfigureAppConfiguration((builderContet, config) => config.AddInMemoryCollection(settings));
        }
    }
}
