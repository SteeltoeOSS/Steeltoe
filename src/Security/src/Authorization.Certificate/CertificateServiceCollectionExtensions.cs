// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common.Security;

namespace Steeltoe.Security.Authorization.Certificate;

public static class CertificateServiceCollectionExtensions
{
    /// <summary>
    /// Add necessary components for server-side authorization of client certificates.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    public static IServiceCollection AddCertificateAuthorizationServer(this IServiceCollection services)
    {
        services.ConfigureCertificateOptions("AppInstanceIdentity");

        services.AddCertificateForwarding(opt =>
        {
            opt.HeaderConverter = certificateData =>
            {
                var certificate = new X509Certificate2(Encoding.ASCII.GetBytes(certificateData));
                return certificate;
            };
        });

        services.TryAddEnumerable(ServiceDescriptor
            .Singleton<IPostConfigureOptions<CertificateAuthenticationOptions>, PostConfigureCertificateAuthenticationOptions>());

        services.AddSingleton<IAuthorizationHandler, CertificateAuthorizationHandler>();
        return services;
    }

    /// <summary>
    /// Add a named <see cref="HttpClient" /> and necessary components for finding client certificates and attaching to outbound requests.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    public static IServiceCollection AddCertificateAuthorizationClient(this IServiceCollection services)
    {
        services.ConfigureCertificateOptions("AppInstanceIdentity");

        services.AddHttpClient(CertificateAuthorizationDefaults.HttpClientName, (serviceProvider, client) =>
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            ILogger logger = loggerFactory.CreateLogger("Steeltoe.Security.Authorization.Certificate.CertificateServiceCollectionExtensions");
            var optionsMonitor = serviceProvider.GetService<IOptionsMonitor<CertificateOptions>>();
            CertificateOptions? certificateOptions = optionsMonitor?.Get("AppInstanceIdentity");
            X509Certificate2? certificate = certificateOptions?.Certificate;

            if (certificate != null)
            {
                logger.LogDebug("Adding certificate with subject {CertificateSubject} to outbound requests in header X-Client-Cert", certificate.Subject);

                string b64 = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));
                client.DefaultRequestHeaders.Add("X-Client-Cert", b64);
            }
            else
            {
                logger.LogError("Failed to find certificates");
            }
        });

        return services;
    }
}
