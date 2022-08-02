// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test;

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
        services.AddCloudFoundryCertificateAuth();
    }

    public void Configure(IApplicationBuilder app, IAuthorizationService authorizationService)
    {
        app.UseCloudFoundryCertificateAuth();

        app.Run(async context =>
        {
            AuthorizationResult authorizationResult =
                await authorizationService.AuthorizeAsync(context.User, null, context.Request.Path.Value.Replace("/", string.Empty));

            if (!authorizationResult.Succeeded)
            {
                await context.ChallengeAsync();
                return;
            }

            context.Response.StatusCode = 200;
        });
    }
}
