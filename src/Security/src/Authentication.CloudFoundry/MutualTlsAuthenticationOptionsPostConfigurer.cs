// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Options;
using Steeltoe.Security.Authentication.Mtls;

namespace Steeltoe.Security.Authentication.CloudFoundry;

public class MutualTlsAuthenticationOptionsPostConfigurer : IPostConfigureOptions<MutualTlsAuthenticationOptions>
{
    private readonly IOptionsMonitor<CertificateOptions> _containerIdentityOptions;
    private readonly ILogger<CloudFoundryInstanceCertificate> _cloudFoundryLogger;

    public MutualTlsAuthenticationOptionsPostConfigurer(IOptionsMonitor<CertificateOptions> containerIdentityOptions, ILoggerFactory loggerFactory)
    {
        _containerIdentityOptions = containerIdentityOptions;
        _cloudFoundryLogger = loggerFactory?.CreateLogger<CloudFoundryInstanceCertificate>();
    }

    public void PostConfigure(string name, MutualTlsAuthenticationOptions options)
    {
        options.IssuerChain = _containerIdentityOptions.Get(ClientCertificates.ContainerIdentity).IssuerChain;

        options.Events = new CertificateAuthenticationEvents
        {
            OnCertificateValidated = context =>
            {
                var claims = new List<Claim>(context.Principal.Claims);

                if (CloudFoundryInstanceCertificate.TryParse(context.ClientCertificate, out CloudFoundryInstanceCertificate cfCert, _cloudFoundryLogger))
                {
                    claims.Add(new Claim(ApplicationClaimTypes.CloudFoundryInstanceId, cfCert.InstanceId, ClaimValueTypes.String,
                        context.Options.ClaimsIssuer));

                    claims.Add(new Claim(ApplicationClaimTypes.CloudFoundryAppId, cfCert.AppId, ClaimValueTypes.String, context.Options.ClaimsIssuer));
                    claims.Add(new Claim(ApplicationClaimTypes.CloudFoundrySpaceId, cfCert.SpaceId, ClaimValueTypes.String, context.Options.ClaimsIssuer));
                    claims.Add(new Claim(ApplicationClaimTypes.CloudFoundryOrgId, cfCert.OrgId, ClaimValueTypes.String, context.Options.ClaimsIssuer));
                }

                var identity = new ClaimsIdentity(claims, CertificateAuthenticationDefaults.AuthenticationScheme);
                context.Principal = new ClaimsPrincipal(identity);
                context.Success();
                return Task.CompletedTask;
            }
        };
    }
}
