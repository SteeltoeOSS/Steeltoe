// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Certificates;
using Steeltoe.Common.Configuration;

namespace Steeltoe.Security.Authorization.Certificate;

public static class CertificateHttpClientBuilderExtensions
{
    /// <summary>
    /// Binds certificate paths in configuration to <see cref="CertificateOptions" /> representing the application instance and attaches the certificate to
    /// outbound requests.
    /// </summary>
    /// <param name="httpClientBuilder">
    /// The <see cref="IHttpClientBuilder" /> to add a client certificate to.
    /// </param>
    public static IHttpClientBuilder AddAppInstanceIdentityCertificate(this IHttpClientBuilder httpClientBuilder)
    {
        return AddClientCertificate(httpClientBuilder, CertificateConfigurationExtensions.AppInstanceIdentityCertificateName);
    }

    /// <summary>
    /// Binds certificate paths in configuration to <see cref="CertificateOptions" /> and attaches the certificate to outbound requests.
    /// </summary>
    /// <param name="httpClientBuilder">
    /// The <see cref="IHttpClientBuilder" /> to configure.
    /// </param>
    /// <param name="certificateName">
    /// The name of the certificate used in configuration.
    /// </param>
    public static IHttpClientBuilder AddClientCertificate(this IHttpClientBuilder httpClientBuilder, string certificateName)
    {
        ArgumentGuard.NotNull(httpClientBuilder);
        ArgumentGuard.NotNull(certificateName);

        httpClientBuilder.Services.ConfigureCertificateOptions(certificateName);

        httpClientBuilder.ConfigureHttpClient((serviceProvider, client) =>
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            ILogger logger = loggerFactory.CreateLogger(nameof(CertificateAuthorizationBuilderExtensions));
            var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CertificateOptions>>();
            CertificateOptions certificateOptions = optionsMonitor.Get(certificateName);
            X509Certificate2? certificate = certificateOptions.Certificate;

            if (certificate != null)
            {
                logger.LogTrace("Adding certificate with subject {CertificateSubject} to outbound requests in header X-Client-Cert", certificate.Subject);

                string b64 = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));
                client.DefaultRequestHeaders.Add("X-Client-Cert", b64);
            }
            else
            {
                logger.LogError("Failed to find a certificate under the name {CertificateOptionsName}", certificateName);
            }
        });

        return httpClientBuilder;
    }
}
