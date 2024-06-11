// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Security.Authorization.Certificate;

public sealed class CertificateAuthorizationHandler : IAuthorizationHandler
{
    private readonly ILogger<CertificateAuthorizationHandler> _logger;
    private ApplicationInstanceCertificate? _applicationInstanceCertificate;

    public CertificateAuthorizationHandler(IOptionsMonitor<CertificateOptions> certificateOptionsMonitor, ILogger<CertificateAuthorizationHandler> logger)
    {
        ArgumentGuard.NotNull(certificateOptionsMonitor);

        _logger = logger;
        certificateOptionsMonitor.OnChange(OnCertificateRefresh);
        OnCertificateRefresh(certificateOptionsMonitor.Get("AppInstanceIdentity"));
    }

    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        HandleCertificateAuthorizationRequirement<SameOrgRequirement>(context, ApplicationClaimTypes.OrganizationId,
            _applicationInstanceCertificate?.OrganizationId);

        HandleCertificateAuthorizationRequirement<SameSpaceRequirement>(context, ApplicationClaimTypes.SpaceId, _applicationInstanceCertificate?.SpaceId);
        return Task.CompletedTask;
    }

    private void OnCertificateRefresh(CertificateOptions certificateOptions)
    {
        if (certificateOptions.Certificate == null)
        {
            return;
        }

        if (ApplicationInstanceCertificate.TryParse(certificateOptions.Certificate, out ApplicationInstanceCertificate? applicationInstanceCertificate,
            _logger))
        {
            _applicationInstanceCertificate = applicationInstanceCertificate;
        }
    }

    private void HandleCertificateAuthorizationRequirement<T>(AuthorizationHandlerContext context, string claimType, string? claimValue)
        where T : class, IAuthorizationRequirement
    {
        T? requirement = context.PendingRequirements.OfType<T>().FirstOrDefault();

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
            _logger.LogDebug("User has the required claim, but the value doesn't match. Expected {ClaimValue} but got {ClaimType}", claimValue,
                context.User.FindFirstValue(claimType));
        }
    }
}
