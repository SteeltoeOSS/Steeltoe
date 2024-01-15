// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.LoadBalancer;

namespace Steeltoe.Common.Http.Discovery;

/// <summary>
/// An <see cref="HttpClientHandler" /> implementation that performs Service Discovery.
/// </summary>
public class DiscoveryHttpClientHandler : HttpClientHandler
{
    private readonly ILogger _logger;
    private readonly DiscoveryHttpClientHandlerBase _discoveryBase;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoveryHttpClientHandler" /> class.
    /// </summary>
    /// <param name="discoveryClient">
    /// Service discovery client to use - provided by calling services.AddDiscoveryClient(Configuration).
    /// </param>
    /// <param name="loggerFactory">
    /// ILoggerFactory for capturing logs from Discovery operations.
    /// </param>
    /// <param name="loadBalancer">
    /// The load balancer to use.
    /// </param>
    public DiscoveryHttpClientHandler(IDiscoveryClient discoveryClient, ILoggerFactory loggerFactory, ILoadBalancer? loadBalancer = null)
    {
        ArgumentGuard.NotNull(discoveryClient);
        ArgumentGuard.NotNull(loggerFactory);

        _discoveryBase = new DiscoveryHttpClientHandlerBase(discoveryClient, loggerFactory, loadBalancer);
        _logger = loggerFactory.CreateLogger<DiscoveryHttpClientHandler>();
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(request);

        Uri requestUri = request.RequestUri!;

        try
        {
            request.RequestUri = await _discoveryBase.LookupServiceAsync(requestUri, cancellationToken);
            return await base.SendAsync(request, cancellationToken);
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            _logger.LogDebug(exception, "Exception during SendAsync()");
            throw;
        }
        finally
        {
            request.RequestUri = requestUri;
        }
    }
}
