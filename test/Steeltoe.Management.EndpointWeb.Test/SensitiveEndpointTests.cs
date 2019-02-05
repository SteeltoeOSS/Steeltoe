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

using System.Net;
using Xunit;

namespace Steeltoe.Management.EndpointWeb.Test
{
    public class SensitiveEndpointTests
    {
        private Settings _defaultSettings = DefaultTestSettingsConfig.DefaultSettings;

        [Fact]
        public async void CloudFoundry_ReturnsUnauthorized()
        {
            var settings = _defaultSettings.Merge(new Settings { ["management:endpoints:sensitive"] = "true" });

            using (var server = new TestServer(settings))
            {
                var client = server.HttpClient;

                var result = await client.GetAsync("http://localhost/management", "GET");

                Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
                Assert.NotNull(result);
                Assert.Contains("Access denied", result.Content);
            }
        }

        [Fact]
        public async void CloudFoundry_ReturnsExpected()
        {
            var settings = _defaultSettings.Merge(
                new Settings
                {
                    ["management:endpoints:sensitive"] = "true",
                    ["management:endpoints:sensitiveclaim:type"] = "scope",
                    ["management:endpoints:sensitiveclaim:value"] = "actuator.read",
                });

            using (var server = new TestServer(settings))
            {
                var client = server.HttpClient;

                client.Context.Setup(ctx => ctx.User).Returns(new TestUser());

                var result = await client.GetAsync("http://localhost/management", "GET");

                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                Assert.NotNull(result);
                Assert.Contains("_links", result.Content);
            }
        }

        [Fact]
        public async void InfoEndpoint_ReturnsUnauthorized()
        {
            var settings = _defaultSettings.Merge(new Settings { ["management:endpoints:info:sensitive"] = "true" });

            using (var server = new TestServer(settings))
            {
                var client = server.HttpClient;

                var result = await client.GetAsync("http://localhost/management/infomanagement", "GET");

                Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
                Assert.NotNull(result);
                Assert.Contains("Access denied", result.Content);
            }
        }

        [Fact]
        public async void InfoEndpoint_ReturnsOKWhenSensitive()
        {
            var settings = _defaultSettings.Merge(new Settings
            {
                ["management:endpoints:info:sensitive"] = "true",
                ["management:endpoints:sensitiveclaim:type"] = "scope",
                ["management:endpoints:sensitiveclaim:value"] = "actuator.read",
            });
            using (var server = new TestServer(settings))
            {
                var client = server.HttpClient;

                client.Context.Setup(ctx => ctx.User).Returns(new TestUser());

                var result = await client.GetAsync("http://localhost/management/infomanagement", "GET");

                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                Assert.NotNull(result);
                Assert.Contains("git", result.Content);
            }
        }

      
        [Fact]
        public async void HealthEndpoint_ReturnsOKWhenSensitive()
        {
            var settings = _defaultSettings.Merge(new Settings
            {
                ["management:endpoints:health:sensitive"] = "true",
                ["management:endpoints:sensitiveclaim:type"] = "scope",
                ["management:endpoints:sensitiveclaim:value"] = "actuator.read",
            });
            using (var server = new TestServer(settings))
            {
                var client = server.HttpClient;

                client.Context.Setup(ctx => ctx.User).Returns(new TestUser());

                var result = await client.GetAsync("http://localhost/management/health", "GET");

                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                Assert.NotNull(result);
                Assert.Contains("status", result.Content);
            }
        }

        [Fact]
        public async void HealthEndpoint_ReturnsUnauthorized()
        {
            var settings = _defaultSettings.Merge(new Settings
            {
                ["management:endpoints:health:sensitive"] = "true"
            });
            using (var server = new TestServer(settings))
            {
                var client = server.HttpClient;

                var result = await client.GetAsync("http://localhost/management/health", "GET");

                Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
                Assert.NotNull(result);
            //    Assert.Contains("Access denied", result.Content);
            }
        }

    }
}