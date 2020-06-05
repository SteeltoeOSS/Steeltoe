// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CircuitBreaker.Hystrix.MetricsEvents.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsEvents.Controllers.Test
{
    public class HystrixMetricsStreamControllerTest : HystrixTestBase
    {
        [Fact]
        public void Constructor_SetsupStream()
        {
            var stream = HystrixDashboardStream.GetInstance();
            var controller = new HystrixMetricsStreamController(stream);
            Assert.NotNull(controller.SampleStream);
        }

        [Fact]
        public async void Endpoint_ReturnsHeaders()
        {
            var builder = new WebHostBuilder().UseStartup<Startup>();
            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();

                client.BaseAddress = new Uri("http://localhost/");
                var result = await client.SendAsync(
                    new HttpRequestMessage(HttpMethod.Get, "hystrix/hystrix.stream"),
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
        }

        [Fact]
        public void Endpoint_ReturnsData()
        {
            var builder = new WebHostBuilder().UseStartup<Startup>();
            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();

                client.BaseAddress = new Uri("http://localhost/");
                var result = client.GetStreamAsync("hystrix/hystrix.stream").GetAwaiter().GetResult();

                var client2 = server.CreateClient();
                var cmdResult = client2.GetAsync("test/test.command").GetAwaiter().GetResult();
                Assert.Equal(HttpStatusCode.OK, cmdResult.StatusCode);

                var reader = new StreamReader(result);
                string data = reader.ReadLine();
                reader.Dispose();

                Assert.False(string.IsNullOrEmpty(data));
                Assert.StartsWith("data: ", data);
                string jsonObject = data.Substring(6);
                var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonObject);
                Assert.NotNull(dict);

                Assert.NotNull(dict["type"]);
                Assert.Equal("HystrixCommand", dict["type"]);
                Assert.NotNull(dict["name"]);
                Assert.Equal("MyCommand", dict["name"]);
                Assert.NotNull(dict["group"]);
                Assert.Equal("MyCommandGroup", dict["group"]);
            }
        }
    }
}
