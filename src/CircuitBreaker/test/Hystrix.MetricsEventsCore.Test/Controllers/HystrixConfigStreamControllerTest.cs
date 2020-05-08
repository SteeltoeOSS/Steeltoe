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
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Steeltoe.CircuitBreaker.Hystrix.Config;
using Steeltoe.CircuitBreaker.Hystrix.MetricsEvents.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsEvents.Controllers.Test
{
    public class HystrixConfigStreamControllerTest : HystrixTestBase
    {
        [Fact]
        public void Constructor_SetsupStream()
        {
            var stream = HystrixConfigurationStream.GetInstance();
            var controller = new HystrixConfigStreamController(stream);
            Assert.NotNull(controller.SampleStream);
        }

        [Fact]
        public async void Endpoint_ReturnsHeaders()
        {
            var builder = new WebHostBuilder().UseStartup<Startup>();
            using var server = new TestServer(builder);
            var client = server.CreateClient();

            client.BaseAddress = new Uri("http://localhost/");
            var result = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, "hystrix/config.stream"),
                HttpCompletionOption.ResponseHeadersRead);

            Assert.NotNull(result);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.True(result.Headers.Contains("Connection"));
            Assert.Contains("keep-alive", result.Headers.Connection);
            Assert.Equal("text/event-stream", result.Content.Headers.ContentType.MediaType);
            Assert.Equal("UTF-8", result.Content.Headers.ContentType.CharSet);
            Assert.True(result.Headers.CacheControl.NoCache);
            Assert.True(result.Headers.CacheControl.NoStore);
            Assert.Equal(new TimeSpan(0, 0, 0), result.Headers.CacheControl.MaxAge);
            Assert.True(result.Headers.CacheControl.MustRevalidate);
            result.Dispose();
        }

        [Fact]
        public void Endpoint_ReturnsData()
        {
            var builder = new WebHostBuilder().UseStartup<Startup>();
            using var server = new TestServer(builder);
            var client = server.CreateClient();

            client.BaseAddress = new Uri("http://localhost/");
            var result = client.GetStreamAsync("hystrix/config.stream").GetAwaiter().GetResult();

            var client2 = server.CreateClient();
            var cmdResult = client2.GetAsync("test/test.command").GetAwaiter().GetResult();
            Assert.Equal(HttpStatusCode.OK, cmdResult.StatusCode);

            var reader = new StreamReader(result);
            var data = reader.ReadLine();
            reader.Dispose();

            Assert.False(string.IsNullOrEmpty(data));
            Assert.StartsWith("data: ", data);
            var jsonObject = data.Substring(6);
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonObject);
            Assert.NotNull(dict);

            Assert.NotNull(dict["type"]);
            Assert.Equal("HystrixConfig", dict["type"]);
            Assert.NotNull(dict["commands"]);
            Assert.NotNull(dict["threadpools"]);
            var cmds = (JObject)dict["commands"];
            Assert.NotNull(cmds["MyCommand"]);
        }
    }
}
