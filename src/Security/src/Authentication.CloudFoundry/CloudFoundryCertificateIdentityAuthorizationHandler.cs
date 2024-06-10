// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Configuration;

namespace Steeltoe.Security.Authentication.CloudFoundry;

public class CloudFoundryCertificateIdentityAuthorizationHandler : IAuthorizationHandler
{
    private readonly ILogger<CloudFoundryCertificateIdentityAuthorizationHandler> _logger;
    private CloudFoundryInstanceCertificate _cloudFoundryCertificate;

    public CloudFoundryCertificateIdentityAuthorizationHandler(IOptionsMonitor<CertificateOptions> identityCert,
        ILogger<CloudFoundryCertificateIdentityAuthorizationHandler> logger)
    {
        _logger = logger;
        identityCert.OnChange(OnCertRefresh);
        OnCertRefresh(identityCert.Get("ContainerIdentity"));
    }

    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        HandleCertRequirement<SameOrgRequirement>(context, ApplicationClaimTypes.CloudFoundryOrgId, _cloudFoundryCertificate?.OrgId);
        HandleCertRequirement<SameSpaceRequirement>(context, ApplicationClaimTypes.CloudFoundrySpaceId, _cloudFoundryCertificate?.SpaceId);
        return Task.CompletedTask;
    }

    private void OnCertRefresh(CertificateOptions cert)
    {
        if (CloudFoundryInstanceCertificate.TryParse(cert.Certificate, out CloudFoundryInstanceCertificate cfCert, _logger))
        {
            _cloudFoundryCertificate = cfCert;
        }
    }

    private void HandleCertRequirement<T>(AuthorizationHandlerContext context, string claimType, string claimValue)
        where T : class, IAuthorizationRequirement
    {
        T requirement = context.PendingRequirements.OfType<T>().FirstOrDefault();

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
            _logger?.LogDebug("User has the required claim, but the value doesn't match. Expected {0} but got {1}", claimValue,
                context.User.FindFirstValue(claimType));
        }
    }
}
