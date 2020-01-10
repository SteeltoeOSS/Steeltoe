using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Security;
using Steeltoe.Security.Authentication.CloudFoundry;
using Steeltoe.Security.Authentication.MtlsCore.Events;

namespace Steeltoe.Security.Authentication.MtlsCore
{
    public static class CloudFoundryServicesExtensions
    {
        public static IServiceCollection AddCloudFoundryContainerIdentity(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();
            services.AddSingleton<IConfigureOptions<CertificateOptions>, PemConfigureCertificateOptions>();
            services.Configure<CertificateOptions>(configuration);
            services.AddSingleton<ICertificateRotationService, CertificateRotationService>();
            services.AddSingleton<IAuthorizationHandler, CloudFoundryCertificateIdentityAuthorizationHandler>();
            return services.AddCertificateHeaderForwarding(opt => opt.CertificateHeader = "X-Forwarded-Client-Cert");
        }

        public static AuthenticationBuilder AddCloudFoundryIdentityCertificate(this AuthenticationBuilder builder)
        {
            builder.AddCertificate(CertificateAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Events = new CertificateAuthenticationEvents()
                {
                    OnValidateCertificate = context =>
                    {
                        var claims = context.GetDefaultClaims();
                        if (CloudFoundryInstanceCertificate.TryParse(context.ClientCertificate, out var cfCert))
                        {
                            claims.Add(new Claim(CloudFoundryClaimTypes.CloudFoundryInstanceId, cfCert.InstanceId, ClaimValueTypes.String, context.Options.ClaimsIssuer));
                            claims.Add(new Claim(CloudFoundryClaimTypes.CloudFoundryAppId, cfCert.AppId, ClaimValueTypes.String, context.Options.ClaimsIssuer));
                            claims.Add(new Claim(CloudFoundryClaimTypes.CloudFoundrySpaceId, cfCert.SpaceId, ClaimValueTypes.String, context.Options.ClaimsIssuer));
                            claims.Add(new Claim(CloudFoundryClaimTypes.CloudFoundryOrgId, cfCert.OrgId, ClaimValueTypes.String, context.Options.ClaimsIssuer));
                        }

                        var identity = new ClaimsIdentity(claims, CertificateAuthenticationDefaults.AuthenticationScheme);
                        context.Principal = new ClaimsPrincipal(identity);
                        context.Success();
                        return Task.CompletedTask;
                    }
                };
            });
            return builder;
        }
    }
}