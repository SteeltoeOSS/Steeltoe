// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using Xunit;

namespace Steeltoe.Management.EndpointWeb.Test
{
    public class ActuatorHandlerTest
    {
        private readonly Settings _defaultSettings = DefaultTestSettingsConfig.DefaultSettings;

        public ActuatorHandlerTest()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
        }

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
