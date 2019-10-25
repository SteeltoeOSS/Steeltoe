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

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using System;
using System.Net.Http;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test
{
    public class TestServerOpenIdStartup
    {
        public IConfiguration Configuration { get; }

        public TestServerOpenIdStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var openIdConfigResponse = Environment.GetEnvironmentVariable("openIdConfigResponse");
            var jwksResponse = Environment.GetEnvironmentVariable("jwksResponse");
            var mockHttpMessageHandler = new MockHttpMessageHandler();

            services.AddOptions();
            services.AddAuthentication((options) =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CloudFoundryDefaults.AuthenticationScheme;
            })
            .AddCookie((options) =>
            {
                options.AccessDeniedPath = new PathString("/Home/AccessDenied");
            })
            .AddCloudFoundryOpenIdConnect(Configuration, (options, config) =>
            {
                var configRequest = mockHttpMessageHandler.Expect(HttpMethod.Get, $"{options.Authority}/.well-known/openid-configuration").Respond("application/json", openIdConfigResponse);
                var jwksRequest = mockHttpMessageHandler.Expect(HttpMethod.Get, $"{options.Authority}/token_keys").Respond("application/json", jwksResponse);
                options.Backchannel = new HttpClient(mockHttpMessageHandler);
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseAuthentication();

            app.Run(async context =>
            {
                await context.ChallengeAsync();
                return;
            });
        }
    }
}
