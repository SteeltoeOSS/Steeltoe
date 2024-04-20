// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Discovery.HttpClients.LoadBalancers;

public interface ILoadBalancer
{
    /// <summary>
    /// Evaluates a URI for a host name that can be resolved into a service instance.
    /// </summary>
    /// <param name="requestUri">
    /// A URI containing a service name that can be resolved into one or more service instances.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// The original URI, with the service name replaced by the host and port of a service instance.
    /// </returns>
    Task<Uri> ResolveServiceInstanceAsync(Uri requestUri, CancellationToken cancellationToken);

    /// <summary>
    /// A mechanism for tracking statistics for service instances.
    /// </summary>
    /// <param name="requestUri">
    /// The original request URI.
    /// </param>
    /// <param name="serviceInstanceUri">
    /// The URI resolved by the load balancer.
    /// </param>
    /// <param name="responseTime">
    /// The amount of time taken for a remote call to complete.
    /// </param>
    /// <param name="exception">
    /// Any exception thrown during calls to a resolved service instance.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    Task UpdateStatisticsAsync(Uri requestUri, Uri serviceInstanceUri, TimeSpan? responseTime, Exception? exception, CancellationToken cancellationToken);
}
