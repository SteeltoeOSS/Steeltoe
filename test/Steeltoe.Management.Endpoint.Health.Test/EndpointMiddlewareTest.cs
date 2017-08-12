//
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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Steeltoe.Management.Endpoint.Health.Contributor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Xunit;

namespace Steeltoe.Management.Endpoint.Health.Test
{
    public class EndpointMiddlewareTest  : BaseTest
    {
        [Fact]
        public void IsHealthRequest_ReturnsExpected()
        {
            var opts = new HealthOptions();
            var contribs = new List<IHealthContributor>() { new DiskSpaceContributor() };
            var ep = new HealthEndpoint(opts, new DefaultHealthAggregator(), contribs);
            var middle = new HealthEndpointMiddleware(null, ep);

            var context = CreateRequest("GET", "/health");
            Assert.True(middle.IsHealthRequest(context));

            var context2 = CreateRequest("PUT", "/health");
            Assert.False(middle.IsHealthRequest(context2));

            var context3 = CreateRequest("GET", "/badpath");
            Assert.False(middle.IsHealthRequest(context3));

        }

        [Fact]
        public async void HandleHealthRequestAsync_ReturnsExpected()
        {
            var opts = new HealthOptions();
            var contribs = new List<IHealthContributor>() { new DiskSpaceContributor() };
            var ep = new TestHealthEndpoint(opts, new DefaultHealthAggregator(), contribs);
            var middle = new HealthEndpointMiddleware(null, ep);
            var context = CreateRequest("GET", "/health");
            await middle.HandleHealthRequestAsync(context);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            StreamReader rdr = new StreamReader(context.Response.Body);
            string json = await rdr.ReadToEndAsync();
            Assert.Equal("{\"status\":\"UNKNOWN\"}", json);

        }

        [Fact]
        public async void HealthActuator_ReturnsExpectedData()
        {

            var builder = new WebHostBuilder().UseStartup<Startup>();
            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var result = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                var json = await result.Content.ReadAsStringAsync();
               Assert.NotNull(json);
         
                //{ "status":"UP","diskSpace":{ "total":499581448192,"free":407577710592,"threshold":10485760,"status":"UP"} }
                var health = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                Assert.NotNull(health);
                Assert.True(health.ContainsKey("status"));
                Assert.True(health.ContainsKey("diskSpace"));

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

    class TestHealthEndpoint : HealthEndpoint
    {
        public TestHealthEndpoint(IHealthOptions options, IHealthAggregator aggregator, IEnumerable<IHealthContributor> contributors, ILogger<HealthEndpoint> logger = null) 
            : base(options, aggregator, contributors, logger)
        {
        }
        public override Health Invoke()
        {
            return new Health();
        }
    }
}
