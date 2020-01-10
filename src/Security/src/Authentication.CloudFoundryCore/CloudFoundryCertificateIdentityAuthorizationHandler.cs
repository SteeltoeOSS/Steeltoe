using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Security;
using Steeltoe.Security.Authentication.MtlsCore;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public class CloudFoundryCertificateIdentityAuthorizationHandler : IAuthorizationHandler
    {
        private readonly IOptionsMonitor<CertificateOptions> _identityCert;
        private CloudFoundryInstanceCertificate _cloudFoundryCertificate;

        public CloudFoundryCertificateIdentityAuthorizationHandler(IOptionsMonitor<CertificateOptions> identityCert)
        {
            _identityCert = identityCert;
            _identityCert.OnChange(OnCertRefresh);
            OnCertRefresh(identityCert.CurrentValue);
        }

        private void OnCertRefresh(CertificateOptions cert)
        {
            if (CloudFoundryInstanceCertificate.TryParse(cert.Certificate, out var cfCert))
            {
                _cloudFoundryCertificate = cfCert;
            }
        }

        public async Task HandleAsync(AuthorizationHandlerContext context)
        {
            HandleCertRequirement<SameOrgRequirement>(context, CloudFoundryClaimTypes.CloudFoundryOrgId, _cloudFoundryCertificate?.OrgId);
            HandleCertRequirement<SameSpaceRequirement>(context, CloudFoundryClaimTypes.CloudFoundrySpaceId, _cloudFoundryCertificate?.SpaceId);
        }

        private void HandleCertRequirement<T>(AuthorizationHandlerContext context, string claimType, string claimValue)
            where T : IAuthorizationRequirement
        {
            ClaimsPrincipal user = context.User;
            var requirement = context.PendingRequirements.OfType<T>().FirstOrDefault();
            if (requirement == null)
                return;
            if (claimValue == null)
            {
                context.Fail();
                return;
            }

            if (user.HasClaim(claimType, claimValue))
            {
                context.Succeed(requirement);
            }
        }
    }
}