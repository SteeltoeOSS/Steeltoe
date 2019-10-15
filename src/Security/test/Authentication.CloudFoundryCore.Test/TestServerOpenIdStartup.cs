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
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Net.Http;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test
{
    public class TestServerOpenIdStartup
    {
        public IConfigurationRoot Configuration { get; }

        public TestServerOpenIdStartup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
               .SetBasePath(env.ContentRootPath)
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddCloudFoundry()
               .AddEnvironmentVariables();

            Configuration = builder.Build();
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

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerfactory)
        {
            loggerfactory.AddConsole(LogLevel.Debug);

            app.UseAuthentication();

            app.Run(async context =>
            {
                await context.ChallengeAsync();
                return;
            });
        }
    }
}
