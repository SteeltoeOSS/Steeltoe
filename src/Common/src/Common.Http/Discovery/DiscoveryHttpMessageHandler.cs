// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Logging;
using Steeltoe.Common.LoadBalancer;
using Steeltoe.Discovery;

namespace Steeltoe.Common.Http.Discovery;

/// <summary>
/// A <see cref="DelegatingHandler" /> implementation that performs Service Discovery.
/// </summary>
public class DiscoveryHttpMessageHandler : DelegatingHandler
{
    private readonly ILogger _logger;
    private readonly DiscoveryHttpClientHandlerBase _discoveryBase;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoveryHttpMessageHandler" /> class.
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
    public DiscoveryHttpMessageHandler(IDiscoveryClient discoveryClient, ILoggerFactory loggerFactory, ILoadBalancer? loadBalancer = null)
    {
        _discoveryBase = new DiscoveryHttpClientHandlerBase(discoveryClient, loggerFactory, loadBalancer);
        _logger = loggerFactory.CreateLogger<DiscoveryHttpMessageHandler>();
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
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
