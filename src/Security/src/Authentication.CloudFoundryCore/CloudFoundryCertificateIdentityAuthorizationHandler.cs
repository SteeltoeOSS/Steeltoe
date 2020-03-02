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

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Security;
using Steeltoe.Security.Authentication.Mtls;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public class CloudFoundryCertificateIdentityAuthorizationHandler : IAuthorizationHandler
    {
        private readonly IOptionsMonitor<CertificateOptions> _identityCert;
        private readonly ILogger<CloudFoundryCertificateIdentityAuthorizationHandler> _logger;
        private CloudFoundryInstanceCertificate _cloudFoundryCertificate;

        public CloudFoundryCertificateIdentityAuthorizationHandler(IOptionsMonitor<CertificateOptions> identityCert, ILogger<CloudFoundryCertificateIdentityAuthorizationHandler> logger)
        {
            _logger = logger;
            _identityCert = identityCert;
            _identityCert.OnChange(OnCertRefresh);
            OnCertRefresh(identityCert.CurrentValue);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task HandleAsync(AuthorizationHandlerContext context)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            HandleCertRequirement<SameOrgRequirement>(context, ApplicationClaimTypes.CloudFoundryOrgId, _cloudFoundryCertificate?.OrgId);
            HandleCertRequirement<SameSpaceRequirement>(context, ApplicationClaimTypes.CloudFoundrySpaceId, _cloudFoundryCertificate?.SpaceId);
        }

        private void OnCertRefresh(CertificateOptions cert)
        {
            if (CloudFoundryInstanceCertificate.TryParse(cert.Certificate, out var cfCert, _logger))
            {
                _cloudFoundryCertificate = cfCert;
            }
        }

        private void HandleCertRequirement<T>(AuthorizationHandlerContext context, string claimType, string claimValue)
            where T : IAuthorizationRequirement
        {
            var requirement = context.PendingRequirements.OfType<T>().FirstOrDefault();
            if (requirement == null)
            {
                return;
            }

            if (claimValue == null)
            {
                context.Fail();
                return;
            }

            if (context.User.HasClaim(claimType, claimValue))
            {
                context.Succeed(requirement);
            }
            else
            {
                _logger?.LogDebug("User has the required claim, but the value doesn't match. Expected {0} but got {1}", claimValue, context.User.FindFirstValue(claimType));
            }
        }
    }
}