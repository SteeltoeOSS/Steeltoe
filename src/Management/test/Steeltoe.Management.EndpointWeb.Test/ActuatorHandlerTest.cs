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
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using Xunit;

namespace Steeltoe.Management.EndpointWeb.Test
{
    public class ActuatorHandlerTest
    {
        private readonly Settings _defaultSettings = DefaultTestSettingsConfig.DefaultSettings;

        [Fact]
        public async void CloudFoundryCorsHandler_ReturnsExpected()
        {
            using (var server = new TestServer(_defaultSettings))
            {
                var client = server.HttpClient;

                var result = await client.GetAsync("http://localhost/management", "OPTIONS");

                Assert.NotNull(result);
                Assert.Equal(3, result.Headers.Count);
                Assert.Contains("Access-Control-Allow-Methods", result.Headers.AllKeys);
                Assert.Contains("Access-Control-Allow-Origin", result.Headers.AllKeys);
                Assert.Contains("Access-Control-Allow-Headers", result.Headers.AllKeys);
            }
        }

        [Fact]
        public async void CloudFoundry_ReturnsExpected()
        {
            using (var server = new TestServer(_defaultSettings))
            {
                var client = server.HttpClient;

                var result = await client.GetAsync("http://localhost/management", "GET");

                Assert.NotNull(result);
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                Assert.Contains("self", result.Content);
            }
        }

        [Fact]
        public async void EnvHandler_ReturnsExpected()
        {
            using (var server = new TestServer(_defaultSettings))
            {
                var client = server.HttpClient;

                var result = await client.GetAsync("http://localhost/management/env", "GET");

                Assert.NotNull(result);
                Assert.Contains("activeProfiles", result.Content);
            }
        }

        [Fact]
        public async void InfoHandler_ReturnsExpected()
        {
            var settings = DefaultTestSettingsConfig.DefaultSettings;
            using (var server = new TestServer(settings))
            {
                var client = server.HttpClient;

                var result = await client.GetAsync("http://localhost/management/infomanagement", "GET");

                Assert.NotNull(result);

                Assert.NotEmpty(result.Content);
                var dict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(result.Content);
                Assert.NotNull(dict);

                Assert.Equal(3, dict.Count);
                Assert.True(dict.ContainsKey("application"));
                Assert.True(dict.ContainsKey("NET"));
                Assert.True(dict.ContainsKey("git"));
            }
        }

        [Fact]
        public async void HealthHandler_ReturnsExpected()
        {
            var settings = DefaultTestSettingsConfig.DefaultSettings;

            using (var server = new TestServer(settings))
            {
                var client = server.HttpClient;

                var response = await client.GetAsync("http://localhost/management/health", "GET");

                Assert.NotEmpty(response.Content);
                Assert.Contains("status", response.Content);
            }
        }

        [Fact]
        public async void HealthHandler_ReturnsDetailsd()
        {
            var settings = DefaultTestSettingsConfig.DefaultSettings;

            using (var server = new TestServer(settings))
            {
                var client = server.HttpClient;

                var response = await client.GetAsync("http://localhost/management/health", "GET");

                Assert.NotEmpty(response.Content);
                Assert.Contains("status", response.Content);

                Assert.Contains("diskSpace", response.Content);
            }
        }

        [Fact]
        public async void LoggersHandler_ReturnsExpected()
        {
            using (var server = new TestServer(_defaultSettings))
            {
                var client = server.HttpClient;

                var response = await client.GetAsync("http://localhost/management/loggers", "GET");

                Assert.NotEmpty(response.Content);
                Assert.Contains("loggers", response.Content);
            }
        }

        [Fact]
        public async void MetricsHandler_ReturnsExpected()
        {
            using (var server = new TestServer(_defaultSettings))
            {
                var client = server.HttpClient;

                var response = await client.GetAsync("http://localhost/management/metrics", "GET");

                Assert.NotEmpty(response.Content);
                Assert.Contains("http.server.request.count", response.Content);
            }
        }

        [Fact]
        public async void ThreadDumpHandler_ReturnsExpected()
        {
            using (var server = new TestServer(_defaultSettings))
            {
                var client = server.HttpClient;

                var response = await client.GetAsync("http://localhost/management/dump", "GET");

                Assert.NotEmpty(response.Content);
                Assert.Contains("stackTrace", response.Content);
            }
        }
    }
}
