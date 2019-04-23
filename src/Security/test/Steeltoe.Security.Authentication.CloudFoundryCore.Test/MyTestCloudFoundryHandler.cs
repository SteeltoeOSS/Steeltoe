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
            return await this.ExchangeCodeAsync(code, redirectUri);
        }

        public string TestBuildChallengeUrl(AuthenticationProperties properties, string redirectUri)
        {
            return BuildChallengeUrl(properties, redirectUri);
        }
    }
}
