// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Certificates;
using Steeltoe.Common.Configuration;

namespace Steeltoe.Security.Authorization.Certificate;

public static class CertificateHttpClientBuilderExtensions
{
    /// <summary>
    /// Binds certificate paths in configuration to <see cref="CertificateOptions" /> representing the application instance and attaches the certificate to
    /// outbound requests.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHttpClientBuilder" /> to configure an <see cref="HttpClient" /> for sending a client certificate.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHttpClientBuilder AddAppInstanceIdentityCertificate(this IHttpClientBuilder builder)
    {
        return AddClientCertificate(builder, CertificateConfigurationExtensions.AppInstanceIdentityCertificateName);
    }

    /// <summary>
    /// Binds certificate paths in configuration to <see cref="CertificateOptions" /> and attaches the certificate to outbound requests.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHttpClientBuilder" /> to configure an <see cref="HttpClient" /> for sending a client certificate.
    /// </param>
    /// <param name="certificateName">
    /// The name of the certificate used in configuration.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHttpClientBuilder AddClientCertificate(this IHttpClientBuilder builder, string certificateName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(certificateName);

        builder.Services.ConfigureCertificateOptions(certificateName);

        builder.ConfigureHttpClient((serviceProvider, client) =>
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

        return builder;
    }
}
