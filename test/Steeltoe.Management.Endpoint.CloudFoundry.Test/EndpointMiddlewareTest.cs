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
using System.Net;
using Xunit;

namespace Steeltoe.Management.Endpoint.CloudFoundry.Test
{
    public class EndpointMiddlewareTest  : BaseTest
    {
        [Fact]
        public async void CloudFoundryActuator_ReturnsExpectedData()
        {

            var builder = new WebHostBuilder().UseStartup<Startup>();
            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var result = await client.GetAsync("http://localhost/cloudfoundryapplication");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                var json = await result.Content.ReadAsStringAsync();
                Assert.NotNull(json);
                var links = JsonConvert.DeserializeObject<Links>(json);
                Assert.NotNull(links);
                Assert.True(links._links.ContainsKey("self"));
                Assert.Equal("http://localhost/cloudfoundryapplication", links._links["self"].href);
                Assert.True(links._links.ContainsKey("info"));
                Assert.Equal("http://localhost/cloudfoundryapplication/info", links._links["info"].href);
            }
        }
    }
}
