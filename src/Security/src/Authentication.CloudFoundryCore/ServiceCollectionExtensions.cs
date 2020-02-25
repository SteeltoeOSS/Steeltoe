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
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Security;
using Steeltoe.Security.Authentication.Mtls;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCloudFoundryContainerIdentity(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();
            services.AddSingleton<IConfigureOptions<CertificateOptions>, PemConfigureCertificateOptions>();
            services.AddSingleton<IPostConfigureOptions<MutualTlsAuthenticationOptions>, CertificateOptionsPostConfigurer>();
            services.Configure<CertificateOptions>(configuration);
            services.AddSingleton<ICertificateRotationService, CertificateRotationService>();
            services.AddSingleton<IAuthorizationHandler, CloudFoundryCertificateIdentityAuthorizationHandler>();
            return services.AddCertificateForwarding(opt => opt.CertificateHeader = "X-Forwarded-Client-Cert");
        }

        public static AuthenticationBuilder AddCloudFoundryIdentityCertificate(this AuthenticationBuilder builder)
        {
            var logger = builder.Services.BuildServiceProvider().GetService<ILogger<CloudFoundryInstanceCertificate>>();
            builder.AddMutualTls(options =>
            {
                options.Events = new CertificateAuthenticationEvents()
                {
                    OnCertificateValidated = context =>
                    {
                        var claims = new List<Claim>(context.Principal.Claims);
                        if (CloudFoundryInstanceCertificate.TryParse(context.ClientCertificate, out var cfCert, logger))
                        {
                            claims.Add(new Claim(ApplicationClaimTypes.CloudFoundryInstanceId, cfCert.InstanceId, ClaimValueTypes.String, context.Options.ClaimsIssuer));
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
            });
            return builder;
        }
    }
}