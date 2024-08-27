// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Steeltoe.Management.Endpoint.Test;

internal sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> optionsMonitor, ILoggerFactory loggerFactory, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(optionsMonitor, loggerFactory, encoder)
{
    public const string AuthenticationScheme = "TestScheme";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claim = new Claim("scope", "actuators.read");
        var identity = new ClaimsIdentity([claim]);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
