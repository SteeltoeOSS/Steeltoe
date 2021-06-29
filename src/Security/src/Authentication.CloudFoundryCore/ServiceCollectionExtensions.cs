// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Options;
using Steeltoe.Common.Security;
using Steeltoe.Security.Authentication.Mtls;
using System;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds options and services to use Cloud Foundry container identity certificates
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Application Configuration</param>
        public static void AddCloudFoundryContainerIdentity(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.AddOptions();
            services.AddSingleton<IConfigureOptions<CertificateOptions>, PemConfigureCertificateOptions>();
            services.AddSingleton<IPostConfigureOptions<MutualTlsAuthenticationOptions>, MutualTlsAuthenticationOptionsPostConfigurer>();
            services.Configure<CertificateOptions>(configuration);
            services.AddSingleton<ICertificateRotationService, CertificateRotationService>();
            services.AddSingleton<IAuthorizationHandler, CloudFoundryCertificateIdentityAuthorizationHandler>();
            services.AddCertificateForwarding(opt => opt.CertificateHeader = "X-Forwarded-Client-Cert");
        }

        /// <summary>
        /// Adds options and services for Cloud Foundry container identity certificates along with certificate-based authentication and authorization
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Application Configuration</param>
        public static void AddCloudFoundryCertificateAuth(this IServiceCollection services, IConfiguration configuration)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.AddCloudFoundryContainerIdentity(configuration);
            services
                .AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
                .AddCloudFoundryIdentityCertificate();

            services.AddAuthorization(cfg =>
            {
                cfg.AddPolicy(CloudFoundryDefaults.SameOrganizationAuthorizationPolicy, builder => builder.SameOrg());
                cfg.AddPolicy(CloudFoundryDefaults.SameSpaceAuthorizationPolicy, builder => builder.SameSpace());
            });
        }
    }
}