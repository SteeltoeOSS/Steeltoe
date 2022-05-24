// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
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
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CloudFoundryDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
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
            });
        }
    }
}
