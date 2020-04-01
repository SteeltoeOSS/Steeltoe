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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test
{
    public class TestServerCertificateStartup
    {
        public static CloudFoundryJwtBearerOptions CloudFoundryOptions { get; set; }

        public IConfiguration Configuration { get; }

        public TestServerCertificateStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCloudFoundryCertificateAuth(Configuration);
        }

        public void Configure(IApplicationBuilder app, IAuthorizationService authorizationService)
        {
            app.UseCloudFoundryCertificateAuth();

            app.Use(async (context, next) =>
            {
                var authorizationResult = await authorizationService.AuthorizeAsync(context.User, null, context.Request.Path.Value.Replace("/", string.Empty));

                if (!authorizationResult.Succeeded)
                {
                    await context.ChallengeAsync();
                    return;
                }

                context.Response.StatusCode = 200;
            });
        }
    }
}
