// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test
{
    public class MyTestCloudFoundryHandler : CloudFoundryOAuthHandler
    {
        public MyTestCloudFoundryHandler(
            IOptionsMonitor<CloudFoundryOAuthOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        public async Task<AuthenticationTicket> TestCreateTicketAsync(ClaimsIdentity identity, AuthenticationProperties properties, OAuthTokenResponse tokens)
        {
            return await CreateTicketAsync(identity, properties, tokens);
        }

        public async Task<OAuthTokenResponse> TestExchangeCodeAsync(string code, string redirectUri)
        {
#if NETCOREAPP3_0
            return await ExchangeCodeAsync(new OAuthCodeExchangeContext(new AuthenticationProperties(), code, redirectUri));
#else
            return await ExchangeCodeAsync(code, redirectUri);
#endif
        }

        public string TestBuildChallengeUrl(AuthenticationProperties properties, string redirectUri)
        {
            return BuildChallengeUrl(properties, redirectUri);
        }
    }
}
