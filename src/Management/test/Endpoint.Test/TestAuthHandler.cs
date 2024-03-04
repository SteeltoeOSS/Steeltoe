// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Steeltoe.Management.Endpoint.Test;

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "TestScheme";

#if NET6_0
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> optionsMonitor, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(optionsMonitor, logger, encoder, clock)
#else
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> optionsMonitor, ILoggerFactory logger, UrlEncoder encoder)
        : base(optionsMonitor, logger, encoder)
#endif
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("scope", "actuators.read")
        })), AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
