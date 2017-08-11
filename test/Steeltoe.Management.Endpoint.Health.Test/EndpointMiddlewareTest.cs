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
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using Xunit;

namespace Steeltoe.Management.Endpoint.Health.Test
{
    public class EndpointMiddlewareTest  : BaseTest
    {
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
    }
}
