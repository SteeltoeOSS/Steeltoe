// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.LoadBalancer;

public interface ILoadBalancer
{
    /// <summary>
    /// Evaluates a URI for a host name that can be resolved into a service instance.
    /// </summary>
    /// <param name="request">
    /// A URI containing a service name that can be resolved into one or more service instances.
    /// </param>
    /// <returns>
    /// The original URI, with serviceName replaced by the host:port of a service instance.
    /// </returns>
    Task<Uri> ResolveServiceInstanceAsync(Uri request);

    /// <summary>
    /// A mechanism for tracking statistics for service instances.
    /// </summary>
    /// <param name="originalUri">
    /// The original request URI.
    /// </param>
    /// <param name="resolvedUri">
    /// The URI resolved by the load balancer.
    /// </param>
    /// <param name="responseTime">
    /// The amount of time taken for a remote call to complete.
    /// </param>
    /// <param name="exception">
    /// Any exception called during calls to a resolved service instance.
    /// </param>
    /// <returns>A task.</returns>
    Task UpdateStatsAsync(Uri originalUri, Uri resolvedUri, TimeSpan responseTime, Exception exception);
}
