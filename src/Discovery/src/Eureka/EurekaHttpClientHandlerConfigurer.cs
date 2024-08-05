// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Http.HttpClientPooling;
using Steeltoe.Discovery.Eureka.Configuration;

namespace Steeltoe.Discovery.Eureka;

/// <summary>
/// Configures the primary <see cref="HttpClientHandler" /> for a named <see cref="HttpClient" /> from Eureka client options.
/// </summary>
internal sealed class EurekaHttpClientHandlerConfigurer : IHttpClientHandlerConfigurer
{
    private readonly IOptionsMonitor<EurekaClientOptions> _optionsMonitor;

    public EurekaHttpClientHandlerConfigurer(IOptionsMonitor<EurekaClientOptions> optionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        _optionsMonitor = optionsMonitor;
    }

    public void Configure(HttpClientHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

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
    }
}
