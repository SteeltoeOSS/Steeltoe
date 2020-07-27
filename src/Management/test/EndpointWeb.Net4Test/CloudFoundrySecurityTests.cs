// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Net;
using Xunit;

namespace Steeltoe.Management.EndpointWeb.Test
{
    public class CloudFoundrySecurityTests
    {
        private readonly Settings _defaultSettings = DefaultTestSettingsConfig.DefaultSettings;

        public CloudFoundrySecurityTests()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", "somestuff");
        }

        [Fact]
        public async void CloudFoundrySecurity_ReturnsSecurityException()
        {
            using var server = new TestServer(_defaultSettings);
            var client = server.HttpClient;

            var result = await client.GetAsync("http://localhost/cloudfoundryapplication", "GET");

            Assert.NotNull(result);
            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        }

        [Fact]
        public async void CloudFoundrySecurityMiddleware_ReturnsServiceUnavailable()
        {
            var appSettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "true",
                ["management:endpoints:path"] = "/",
                ["management:endpoints:info:enabled"] = "true",
            };

            using (var server = new TestServer(new Settings(appSettings)))
            {
                var client = server.HttpClient;

                var result = await client.GetAsync("http://localhost/cloudfoundryapplication/info", "GET");

                Assert.NotNull(result);
                Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
            }

            var appSettings2 = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "true",
                ["management:endpoints:path"] = "/",
                ["management:endpoints:info:enabled"] = "true",
                ["info:application:name"] = "foobar",
                ["vcap:application:application_id"] = "foobar"
            };

            using (var server = new TestServer(new Settings(appSettings2)))
            {
                var client = server.HttpClient;
                var result = await client.GetAsync("http://localhost/cloudfoundryapplication/info", "GET");
                Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
            }

            var appSettings3 = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "true",
                ["management:endpoints:path"] = "/",
                ["management:endpoints:info:enabled"] = "true",
                ["info:application:name"] = "foobar",
                ["vcap:application:application_id"] = "foobar",
                ["vcap:application:cf_api"] = "http://localhost:9999/foo"
            };

            using (var server = new TestServer(new Settings(appSettings3)))
            {
                var client = server.HttpClient;
                var result = await client.GetAsync("http://localhost/cloudfoundryapplication/info", "GET");
                Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
            }
        }
    }
}
