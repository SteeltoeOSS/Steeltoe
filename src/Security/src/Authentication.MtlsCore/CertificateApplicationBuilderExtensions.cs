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
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Security;
using System;

namespace Steeltoe.Security.Authentication.Mtls
{
    public static class CertificateApplicationBuilderExtensions
    {
        /// <summary>
        /// Start the certificate rotation service
        /// </summary>
        /// <param name="applicationBuilder">The <see cref="ApplicationBuilder"/></param>
        public static IApplicationBuilder UseCertificateRotation(this IApplicationBuilder applicationBuilder)
        {
            var certificateStoreService = applicationBuilder.ApplicationServices.GetService<ICertificateRotationService>();
            certificateStoreService.Start();
            return applicationBuilder;
        }

        /// <summary>
        /// Adds certificate authentication.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        public static AuthenticationBuilder AddMutualTls(this AuthenticationBuilder builder)
            => builder.AddMutualTls(CertificateAuthenticationDefaults.AuthenticationScheme);

        /// <summary>
        /// Adds certificate authentication.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="authenticationScheme">Scheme identifier</param>
        public static AuthenticationBuilder AddMutualTls(this AuthenticationBuilder builder, string authenticationScheme)
            => builder.AddMutualTls(authenticationScheme, configureOptions: null);

        /// <summary>
        /// Adds certificate authentication.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="configureOptions">Additional options configuration</param>
        public static AuthenticationBuilder AddMutualTls(this AuthenticationBuilder builder, Action<MutualTlsAuthenticationOptions> configureOptions)
            => builder.AddMutualTls(CertificateAuthenticationDefaults.AuthenticationScheme, configureOptions);

        /// <summary>
        /// Adds certificate authentication.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="authenticationScheme">Scheme identifier</param>
        /// <param name="configureOptions">Additional options configuration</param>
        public static AuthenticationBuilder AddMutualTls(
            this AuthenticationBuilder builder,
            string authenticationScheme,
            Action<MutualTlsAuthenticationOptions> configureOptions)
            => builder.AddScheme<MutualTlsAuthenticationOptions, MutualTlsAuthenticationHandler>(authenticationScheme, configureOptions);
    }
}