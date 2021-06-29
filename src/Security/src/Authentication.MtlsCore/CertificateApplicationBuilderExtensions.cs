// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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