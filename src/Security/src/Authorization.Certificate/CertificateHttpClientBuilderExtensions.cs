// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Certificates;

namespace Steeltoe.Security.Authorization.Certificate;

public static partial class CertificateHttpClientBuilderExtensions
{
    /// <summary>
    /// Binds certificate paths in configuration to <see cref="CertificateOptions" /> representing the application instance and attaches the certificate to
    /// outbound requests by injecting it as an HTTP header.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHttpClientBuilder" /> to configure an <see cref="HttpClient" /> for sending a client certificate.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    [Obsolete("Injecting a certificate into a header is no longer recommended because it may enable identity spoofing. " +
        $"This overload will be removed in a future release. Use {nameof(AddAppInstanceIdentityCertificateForMutualTls)} instead.")]
    public static IHttpClientBuilder AddAppInstanceIdentityCertificate(this IHttpClientBuilder builder)
    {
        return AddClientCertificate(builder, CertificateConfigurationExtensions.AppInstanceIdentityCertificateName);
    }

    /// <summary>
    /// Binds certificate paths in configuration to <see cref="CertificateOptions" /> representing the application instance and attaches the certificate to
    /// outbound requests by injecting it as an HTTP header.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHttpClientBuilder" /> to configure an <see cref="HttpClient" /> for sending a client certificate.
    /// </param>
    /// <param name="certificateHeaderName">
    /// The name of the HTTP header used to pass the certificate.
    /// </param>
    [Obsolete("Configuring a custom header name is no longer recommended because it may enable identity spoofing. " +
        $"This overload will be removed in a future release. Use {nameof(AddAppInstanceIdentityCertificateForMutualTls)} instead.")]
    public static IHttpClientBuilder AddAppInstanceIdentityCertificate(this IHttpClientBuilder builder, string certificateHeaderName)
    {
        return AddClientCertificate(builder, CertificateConfigurationExtensions.AppInstanceIdentityCertificateName, certificateHeaderName);
    }

    /// <summary>
    /// Binds certificate paths in configuration to <see cref="CertificateOptions" /> and attaches the certificate to outbound requests by injecting it as an
    /// HTTP header.
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
    [Obsolete("Injecting a certificate into a header is no longer recommended because it may enable identity spoofing. " +
        $"This overload will be removed in a future release. Use {nameof(AddClientCertificateForMutualTls)} instead.")]
    public static IHttpClientBuilder AddClientCertificate(this IHttpClientBuilder builder, string certificateName)
    {
        return AddClientCertificate(builder, certificateName, "X-Client-Cert");
    }

    /// <summary>
    /// Binds certificate paths in configuration to <see cref="CertificateOptions" /> and attaches the certificate to outbound requests by injecting it as an
    /// HTTP header.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHttpClientBuilder" /> to configure an <see cref="HttpClient" /> for sending a client certificate.
    /// </param>
    /// <param name="certificateName">
    /// The name of the certificate used in configuration.
    /// </param>
    /// <param name="certificateHeaderName">
    /// The name of the HTTP header used to pass the certificate.
    /// </param>
    [Obsolete("Configuring a custom header name is no longer recommended because it may enable identity spoofing. " +
        $"This overload will be removed in a future release. Use {nameof(AddClientCertificateForMutualTls)} instead.")]
    public static IHttpClientBuilder AddClientCertificate(this IHttpClientBuilder builder, string certificateName, string certificateHeaderName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(certificateName);
        ArgumentNullException.ThrowIfNull(certificateHeaderName);

        builder.Services.ConfigureCertificateOptions(certificateName);

        builder.ConfigureHttpClient((serviceProvider, client) =>
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            ILogger logger = loggerFactory.CreateLogger(typeof(CertificateHttpClientBuilderExtensions).FullName!);
            var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CertificateOptions>>();
            CertificateOptions certificateOptions = optionsMonitor.Get(certificateName);
            X509Certificate2? certificate = certificateOptions.Certificate;

            if (certificate != null)
            {
                LogAddingCertificate(logger, certificate.Subject, certificateHeaderName);

                string b64 = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));
                client.DefaultRequestHeaders.Add(certificateHeaderName, b64);
            }
            else
            {
                LogCertificateNotAttachedToHeader(logger, builder.Name, certificateName, certificateHeaderName);
            }
        });

        return builder;
    }

    /// <summary>
    /// Binds certificate paths in configuration to <see cref="CertificateOptions" /> representing the application instance and attaches the certificate to
    /// outbound requests using mTLS.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHttpClientBuilder" /> to configure an <see cref="HttpClient" /> for sending a client certificate.
    /// </param>
    /// <remarks>
    /// mTLS requires <see cref="SocketsHttpHandler" /> as the primary handler. If a <see cref="SocketsHttpHandler" /> has already been configured (for
    /// example, via <see cref="HttpClientBuilderExtensions.ConfigurePrimaryHttpMessageHandler{THandler}" />), it is reused and the client certificate is
    /// applied to it. If no handler was explicitly configured, a new <see cref="SocketsHttpHandler" /> is created. Configuring any other primary handler
    /// type causes an <see cref="InvalidOperationException" /> when the first request is made. This configuration always runs after any calls to
    /// <see cref="HttpClientBuilderExtensions.ConfigurePrimaryHttpMessageHandler{THandler}" />, regardless of registration order.
    /// </remarks>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHttpClientBuilder AddAppInstanceIdentityCertificateForMutualTls(this IHttpClientBuilder builder)
    {
        return AddClientCertificateForMutualTls(builder, CertificateConfigurationExtensions.AppInstanceIdentityCertificateName);
    }

    /// <summary>
    /// Binds certificate paths in configuration to <see cref="CertificateOptions" /> and attaches the certificate to outbound requests using mTLS.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHttpClientBuilder" /> to configure an <see cref="HttpClient" /> for sending a client certificate.
    /// </param>
    /// <param name="certificateName">
    /// The name of the certificate used in configuration.
    /// </param>
    /// <remarks>
    /// mTLS requires <see cref="SocketsHttpHandler" /> as the primary handler. If a <see cref="SocketsHttpHandler" /> has already been configured (for
    /// example, via <see cref="HttpClientBuilderExtensions.ConfigurePrimaryHttpMessageHandler{THandler}" />), it is reused and the client certificate is
    /// applied to it. If no handler was explicitly configured, a new <see cref="SocketsHttpHandler" /> is created. Configuring any other primary handler
    /// type causes an <see cref="InvalidOperationException" /> when the first request is made. This configuration always runs after any calls to
    /// <see cref="HttpClientBuilderExtensions.ConfigurePrimaryHttpMessageHandler{THandler}" />, regardless of registration order.
    /// </remarks>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHttpClientBuilder AddClientCertificateForMutualTls(this IHttpClientBuilder builder, string certificateName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(certificateName);

        builder.Services.ConfigureCertificateOptions(certificateName);

        builder.Services.AddKeyedSingleton(certificateName, (serviceProvider, _) =>
        {
            var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CertificateOptions>>();
            ILogger logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(CertificateHttpClientBuilderExtensions).FullName!);
            return new CertificateSslContextHolder(certificateName, optionsMonitor, logger);
        });

        builder.Services.PostConfigure<HttpClientFactoryOptions>(builder.Name, options =>
        {
            options.HttpMessageHandlerBuilderActions.Add(handlerBuilder =>
            {
                var certificateContextHolder = handlerBuilder.Services.GetRequiredKeyedService<CertificateSslContextHolder>(certificateName);

                ILogger logger = handlerBuilder.Services.GetRequiredService<ILoggerFactory>()
                    .CreateLogger(typeof(CertificateHttpClientBuilderExtensions).FullName!);

                SslStreamCertificateContext? certificateContext = certificateContextHolder.CertificateContext;

                SocketsHttpHandler socketsHandler;

                if (handlerBuilder.PrimaryHandler is SocketsHttpHandler currentHandler)
                {
                    socketsHandler = currentHandler;
                }
                else
                {
                    // PrimaryHandler defaults to SocketsHttpHandler; reaching this branch means the
                    // user explicitly configured an incompatible handler type, which is an error.
                    throw new InvalidOperationException($"HttpClient '{builder.Name}' has an incompatible primary handler " +
                        $"'{handlerBuilder.PrimaryHandler.GetType().Name}'. mTLS requires SocketsHttpHandler. " +
                        $"Call ConfigurePrimaryHttpMessageHandler<SocketsHttpHandler>() or remove the ConfigurePrimaryHttpMessageHandler call.");
                }

                if (certificateContext == null)
                {
                    LogMutualTlsClientCertificateUnavailable(logger, builder.Name, certificateName);
                }
                else
                {
                    socketsHandler.SslOptions.ClientCertificateContext = certificateContext;
                }

                handlerBuilder.PrimaryHandler = socketsHandler;
            });
        });

        return builder;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Replacing the PrimaryHandler on HttpClient '{httpClientName}' with a new SocketsHttpHandler.")]
    private static partial void LogReplacingPrimaryHttpMessageHandler(ILogger logger, string httpClientName);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Adding certificate with subject '{CertificateSubject}' to outbound requests.")]
    private static partial void LogAddingCertificate(ILogger logger, string certificateSubject);

    [LoggerMessage(Level = LogLevel.Trace,
        Message = "Adding certificate with subject '{CertificateSubject}' to outbound requests in header '{CertificateHeaderName}'.")]
    private static partial void LogAddingCertificate(ILogger logger, string certificateSubject, string certificateHeaderName);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Failed to find a certificate under the name '{CertificateOptionsName}' for HttpClient '{HttpClientName}'. " +
            "It will not be attached to outbound requests in header '{CertificateHeaderName}'.")]
    private static partial void LogCertificateNotAttachedToHeader(ILogger logger, string httpClientName, string certificateOptionsName,
        string certificateHeaderName);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Failed to find a certificate under the name '{CertificateOptionsName}'. " +
            "The mTLS client certificate will not be available until this is resolved.")]
    private static partial void LogMutualTlsCertificateNotConfigured(ILogger logger, string certificateOptionsName);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "HttpClient '{HttpClientName}' is being configured without an mTLS client certificate " +
            "because certificate '{CertificateOptionsName}' is currently unavailable.")]
    private static partial void LogMutualTlsClientCertificateUnavailable(ILogger logger, string httpClientName, string certificateOptionsName);

    private sealed class CertificateSslContextHolder
    {
        private readonly string _certificateName;
        private readonly ILogger _logger;
        private volatile SslStreamCertificateContext? _certificateContext;

        public SslStreamCertificateContext? CertificateContext => _certificateContext;

        public CertificateSslContextHolder(string certificateName, IOptionsMonitor<CertificateOptions> certificateOptionsMonitor, ILogger logger)
        {
            _certificateName = certificateName;
            _logger = logger;

            certificateOptionsMonitor.OnChange((options, name) =>
            {
                if (name == _certificateName)
                {
                    RebuildSslStreamContext(options);
                }
            });

            RebuildSslStreamContext(certificateOptionsMonitor.Get(certificateName));
        }

        private void RebuildSslStreamContext(CertificateOptions options)
        {
            X509Certificate2? certificate = options.Certificate;

            if (certificate != null)
            {
                LogAddingCertificate(_logger, certificate.Subject);
                var issuerChain = new X509Certificate2Collection(options.IssuerChain.ToArray());

                // offline: true prevents the TLS layer from fetching AIA/CRL/OCSP for the Diego CA,
                // which is internal and has no publicly reachable distribution points.
                _certificateContext = SslStreamCertificateContext.Create(certificate, issuerChain, true);
            }
            else
            {
                LogMutualTlsCertificateNotConfigured(_logger, _certificateName);
                _certificateContext = null;
            }
        }
    }
}
