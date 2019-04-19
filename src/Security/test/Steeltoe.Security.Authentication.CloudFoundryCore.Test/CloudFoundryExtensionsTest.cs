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
using System;
using System.Net;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test
{
    public class CloudFoundryExtensionsTest
    {
    private readonly string vcap_application = @"
{
    'cf_api': 'https://api.system.testcloud.com',
    'limits': { 'fds': 16384, 'mem': 512, 'disk': 1024 },
    'application_name': 'fortuneService',
    'application_uris': [ 'fortuneservice.apps.testcloud.com' ],
    'name': 'fortuneService',
    'space_name': 'test',
    'space_id': '54af9d15-2f18-453b-a533-f0c9e6522c97',
    'uris': [ 'fortuneservice.apps.testcloud.com' ],
    'users': null,
    'application_id': '2ddeb650-187e-41db-a4a6-84fb60567908',
    'version': 'd2911a1c-c81a-47aa-be81-d820a6700d2b',
    'application_version': 'd2911a1c-c81a-47aa-be81-d820a6700d2b'
}";

    private readonly string vcap_services = @"
{
    'user-provided': [{
        'credentials': {
            'client_id': 'testApp',
            'client_secret': 'testApp',
            'uri': 'uaa://login.system.testcloud.com'
        },
        'syslog_drain_url': '',
        'volume_mounts': [],
        'label': 'user-provided',
        'name': 'myOAuthService',
        'tags': []
    }]
}";

        [Fact]
        public async void AddCloudFoundryOAuthAuthentication_AddsIntoPipeline()
        {
            var builder = new WebHostBuilder().UseStartup<TestServerStartup>().UseEnvironment("development");
            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var result = await client.GetAsync("http://localhost/");
                Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
                var location = result.Headers.Location.ToString();
                Assert.StartsWith("http://default_oauthserviceurl/oauth/authorize", location);
            }
        }

        [Fact]
        public async void AddCloudFoundryOAuthAuthentication_AddsIntoPipeline_UsesSSOInfo()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", vcap_application);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", vcap_services);

            var builder = new WebHostBuilder().UseStartup<TestServerStartup>().UseEnvironment("development");
            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var result = await client.GetAsync("http://localhost/");
                Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
                var location = result.Headers.Location.ToString();
                Assert.StartsWith("https://login.system.testcloud.com/oauth/authorize", location);
            }

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
        }

        [Fact]
        public async void AddCloudFoundryJwtBearerAuthentication_AddsIntoPipeline()
        {
            var builder = new WebHostBuilder().UseStartup<TestServerJwtStartup>().UseEnvironment("development");
            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var result = await client.GetAsync("http://localhost/");
                Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
            }
        }

        [Fact(Skip = "This test would require injection of a mock http client/handler into the OpenIdConnectOptions")]
        public async void AddCloudFoundryOpenId_AddsIntoPipeline()
        {
            var builder = new WebHostBuilder().UseStartup<TestServerOpenIdStartup>().UseEnvironment("development");
            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var result = await client.GetAsync("http://localhost/");
                var body = await result.Content.ReadAsStringAsync();
                Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
                var location = result.Headers.Location.ToString();
                Assert.StartsWith("https://default_oauthserviceurl/oauth/authorize", location);
            }
        }

        [Fact(Skip = "This test would require injection of a mock http client/handler into the OpenIdConnectOptions")]
        public async void AddCloudFoundryOpenId_AddsIntoPipeline_UsesSSOInfo()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", vcap_application);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", vcap_services);

            var builder = new WebHostBuilder().UseStartup<TestServerOpenIdStartup>().UseEnvironment("development");
            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var result = await client.GetAsync("http://localhost/");
                Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
                var location = result.Headers.Location.ToString();
                Assert.StartsWith("https://login.system.testcloud.com/oauth/authorize", location);
            }

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
        }
    }
}
