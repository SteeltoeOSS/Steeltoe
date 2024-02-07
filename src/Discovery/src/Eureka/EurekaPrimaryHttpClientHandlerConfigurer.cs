// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Http;

namespace Steeltoe.Discovery.Eureka;

/// <summary>
/// Configures the primary <see cref="HttpClientHandler" /> used by <see cref="IHttpClientFactory" /> from Eureka client options.
/// </summary>
internal sealed class EurekaPrimaryHttpClientHandlerConfigurer : IPrimaryHttpClientHandlerConfigurer
{
    private readonly IOptionsMonitor<EurekaClientOptions> _optionsMonitor;

    public EurekaPrimaryHttpClientHandlerConfigurer(IOptionsMonitor<EurekaClientOptions> optionsMonitor)
    {
        ArgumentGuard.NotNull(optionsMonitor);

        _optionsMonitor = optionsMonitor;
    }

    public void Configure(HttpClientHandler handler)
    {
        ArgumentGuard.NotNull(handler);

        EurekaClientOptions clientOptions = _optionsMonitor.CurrentValue;

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

        if (!clientOptions.ValidateCertificates)
        {
#pragma warning disable S4830 // Server certificates should be verified during SSL/TLS connections
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#pragma warning restore S4830 // Server certificates should be verified during SSL/TLS connections
        }
    }
}
