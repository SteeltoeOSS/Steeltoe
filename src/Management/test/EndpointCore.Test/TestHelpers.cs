// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.CloudFoundry;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Test
{
    public static class TestHelpers
    {
        public static IEnumerable<IManagementOptions> GetManagementOptions(params IEndpointOptions[] options)
        {
            var mgmtOptions = new CloudFoundryManagementOptions();
            mgmtOptions.EndpointOptions.AddRange(options);
            return new List<IManagementOptions>() { mgmtOptions };
        }
    }

    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public static string AuthenticationScheme = "TestScheme";

        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("scope", "actuators.read") })), TestAuthHandler.AuthenticationScheme);
            return Task.FromResult<AuthenticateResult>(AuthenticateResult.Success(ticket));
        }
    }
}