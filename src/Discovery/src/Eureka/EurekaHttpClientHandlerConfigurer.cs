// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Http.HttpClientPooling;
using Steeltoe.Discovery.Eureka.Configuration;

namespace Steeltoe.Discovery.Eureka;

/// <summary>
/// Configures the primary <see cref="HttpClientHandler" /> for the "Eureka" <see cref="HttpClient" /> from configuration.
/// </summary>
internal sealed class EurekaHttpClientHandlerConfigurer
{
    private readonly IOptionsMonitor<EurekaClientOptions> _optionsMonitor;
    private readonly ClientCertificateHttpClientHandlerConfigurer _clientCertificateHttpClientHandlerConfigurer;
    private readonly ValidateCertificatesHttpClientHandlerConfigurer<EurekaClientOptions> _validateCertificatesHttpClientHandlerConfigurer;

    public EurekaHttpClientHandlerConfigurer(IOptionsMonitor<EurekaClientOptions> optionsMonitor,
        ClientCertificateHttpClientHandlerConfigurer clientCertificateHttpClientHandlerConfigurer,
        ValidateCertificatesHttpClientHandlerConfigurer<EurekaClientOptions> validateCertificatesHttpClientHandlerConfigurer)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(validateCertificatesHttpClientHandlerConfigurer);

        _optionsMonitor = optionsMonitor;
        _clientCertificateHttpClientHandlerConfigurer = clientCertificateHttpClientHandlerConfigurer;
        _validateCertificatesHttpClientHandlerConfigurer = validateCertificatesHttpClientHandlerConfigurer;
    }

    public void Configure(HttpClientHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _clientCertificateHttpClientHandlerConfigurer.Configure("Eureka", handler);
        _validateCertificatesHttpClientHandlerConfigurer.Configure(Options.DefaultName, handler);

        EurekaClientOptions clientOptions = _optionsMonitor.CurrentValue;
        ConfigureClientOptions(clientOptions, handler);
    }

    private static void ConfigureClientOptions(EurekaClientOptions clientOptions, HttpClientHandler handler)
    {
        if (handler.SupportsProxy && !string.IsNullOrEmpty(clientOptions.EurekaServer.ProxyHost))
        {
            handler.Proxy = new WebProxy(clientOptions.EurekaServer.ProxyHost, clientOptions.EurekaServer.ProxyPort);

            if (!string.IsNullOrEmpty(clientOptions.EurekaServer.ProxyPassword))
            {
                handler.Proxy.Credentials = new NetworkCredential(clientOptions.EurekaServer.ProxyUserName, clientOptions.EurekaServer.ProxyPassword);
            }
        }

        if (clientOptions.EurekaServer.ShouldGZipContent)
        {
            handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        }
    }
}
