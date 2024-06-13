// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Configuration;

namespace Steeltoe.Security.Authorization.Certificate;

public sealed class PostConfigureCertificateAuthenticationOptions : IPostConfigureOptions<CertificateAuthenticationOptions>
{
    private readonly IOptionsMonitor<CertificateOptions> _certificateOptionsMonitor;
    private readonly ILogger<PostConfigureCertificateAuthenticationOptions> _logger;

    public PostConfigureCertificateAuthenticationOptions(IOptionsMonitor<CertificateOptions> certificateOptionsMonitor,
        ILogger<PostConfigureCertificateAuthenticationOptions> logger)
    {
        ArgumentGuard.NotNull(certificateOptionsMonitor);

        _certificateOptionsMonitor = certificateOptionsMonitor;
        _logger = logger;
    }

    public void PostConfigure(string? name, CertificateAuthenticationOptions options)
    {
        ArgumentGuard.NotNull(name);
        ArgumentGuard.NotNull(options);

        CertificateOptions appInstanceIdentityOptions = _certificateOptionsMonitor.Get("AppInstanceIdentity");
        options.ChainTrustValidationMode = X509ChainTrustMode.CustomRootTrust;
        options.ClaimsIssuer = appInstanceIdentityOptions.Certificate?.Issuer;
        options.RevocationMode = X509RevocationMode.NoCheck;

        string? systemCertPath = Environment.GetEnvironmentVariable("CF_SYSTEM_CERT_PATH");

        if (!string.IsNullOrEmpty(systemCertPath))
        {
            X509Certificate2[] systemCertificates =
                Directory.GetFiles(systemCertPath).Select(certificateFilename => new X509Certificate2(certificateFilename)).ToArray();

            options.CustomTrustStore.AddRange(systemCertificates);
        }

        if (appInstanceIdentityOptions.IssuerChain.Count > 0)
        {
            options.AdditionalChainCertificates.AddRange(appInstanceIdentityOptions.IssuerChain.ToArray());
        }

        options.Events = new CertificateAuthenticationEvents
        {
            OnCertificateValidated = context =>
            {
                if (context.Principal == null)
                {
                    return Task.CompletedTask;
                }

                var claims = new List<Claim>(context.Principal.Claims);

                if (ApplicationInstanceCertificate.TryParse(context.ClientCertificate, out ApplicationInstanceCertificate? clientCertificate))
                {
                    claims.Add(new Claim(ApplicationClaimTypes.OrganizationId, clientCertificate.OrganizationId, ClaimValueTypes.String,
                        context.Options.ClaimsIssuer));

                    claims.Add(new Claim(ApplicationClaimTypes.SpaceId, clientCertificate.SpaceId, ClaimValueTypes.String, context.Options.ClaimsIssuer));

                    claims.Add(new Claim(ApplicationClaimTypes.ApplicationId, clientCertificate.ApplicationId, ClaimValueTypes.String,
                        context.Options.ClaimsIssuer));

                    claims.Add(new Claim(ApplicationClaimTypes.ApplicationInstanceId, clientCertificate.InstanceId, ClaimValueTypes.String,
                        context.Options.ClaimsIssuer));
                }
                else
                {
                    _logger.LogError("Identity certificate did not match an expected pattern. Subject was: {CertificateSubject}",
                        context.ClientCertificate.Subject);
                }

                var identity = new ClaimsIdentity(claims, CertificateAuthenticationDefaults.AuthenticationScheme);
                context.Principal = new ClaimsPrincipal(identity);
                context.Success();

                return Task.CompletedTask;
            }
        };
    }
}
