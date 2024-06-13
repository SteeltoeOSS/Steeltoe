// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Security.Authorization.Certificate.Test;

public sealed class TestServerCertificateStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthentication().AddCertificate(options =>
        {
            options.ValidateValidityPeriod = false;
        });

        services.AddAuthorizationBuilder().AddAppInstanceIdentityCertificate();
    }

    public void Configure(IApplicationBuilder app, IAuthorizationService authorizationService)
    {
        app.UseCertificateAuthorization();

        app.Run(async context =>
        {
            AuthorizationResult authorizationResult = await authorizationService.AuthorizeAsync(context.User, null,
                context.Request.Path.Value!.Replace("/", string.Empty, StringComparison.Ordinal));

            if (!authorizationResult.Succeeded)
            {
                await context.ChallengeAsync();
                return;
            }

            context.Response.StatusCode = 200;
        });
    }
}
